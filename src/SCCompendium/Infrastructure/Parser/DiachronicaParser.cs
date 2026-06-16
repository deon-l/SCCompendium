using System.Text;
using System.Text.RegularExpressions;
using SCCompendium.Application.Parser;
using SCCompendium.Domain;

namespace SCCompendium.Infrastructure.Parser;

public class DiachronicaParser : IDiachronicaParser
{
    private readonly Regex _sectionHeader = new(@"^\\(?:sub)*(?:section|paragraph)", RegexOptions.Compiled);

    private readonly ILatexParser _latexParser;
    private readonly IPhonologicalRuleParser _phonoRuleParser;

    public DiachronicaParser(ILatexParser latexParser, IPhonologicalRuleParser phonoRuleParser)
    {
        _latexParser = latexParser;
        _phonoRuleParser = phonoRuleParser;
    }


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
            if (result.Success && !result.Value.StartsWith(@"\section"))
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

    public List<PhonologicalRuleGroup> Parse(TextReader file)
    {
        ArgumentNullException.ThrowIfNull(file);
        SavingTextReader reader = new(file);

        List<Exception> sectionExceptions = new();
        List<PhonologicalRuleGroup> ruleGroups = new();

        reader.Advance();
        for (var subsectionHeader = GetNextSubsection(reader);
             subsectionHeader is not null;
             subsectionHeader = GetNextSubsection(reader))
        {
            (List<PhonologicalRule> rules, List<Exception> sectionParsingErrors) = ParseSubsectionRules(reader);
            if (sectionParsingErrors.Count == 0 && rules.Count == 0)
            {
                continue;
            }

            string titleTranslated;
            string creditTranslated;
            try
            {
                titleTranslated = _latexParser.ParseLatexSegment(subsectionHeader.Value.title);
            }
            catch (Exception e)
            {
                sectionParsingErrors.Insert(0, new ArgumentException(
                    $"Error parsing title of section: {subsectionHeader.Value.title}", nameof(file), e));
                titleTranslated = subsectionHeader.Value.title;
            }
            try
            {
                creditTranslated = _latexParser.ParseLatexSegment(subsectionHeader.Value.credit);
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

            ruleGroups.Add(new(titleTranslated, creditTranslated, rules));
        }

        if (sectionExceptions.Count > 0)
        {
            // Todo: Temporary - TUnits normal logging of AggregateExceptions isn't formated well enough.
            Console.WriteLine(new AggregateException(sectionExceptions));
            return new();
            throw new AggregateException("errors while parsing stream", sectionExceptions);
        }

        return ruleGroups;
    }

    /// <summary>Parses and lists lines containing rules until encounters next section header.</summary>
    /// <remarks>
    /// Starts at current line at <paramref name="file"/>, so assumes it isn't current section header.
    /// </remarks>
    private (List<PhonologicalRule>, List<Exception> exceptions) ParseSubsectionRules(SavingTextReader file)
    {
        List<Exception> exceptions = new();
        List<PhonologicalRule> rules = new();
        string possiblePrenote = "";
        bool isPrenoteParsed = false;
        bool isPrenoteGreedy = false;
        while (true)
        {
            string? line = file.CurrentLine;
            file.Advance();
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
                successfulParse = _phonoRuleParser.TryParseRule(line, out rule);
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

                rule = rule with { Note = possiblePrenote };
            }
            rules.Add(rule);
        }

        return (rules, exceptions);

        bool IsSectionEnder(string line)
        {
            return _sectionHeader.IsMatch(line);
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
