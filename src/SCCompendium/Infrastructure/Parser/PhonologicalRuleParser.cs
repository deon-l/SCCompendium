using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using SCCompendium.Application.Parser;
using SCCompendium.Domain;

namespace SCCompendium.Infrastructure.Parser;

public class PhonologicalRuleParser : IPhonologicalRuleParser
{
    /// <remarks>Formated as '[char1][char2][buffer]', for visual clarity. Not all pairs are here.</remarks>
    private const string IpaDoubleCharSource = "pɸ bβ pf bv ts dz tʃ ʈʂ ɖʐ tɕ dʑ cç ɟʝ kx ɡɣ qχ ɢʁ ʡʜ ʡʢ ʔh tɬ dɮ ";
    private static readonly HashSet<(char, char)> _ipaDoubleChars =
        IpaDoubleCharSource.Chunk(3).Select(chars => (chars[0], chars[1])).ToHashSet();
    private static readonly HashSet<char> _vowels = new("iyɨʉɯuɪʏʊeøɘɵɤoəɛœɜɞʌɔæɐaɶɑɒ");

    private static readonly Regex _ruleDecomposer =
        new(@"^(--- )?(.+?)(?:\\change|\\textrightarrow)(.+?)(?:/(.+?))?(?:!(?![^{\n]*?})(.+?))?(?:\\\\)?$",
            RegexOptions.Compiled);

    private readonly ILatexParser _latexParser;

    public PhonologicalRuleParser(ILatexParser latexParser)
    {
        _latexParser = latexParser;
    }

    public bool TryParseRule(string line,
        out PhonologicalRule rule)
    {
        Match result = _ruleDecomposer.Match(line);
        if (!result.Success)
        {
            rule = default;
            return false;
        }

        GroupCollection groups = result.Groups;
        Debug.Assert(groups.Count == 6);

        bool inSubgroup = groups[1].Success;
        Group
            input = groups[2],
            output = groups[3],
            context = groups[4],
            exception = groups[5];
        List<IpaCharacter>
            inputChars = [],
            outputChars = [],
            contextChars = [];

        StringBuilder ruleBuilder = new();
        if (inSubgroup)
        {
            ruleBuilder.Append('—');
        }
        Debug.Assert(input.Success && !input.ValueSpan.IsWhiteSpace());
        ParseRuleSegment(input, ruleBuilder, inputChars);
        ruleBuilder.Append('→');
        Debug.Assert(output.Success && !output.ValueSpan.IsWhiteSpace());
        ParseRuleSegment(output, ruleBuilder, outputChars);
        if (context.Success && !context.ValueSpan.IsWhiteSpace())
        {
            ruleBuilder.Append('/');
            ParseRuleSegment(context, ruleBuilder, contextChars);
        }
        if (exception.Success && !context.ValueSpan.IsWhiteSpace())
        {
            ruleBuilder.Append('!');
            ParseRuleSegment(exception, ruleBuilder, contextChars);
        }

        rule = new PhonologicalRule(ruleBuilder.ToString(), inputChars.ToArray(), outputChars.ToArray(), contextChars.ToArray());
        return true;


        void ParseRuleSegment(Group segment, StringBuilder builder, List<IpaCharacter> foundCharacters)
        {
            // TODO: Strip english parts.
            int startI = builder.Length;
            _latexParser.ParseLatexSegment(segment.ValueSpan, builder);

            var addedSegment = builder.ToString(startI, builder.Length - startI).AsSpan();
            ExtractCharacters(addedSegment, foundCharacters);
        }
    }

    private void ExtractCharacters(ReadOnlySpan<char> segment, List<IpaCharacter> foundChars)
    {
        for (int i = 0; i < segment.Length; /* increment manually as loop moves i */)
        {
            char c = segment[i];
            if (!IsIpaChar(c))
            {
                i++;
                continue;
            }

            int characterEndI = i + 1;
            if (_vowels.Contains(c))
            {
                while (characterEndI < segment.Length && _vowels.Contains(segment[characterEndI]))
                {
                    characterEndI++;
                }
            }
            else // is maybe affricate
            {
                if (characterEndI < segment.Length && _ipaDoubleChars.Contains((c, segment[characterEndI])))
                {
                    characterEndI++;
                }
            }

            string character = segment[i..characterEndI].ToString();
            i = characterEndI;

            List<string> diacritics = new();

            for (/* i */; i < segment.Length; /* manually increment as loop moves i */)
            {
                char c2 = segment[i];
                if (c2 == '[')
                {
                    int diacriticLength = segment[i..].IndexOf(']');
                    if (diacriticLength == -1)
                    {
                        break;
                    }

                    Debug.Assert(segment[i + diacriticLength] ==']');
                    ParseProperties(segment[(i + 1)..(i + diacriticLength)], diacritics);
                    i += diacriticLength + 1;
                    continue;
                }
                if (Char.GetUnicodeCategory(c2) is UnicodeCategory.ModifierLetter
                    or UnicodeCategory.ModifierSymbol or UnicodeCategory.SpacingCombiningMark
                    or UnicodeCategory.NonSpacingMark)
                {
                    diacritics.Add(segment[i].ToString());
                    i++;
                    continue;
                }

                break;
            }

            diacritics.Sort(StringComparer.Ordinal);
            IpaCharacter ipaChar = new(character, diacritics.ToArray());
            if (!foundChars.Contains(ipaChar))
            {
                foundChars.Add(ipaChar);
            }
        }

        // Parse properties contained in segment, and add them to param `diacritics`.
        // Assumes segment isn't surrounded by '[ ]'.
        static void ParseProperties(ReadOnlySpan<char> segment, List<string> diacritics)
        {
            segment = segment.TrimStart();
            if (segment.IsEmpty || segment.IsWhiteSpace())
            {
                return;
            }

            Debug.Assert(segment[0] is '+' or '-');
            bool positiveProperty = segment[0] == '+';

            segment = segment[1..].TrimStart();
            if (segment.Length < 0)
            {
                // Todo: more appropriate Exception, maybe
                throw new ArgumentException("Has property with no associated name", nameof(segment));
            }
            if (segment[0] is '+' or '-')
            {
                throw new ArgumentException("duplicated +/- sequence", nameof(segment));
            }

            int parsedLength = 0;
            while (parsedLength < segment.Length)
            {
                char c = segment[parsedLength];
                if (Char.IsLetter(c))
                {
                    parsedLength++;
                    continue;
                }
                ReadOnlySpan<char> maybeNext = segment[(parsedLength + 1)..].TrimStart();
                if (maybeNext.IsEmpty || maybeNext[0] is '+' or '-')
                {
                    break;
                }

                parsedLength++;
            }

            if (parsedLength == 0)
            {
                throw new ArgumentException("Property name is just a symbol.", nameof(segment));
            }
            diacritics.Add($"[{(positiveProperty ? '+' : '-')}{segment[..parsedLength]}]");
            if (parsedLength < segment.Length)
            {
                parsedLength++;
            }
            ParseProperties(segment[parsedLength..], diacritics);
        }

        static bool IsIpaChar(char c)
        {
            return !(
                Char.IsWhiteSpace(c) ||
                "[](){}_~".Contains(c)
            );
        }
    }
}
