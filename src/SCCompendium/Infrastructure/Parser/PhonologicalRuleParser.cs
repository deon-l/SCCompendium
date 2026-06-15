using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SCCompendium.Application.Parser;
using SCCompendium.Domain;

namespace SCCompendium.Infrastructure.Parser;

public class PhonologicalRuleParser : IPhonologicalRuleParser
{
    /// <summary>Source string for <see cref="_ipaDoubleChars"/></summary>
    /// <remarks>Formated as '[char1][char2][buffer]', for visual clarity. Not all pairs are here.</remarks>
    private const string IpaDoubleCharSource = "pɸ bβ pf bv ts dz tʃ ʈʂ ɖʐ tɕ dʑ cç ɟʝ kx ɡɣ qχ ɢʁ ʡʜ ʡʢ ʔh tɬ dɮ ";
    /// <summary>Separator inserted if multiple notes are found in the same rule.</summary>
    private const string NoteSeparator = " | ";

    /// <summary>set of pairs of chars which in ipa may be considered 1 sound (e.g. affricates).</summary>
    private static readonly HashSet<(char, char)> _ipaDoubleChars =
        IpaDoubleCharSource.Chunk(3).Select(chars => (chars[0], chars[1])).ToHashSet();
    /// <summary>set of vowels</summary>
    private static readonly HashSet<char> _vowels = new("iyɨʉɯuɪʏʊeøɘɵɤoəɛœɜɞʌɔæɐaɶɑɒ");

    /// <summary>Regex used to identify source rules, and decompose them into parts.</summary>
    /// <remarks>Also detects the starting "---" segment in some rules, and the ending "\\".</remarks>
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
        StringBuilder noteSb = new StringBuilder();

        Debug.Assert(input.Success && !input.ValueSpan.IsWhiteSpace());
        ParseRuleSegment(StripNote(input.ValueSpan, FieldType.Input, noteSb), inputChars);

        Debug.Assert(output.Success && !output.ValueSpan.IsWhiteSpace());
        ParseRuleSegment(StripNote(output.ValueSpan, FieldType.Output, noteSb), outputChars);

        if (context.Success && !context.ValueSpan.IsWhiteSpace())
        {
            ParseRuleSegment(StripNote(context.ValueSpan, FieldType.Context, noteSb), contextChars);
        }

        if (exception.Success && !exception.ValueSpan.IsWhiteSpace())
        {
            ParseRuleSegment(StripNote(exception.ValueSpan, FieldType.Context, noteSb), contextChars);
        }

        string ruleString = _latexParser.ParseLatexSegment(line);
        string notes = noteSb.ToString();
        rule = new PhonologicalRule(ruleString, inputChars.ToArray(), outputChars.ToArray(), contextChars.ToArray(),
            notes);
        return true;

        void ParseRuleSegment(ReadOnlySpan<char> segment, List<IpaCharacter> foundCharacters)
        {
            string addedSegment = _latexParser.ParseLatexSegment(segment);
            ExtractCharacters(addedSegment, foundCharacters);
        }
    }

    /// <summary>
    /// Scans the already parsed <paramref name="segment"/> for ipa sounds and
    /// adds them to <paramref name="foundChars"/>.
    /// </summary>
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
                "[](){},_~".Contains(c)
            );
        }
    }

    /// <summary>
    /// Parse <paramref name="segment"/> and add it to <paramref name="sb"/>.
    /// Will separate from existing notes using <see cref="NoteSeparator"/> if necessary.
    /// </summary>
    private void AddNote(ReadOnlySpan<char> segment, StringBuilder sb)
    {
        if (sb.Length != 0)
        {
            sb.Append(NoteSeparator);
        }

        _latexParser.ParseLatexSegment(segment, sb);
    }

    /// <summary>
    /// Indicates the part of a phonological rule a segment is from.
    /// </summary>
    /// <remarks>
    /// <see cref="FieldType.Context"/> refers to both context and exception parts.
    /// Merged as they are often parsed identically.
    /// </remarks>
    private enum FieldType
    {
        Input, Output, Context
    }

    /// <summary>
    /// Takes in <i>unparsed</i> ipa in <paramref name="segment"/>,
    /// and returns segment stripped of notes (non-phonological data).
    /// The stripped notes are parsed and added to <paramref name="noteSb"/>.
    /// </summary>
    private ReadOnlySpan<char> StripNote(ReadOnlySpan<char> segment, FieldType fieldType, StringBuilder noteSb)
    {
        const int significantNoteLength = 5;
        const int edgeBuffer = 4;

        if (fieldType == FieldType.Input)
        {
            for (int i = 0; i < segment.Length; i++)
            {
                char c = segment[i];
                if (Char.IsUpper(c))
                {
                    continue;
                }
                if (Char.IsLower(c))
                {
                    while (i < segment.Length
                           && Char.IsLetterOrDigit(segment[i]))
                    {
                        i++;
                    }
                    if (i < segment.Length && ":;.?! ".Contains(segment[i]))
                    {
                        i++;
                    }

                    AddNote(segment[..i], noteSb);
                    segment = segment[i..];
                }
                break;
            }
        }
        if (fieldType == FieldType.Context)
        {
            int place = segment.IndexOf('_');
            if (place == -1)
            {
                AddNote(segment, noteSb);
                return new();
            }
        }

        int quoteStart = segment.LastIndexOf("``");
        int parenthesisStart = segment.LastIndexOf('(');
        int parenthesisEnd = segment.LastIndexOf(')');

        if ((quoteStart < parenthesisStart || parenthesisEnd < quoteStart)
            && quoteStart > 0 && segment.Length - quoteStart > significantNoteLength)
        {
            AddNote(segment[quoteStart..], noteSb);
            segment = segment[..quoteStart];
            return StripNote(segment, fieldType, noteSb);
        }

        if (parenthesisStart != -1 && parenthesisEnd != -1 && parenthesisEnd > parenthesisStart
            && parenthesisEnd - parenthesisStart > significantNoteLength
            && !segment[parenthesisStart..parenthesisEnd].Contains('\\')
           )
        {
            if (parenthesisStart < edgeBuffer)
            {
                AddNote(segment[..(parenthesisStart + 1)], noteSb);
                segment = segment[(parenthesisEnd + 1)..];
            }
            else if (parenthesisEnd > segment.Length - edgeBuffer)
            {
                AddNote(segment[parenthesisStart..], noteSb);
                segment = segment[..parenthesisStart];
            }
        }

        return segment;
    }
}
