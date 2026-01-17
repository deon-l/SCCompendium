using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DiachronicaParserSearcher.Parser;

public class DiachronicaParser
{
    private readonly Regex _sectionHeader = new(@"^\\(?:sub)*(?:section|paragraph)", RegexOptions.Compiled);
    private readonly Regex _ruleDecomposer =
        new(@"^(--- )?(.+?)(?:\\change|\\textrightarrow)(.+?)(?:/(.+?))?(?:!(?![^{\n]*?})(.+?))?(?:\\\\)?$",
        RegexOptions.Compiled);
    private readonly LatexParser _latexParser = new();

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
                    new AggregateException($"errors while parsing section: {titleTranslated}", sectionExceptions));
                continue;
            }

            organizedRules.Add(titleTranslated, (creditTranslated, rules));
        }

        if (sectionExceptions.Count > 0)
        {
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
            if (line is not null)
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
                    possiblePrenote = _latexParser.ParseLatexSegment(possiblePrenote);
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
            int startI = builder.Length + 1;
            _latexParser.ParseLatexSegment(segment.ValueSpan, builder);

            var addedSegment = builder.ToString(startI, builder.Length - startI).AsSpan();
            for (int i = 0; i < addedSegment.Length; i++)
            {
                char c = builder[i];
                if (Char.IsWhiteSpace(c) || c is '{' or '}' or '_' or ',' or '(' or ')')
                {
                    continue;
                }

                int endI = i;
                while (true)
                {
                    endI++;
                    if (Char.GetUnicodeCategory(addedSegment[endI]) is UnicodeCategory.ModifierLetter
                        or UnicodeCategory.ModifierSymbol or UnicodeCategory.SpacingCombiningMark
                        or UnicodeCategory.NonSpacingMark)
                    {
                        continue;
                    }
                    if (addedSegment[endI] == '[')
                    {
                        endI = addedSegment[endI..].IndexOf(']') + endI;
                        continue;
                    }

                    break;
                }

                foundCharacters.Add(new String(addedSegment[i..endI]));
            }
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
