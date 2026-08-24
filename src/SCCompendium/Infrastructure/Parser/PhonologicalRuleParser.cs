using System.Globalization;
using System.Text;
using SCCompendium.Application.Parser;
using SCCompendium.Domain.ValueObjects.Parsed;

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

    private readonly ILatexParser _latexParser;

    public PhonologicalRuleParser(ILatexParser latexParser)
    {
        _latexParser = latexParser;
    }

    /// <summary>
    /// Result struct holding the segments a rule is broken into, or indicating that the matching failed.
    /// </summary>
    private readonly ref struct RuleDecomposition
    {
        public bool IsMatchSuccess { get; init;  }
        public bool InSubgroup { get; init; }
        public ReadOnlySpan<char> InputSegment { get; init; }
        public ReadOnlySpan<char> OutputSegment { get; init; }
        public ReadOnlySpan<char> ContextSegment { get; init; }
        public ReadOnlySpan<char> ExceptionSegment { get; init; }

        public override String ToString()
        {
            return IsMatchSuccess ? $"Matched Rule:\n\tInput = '{InputSegment}' \n\toutput = '{OutputSegment}'\n\tcontext = '{ContextSegment}\n\texception = '{ExceptionSegment}'"
                                  : "No match";
        }
    }

    /// <summary>
    /// Finds the first index position in <paramref name="str"/> that matches any of the strings in <paramref name="matches"/>
    /// It takes into account collections formed from <c>()</c>, <c>{}</c>, etc. and doesn't match within those groups.
    /// </summary>
    /// <returns>The first index with a found match and the string that matched, or <c>(-1, <see cref="String.Empty"/>)</c></returns>
    private (int, string match) MultiIndexOfConsiderate(ReadOnlySpan<char> str, params Span<string> matches)
    {
        bool wasBackslash = false;
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (wasBackslash)
            {
                wasBackslash = false;
            }
            else if (c == '\\')
            {
                wasBackslash = true;
            }
            else
            {
                int amount = 0;
                if (c == '{')
                {
                    amount = Skip(str[i..], '{', '}');
                }
                else if (c == '(')
                {
                    amount = Skip(str[i..], '(', ')');
                }
                else if (c == '[')
                {
                    amount = Skip(str[i..], '[', ']');
                }
                else if (str[i..].StartsWith("``"))
                {
                    amount = MultiIndexOfConsiderate(str[(i + 2)..], "''", "\"").Item1;
                    if (amount == -1)
                    {
                        break;
                    }
                }

                if (amount > 0)
                {
                    i += amount;
                    continue;
                }
            }

            foreach (string match in matches)
            {
                if (str[i..].StartsWith(match))
                {
                    return (i, match);
                }
            }
        }
        return (-1, String.Empty);

        static int Skip(ReadOnlySpan<char> segment, char opening, char closing)
        {
            Debug.Assert(segment.Length > 0);
            Debug.Assert(segment[0] == opening);
            int skipAmount;
            int depth = 0;
            for (skipAmount = 0; skipAmount < segment.Length; skipAmount++)
            {
                char c = segment[skipAmount];
                if (c == opening)
                {
                    depth++;
                }
                if (c == closing)
                {
                    depth--;
                    if (depth == 0)
                    {
                        break;
                    }
                }
                if (c == '\\')
                {
                    skipAmount++;
                }
            }

            if (depth != 0)
            {
                return -1;
            }
            return skipAmount;
        }
    }


    private RuleDecomposition DecomposeRule(ReadOnlySpan<char> line)
    {
        var (nextIndexOffset, nextMatch) = MultiIndexOfConsiderate(line, @"\change", @"\textrightarrow");
        if (nextIndexOffset == -1)
        {
            return new RuleDecomposition() { IsMatchSuccess = false };
        }
        int inputEndI = nextIndexOffset;
        RuleDecomposition decomposition = new() { IsMatchSuccess = true,  InputSegment = line[..inputEndI] };

        if (line.TrimStart().StartsWith("---"))
        {
            decomposition = decomposition with { InSubgroup = true };
        }

        int currentI = nextIndexOffset + nextMatch.Length;
        string currentMatch = nextMatch;
        int outputStartI = currentI;
        int outputEndI = inputEndI;

        do
        {
            if (currentMatch == "")
            {
                break;
            }

            if (currentMatch == "!")
            {
                break;
            }

            (nextIndexOffset, nextMatch) =
                MultiIndexOfConsiderate(line[currentI..], @"\change", @"\textrightarrow", "/", "!");
            int nextIndex = (nextIndexOffset == -1 ? line.Length : currentI + nextIndexOffset);

            inputEndI = outputEndI;
            outputEndI = nextIndex;
            decomposition = decomposition with
            {
                InputSegment = line[..inputEndI],
                OutputSegment = line[outputStartI..outputEndI],
            };

            currentI += nextIndexOffset + nextMatch.Length;
            currentMatch = nextMatch;
        } while (currentMatch is not ("" or "!" or "/"));

        int contextStartI = currentI;
        int contextEndI = currentI;
        if (currentMatch == "/")
        {
            (nextIndexOffset, nextMatch) = MultiIndexOfConsiderate(line[currentI..],  "!");
            if (nextIndexOffset == -1)
            {
                contextEndI = line.Length;
                currentI = line.Length;
            }
            else
            {
                contextEndI = currentI + nextIndexOffset;
                currentI += nextIndexOffset + nextMatch.Length;
            }
            currentMatch = nextMatch;
        }
        decomposition = decomposition with { ContextSegment = line[contextStartI..contextEndI] };

        if (currentMatch == "!")
        {
            decomposition = decomposition with { ExceptionSegment = line[currentI..] };
        }

        return decomposition;
    }

    public bool TryParseRule(string line, out PhonologicalRule rule)
    {
        (int start, int length) = ExtractExtraneousCommands(line);
        RuleDecomposition decomposition = DecomposeRule(line.AsSpan().Slice(start, length));
        Console.WriteLine(decomposition.ToString());
        if (!decomposition.IsMatchSuccess)
        {
            rule = default;
            return false;
        }
        List<IpaCharacter>
            inputChars = [],
            outputChars = [],
            contextChars = [];
        StringBuilder noteSb = new StringBuilder();

        Debug.Assert(!decomposition.InputSegment.IsWhiteSpace());
        ParseRuleSegment(StripNote(decomposition.InputSegment, FieldType.Input, noteSb), inputChars);

        Debug.Assert(!decomposition.OutputSegment.IsWhiteSpace());
        ParseRuleSegment(StripNote(decomposition.OutputSegment, FieldType.Output, noteSb), outputChars);

        if (!decomposition.ContextSegment.IsWhiteSpace())
        {
            ParseRuleSegment(StripNote(decomposition.ContextSegment, FieldType.Context, noteSb), contextChars);
        }

        if (!decomposition.ExceptionSegment.IsWhiteSpace())
        {
            ParseRuleSegment(StripNote(decomposition.ExceptionSegment, FieldType.Context, noteSb), contextChars);
        }

        string ruleString = _latexParser.ParseLatexSegment(line.AsSpan().Slice(start, length));
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

    private (int start, int length) ExtractExtraneousCommands(string line)
    {
        int startI = 0;
        int endI = line.Length;
        while (true)
        {
            if (endI == startI)
            {
                return (0, 0);
            }
            var segment = line.AsSpan()[startI..endI];
            startI += segment.Length - segment.TrimStart().Length;
            endI -= segment.Length - segment.TrimEnd().Length;

            if (startI >= endI)
            {
                return (0, 0);
            }
            Console.WriteLine($"{startI}:{endI}");
            segment = line.AsSpan()[startI..endI];

            if (segment.EndsWith(@"\\"))
            {
                endI -= 2;
                continue;
            }

            if (StartsWithWord(segment, @"\item"))
            {
                startI += @"\item".Length;
                continue;
            }

            if (StartsWithWord(segment, @"\end"))
            {
                startI += @"\end".Length;
                while (startI < endI && Char.IsWhiteSpace(line[startI]))
                {
                    startI++;
                }

                int depth = 0;
                do
                {
                    if (startI == endI)
                    {
                        break;
                    }
                    char c = line[startI];
                    switch (c)
                    {
                        case '{':
                            depth++; break;
                        case '}':
                            depth--; break;
                        case '\\':
                            startI++; break;
                    }
                    startI++;
                } while (depth > 0);
                continue;
            }

            int index = segment.LastIndexOf(@"\end");
            if (index != -1 && StartsWithWord(segment[index..], @"\end"))
            {
                endI -= segment.Length - index;
                continue;
            }

            break;
        }

        return (startI, endI - startI);

        static bool StartsWithWord(ReadOnlySpan<char> segment, String word)
        {
            return segment.StartsWith(word)
                && (segment.Length <= word.Length || !Char.IsLetter(segment[word.Length]));
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
                if (Char.IsUpper(c) || Char.IsWhiteSpace(c))
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
            segment = StripFromEndConsiderate(segment, noteSb, quoteStart);
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
                segment = StripFromEndConsiderate(segment, noteSb, parenthesisStart);
            }
        }

        return segment;
    }

    /// <summary>
    /// Attempts to strip a portion of the text in <paramref name="segment"/>, from <paramref name="targetIndex"/>
    /// (inclusive) to the end, taking more in order to maintain proper group closure and ensure both are valid latex
    /// segments
    /// </summary>
    private ReadOnlySpan<char> StripFromEndConsiderate(ReadOnlySpan<char> segment, StringBuilder noteSb, int targetIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(targetIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(targetIndex,segment.Length);

        int depth = 0;
        int sliceI;
        for (sliceI = segment.Length - 1; sliceI >= 0 && (sliceI >= targetIndex || depth > 0); sliceI--)
        {
            Console.WriteLine($"sliceI: {sliceI} ({segment[sliceI]})");
            if (sliceI == 0 || segment[sliceI - 1] != '\\')
            {
                char c = segment[sliceI];
                if (c == '{') depth--;
                if (c == '}') depth++;
            }
        }

        while (sliceI >= 0 && char.IsWhiteSpace(segment[sliceI]))
        {
            sliceI--;
        }

        sliceI++;
        int commandStartI = sliceI - 1;
        while (commandStartI >= 0 && Char.IsLetter(segment[commandStartI]))
        {
            commandStartI--;
        }

        if (commandStartI >= 0 && segment[commandStartI] == '\\')
        {
            var commandName = segment[(commandStartI + 1)..sliceI];
            if (commandName is "textit" or "textbf" or "texttt" or "textsc")
            {
                sliceI = commandStartI;
            }
        }
        Console.WriteLine($"{segment} - {sliceI} = {segment[sliceI..]}");
        AddNote(segment[sliceI..], noteSb);
        return segment[..sliceI];
    }
}
