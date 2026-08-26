using System.Text;
using System.Text.RegularExpressions;
using SCCompendium.Application.Parser;
using SCCompendium.Domain.ValueObjects.Parsed;

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

    /// <summary>
    /// Advances <paramref name="reader"/> to next section header, and returns unparsed title/credit of header.
    /// Returns <see langword="null"/> on end of text.
    /// </summary>
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
        int titleEndIndex;
        int depth = 1;
        for (titleEndIndex = titleStartIndex + 1; depth > 0; titleEndIndex++)
        {
            if (titleEndIndex >= reader.CurrentLine.Length)
            {
                reader.Advance();
                GetNextSubsection(reader);
            }
            char c = reader.CurrentLine[titleEndIndex];
            if (c == '{')
            {
                depth++;
            }
            if (c == '}')
            {
                depth--;
            }
            if (c == '\\')
            {
                // skip next char, in case it is an escaped '{' or '}'
                titleEndIndex++;
            }
        }
        string title = reader.CurrentLine[titleStartIndex..titleEndIndex];
        string credit = reader.CurrentLine[titleEndIndex..];
        return (title, credit);
    }

    /// <summary>
    /// Advances <paramref name="reader"/> to next nonempty line, and returns it unparsed if it is a section note.
    /// Otherwise, returns <see langword="null"/>.
    /// </summary>
    private string? TryGetNote(SavingTextReader reader)
    {
        while (true)
        {
            if (reader.CurrentLine is null)
            {
                return null;
            }
            if (reader.CurrentLine.IsWhiteSpace())
            {
                reader.Advance();
                continue;
            }
            break;
        }
        ReadOnlySpan<char> line = reader.CurrentLine;
        line = line.TrimStart();
        while (line.Length > 0)
        {
            if (line.StartsWith(@"\tab"))
            {
                line = line[@"\tab".Length..].TrimStart();
                continue;
            }
            if (line[0] == '{')
            {
                line = line[1..].TrimStart();
                continue;
            }
            if (line.StartsWith(@"\it"))
            {
                line = line[@"\it".Length..].TrimStart();
                continue;
            }
            if (line.StartsWith(@"\textit{"))
            {
                line = line[@"\textit{".Length..].TrimStart();
                continue;
            }
            break;
        }

        var match = line.StartsWith("NB");
        string? result = match ? reader.CurrentLine : null;
        if (match)
        {
            reader.Advance();
        }
        return result;
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
            reader.Advance();
            string? sectionNote = TryGetNote(reader);
            (List<PhonologicalRule> rules, List<Exception> sectionParsingErrors) = ParseSubsectionRules(reader);
            if (sectionParsingErrors.Count == 0 && rules.Count == 0)
            {
                continue;
            }

            string titleTranslated;
            string creditTranslated;
            string sectionNoteTranslated = String.Empty;
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
            if (sectionNote is not null)
            {
                sectionNoteTranslated = _latexParser.ParseLatexSegment(sectionNote);
            }

            if (sectionParsingErrors.Count > 0)
            {
                sectionExceptions.Add(
                    new AggregateException($"errors while parsing section: {titleTranslated}", sectionParsingErrors));
                continue;
            }

            ruleGroups.Add(new(titleTranslated, creditTranslated, rules, sectionNoteTranslated));
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
            if (line is null)
            {
                break;
            }
            if (line.IsWhiteSpace())
            {
                file.Advance();
                continue;
            }
            if (IsSectionEnder(line))
            {
                break;
            }
            file.Advance();

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
                // note: file.CurrentLine here refers to the next line to process.
                if (file.CurrentLine is not null && IsPossibleNote(line))
                {
                    possiblePrenote = line;
                    isPrenoteParsed = false;
                    isPrenoteGreedy = !file.CurrentLine.StartsWith("---");
                }
                else if (!line.StartsWith("---"))
                {
                    possiblePrenote = "";
                }
                continue;
            }

            if (line.StartsWith("---") || isPrenoteGreedy)
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
            else
            {
                possiblePrenote = "";
            }
            rules.Add(rule);
        }

        return (rules, exceptions);

        bool IsPossibleNote(string line)
        {
            ReadOnlySpan<char> span = line.AsSpan().TrimEnd();
            if (span.EndsWith(@"\\"))
            {
                span = span[..^2].TrimEnd();
            }
            if (!span.EndsWith(':'))
            {
                return false;
            }
            if (line.StartsWith("---") && possiblePrenote.Length != 0 && !isPrenoteGreedy)
            {
                return false;
            }
            return true;
        }

        bool IsSectionEnder(string line)
        {
            return _sectionHeader.IsMatch(line);
        }
    }

    /// <summary>
    /// Wrapper around <see cref="TextReader"/> to save output of <see cref="TextReader.ReadLine"/>
    /// </summary>
    private class SavingTextReader(TextReader reader)
    {
        public TextReader TextReader { get; } = reader;
        public string? CurrentLine { get; private set; }

        /// <remarks>Also saves line.</remarks>
        public string? ReadNextLine()
        {
            Advance();
            return CurrentLine;
        }

        /// <summary>
        /// Gets next line, and saves output in <see cref="CurrentLine"/>.
        /// </summary>
        public void Advance()
        {
            CurrentLine = TextReader.ReadLine();
        }
    }
}
