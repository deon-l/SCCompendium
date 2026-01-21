using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DiachronicaParserSearcher.Parser;

public class DiachronicaParser
{
    /// <remarks>
    /// Formated as '[char1][char2][buffer]', for visual clarity. Not all pairs are here.
    /// </remarks>
    private const string IpaDoubleCharSource = "pɸ bβ pf bv ts dz tʃ ʈʂ ɖʐ tɕ dʑ cç ɟʝ kx ɡɣ qχ ɢʁ ʡʜ ʡʢ ʔh tɬ dɮ ";

    private readonly Regex _sectionHeader = new(@"^\\(?:sub)*(?:section|paragraph)", RegexOptions.Compiled);
    private readonly Regex _ruleDecomposer =
        new(@"^(--- )?(.+?)(?:\\change|\\textrightarrow)(.+?)(?:/(.+?))?(?:!(?![^{\n]*?})(.+?))?(?:\\\\)?$",
            RegexOptions.Compiled);
    private readonly LatexParser _latexParser = new();

    private static readonly HashSet<(char, char)> _ipaDoubleChars =
        IpaDoubleCharSource.Chunk(3).Select(chars => (chars[0], chars[1])).ToHashSet();
    private static readonly HashSet<char> _vowels = new("iyɨʉɯuɪʏʊeøɘɵɤoəɛœɜɞʌɔæɐaɶɑɒ");

        private (string title, string credit)? GetNextSubsection(SavingTextReader reader)
    {
        Match result;
        while (true)
        {
            if (reader.CurrentLine is null)
            {
                return null;
            }

            result = _sectionHeader.Match(reader.CurrentLine);
            if (result.Success)
            {
                break;
            }

            reader.Advance();
        }

        int titleStartIndex = result.Length;
        Debug.Assert(reader.CurrentLine[titleStartIndex] == '{');
        int titleEndIndex = reader.CurrentLine.IndexOf('}', titleStartIndex);
        Debug.Assert(titleEndIndex != -1);
        string title = reader.CurrentLine[(titleStartIndex + 1)..titleEndIndex];
        string credit = reader.CurrentLine[(titleEndIndex + 1)..];
        return (title, credit);
    }

    public Dictionary<string, (string credit, List<PhonologicalRule> rules)> ParseFile(TextReader file)
    {
        ArgumentNullException.ThrowIfNull(file);
        SavingTextReader reader = new(file);

        List<Exception> sectionExceptions = new();
        Dictionary<string, (string credit, List<PhonologicalRule> rules)> organizedRules = new();
        StringBuilder builder = new();

        reader.Advance();
        for (var subsectionHeader = GetNextSubsection(reader);
             subsectionHeader is not null;
             subsectionHeader = GetNextSubsection(reader))
        {
            (List<PhonologicalRule> rules, List<Exception> sectionParsingErrors) = ParseSubsection(reader);
            if (sectionParsingErrors.Count == 0 && rules.Count == 0)
            {
                continue;
            }

            string titleTranslated;
            string creditTranslated;
            try
            {
                _latexParser.ParseLatexSegment(subsectionHeader.Value.title, builder);
                titleTranslated = builder.ToString();
            }
            catch (Exception e)
            {
                sectionParsingErrors.Insert(0, new ArgumentException(
                    $"Error parsing title of section: {subsectionHeader.Value.title}", nameof(file), e));
                titleTranslated = subsectionHeader.Value.title;
            }
            builder.Clear();
            try
            {
                _latexParser.ParseLatexSegment(subsectionHeader.Value.credit, builder);
                creditTranslated = builder.ToString();
            }
            catch (Exception e)
            {
                sectionParsingErrors.Insert(0, new ArgumentException(
                    $"Error parsing title of section: {subsectionHeader.Value.credit}", nameof(file), e));
                creditTranslated = subsectionHeader.Value.credit;
            }

            if (sectionParsingErrors.Count > 0)
            {
                sectionExceptions.Add(
                    new AggregateException($"errors while parsing section: {titleTranslated}", sectionParsingErrors));
                continue;
            }

            organizedRules.Add(titleTranslated, (creditTranslated, rules));
        }

        if (sectionExceptions.Count > 0)
        {
            // Todo: Temporary - TUnits normal logging of AggregateExceptions isn't formated well enough.
            Console.WriteLine(new AggregateException(sectionExceptions));
            return new();
            throw new AggregateException("errors while parsing stream", sectionExceptions);
        }

        return organizedRules;
    }

    private (List<PhonologicalRule>, List<Exception> exceptions) ParseSubsection(SavingTextReader file)
    {
        List<Exception> exceptions = new();
        List<PhonologicalRule> rules = new();
        string possiblePrenote = "";
        bool isPrenoteParsed = false;
        bool isPrenoteGreedy = false;
        while (true)
        {
            string? line = file.ReadNextLine();
            if (line is null)
            {
                break;
            }
            if (String.IsNullOrEmpty(line))
            {
                continue;
            }
            if (IsSectionEnder(line))
            {
                break;
            }

            PhonologicalRule rule;
            bool successfulParse;
            try
            {
                successfulParse = TryParseRule(line, out rule);
            }
            catch (Exception e)
            {
                exceptions.Add(new ArgumentException($"Error parsing line: {line}", nameof(file), e));
                continue;
            }

            if (!successfulParse)
            {
                possiblePrenote = line;
                isPrenoteParsed = false;
                isPrenoteGreedy = possiblePrenote.StartsWith("---");
                continue;
            }

            if (rule.Rule.StartsWith('—') || isPrenoteGreedy)
            {
                if (!isPrenoteParsed)
                {
                    try
                    {
                        possiblePrenote = _latexParser.ParseLatexSegment(possiblePrenote);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(new ArgumentException($"Error parsing line: {line}", nameof(file), e));
                    }
                    isPrenoteParsed = true;
                }

                rule = rule with { Prenote = possiblePrenote };
            }
            rules.Add(rule);
        }

        return (rules, exceptions);

        bool IsSectionEnder(string line)
        {
            return _sectionHeader.IsMatch(line);
        }
    }

    private bool TryParseRule(string line,
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
        List<string>
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
            ParseRuleSegment(output, ruleBuilder, contextChars);
        }
        if (exception.Success && !context.ValueSpan.IsWhiteSpace())
        {
            ruleBuilder.Append('!');
            ParseRuleSegment(output, ruleBuilder, contextChars);
        }

        rule = new PhonologicalRule(ruleBuilder.ToString(), inputChars, outputChars, contextChars);
        return true;


        void ParseRuleSegment(Group segment, StringBuilder builder, List<string> foundCharacters)
        {
            // TODO: Strip english parts.
            int startI = builder.Length;
            _latexParser.ParseLatexSegment(segment.ValueSpan, builder);

            var addedSegment = builder.ToString(startI, builder.Length - startI).AsSpan();
            ExtractCharacters(addedSegment, foundCharacters);
        }
    }

    private void ExtractCharacters(ReadOnlySpan<char> segment, List<string> foundChars)
    {
        for (int i = 0; i < segment.Length; i++)
        {
            char c = segment[i];
            if (!IsIpaChar(c))
            {
                continue;
            }

            int endI = i + 1;
            if (_vowels.Contains(c))
            {
                while (endI < segment.Length && _vowels.Contains(segment[endI]))
                {
                    endI++;
                }
            }
            else // consonants
            {
                if (endI < segment.Length && _ipaDoubleChars.Contains((c, segment[endI])))
                {
                    endI++;
                }
            }

            for (; endI < segment.Length; endI++)
            {
                char c2 = segment[endI];
                if (c2 == '[')
                {
                    int attributeEndI = segment[endI..].IndexOf(']');
                    if (attributeEndI == -1)
                    {
                        endI = segment.Length;
                        break;
                    }

                    endI += attributeEndI;
                    continue;
                }
                if (Char.GetUnicodeCategory(segment[endI]) is UnicodeCategory.ModifierLetter
                    or UnicodeCategory.ModifierSymbol or UnicodeCategory.SpacingCombiningMark
                    or UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }
                break;
            }

            foundChars.Add(segment[i..endI].ToString());
        }

        bool IsIpaChar(char c)
        {
            return !(
                Char.IsWhiteSpace(c) ||
                "[](){}_".Contains(c)
            );
        }
    }

    private class SavingTextReader(TextReader reader)
    {
        public TextReader TextReader { get; } = reader;
        public string? CurrentLine { get; private set; }

        public string? ReadNextLine()
        {
            Advance();
            return CurrentLine;
        }

        public void Advance()
        {
            CurrentLine = TextReader.ReadLine();
        }
    }
}
