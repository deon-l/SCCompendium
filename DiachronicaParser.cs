using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public class DiachronicaParser
{
    private readonly Regex _sectionHeader = new(@"^\\(sub)*(section|paragraph)", RegexOptions.Compiled);
    private readonly Regex _ruleDecomposer =
        new(@"^(.+?)(?:\\change|\\textrightarrow)(.+?)(?:/(.+?))?(?:!(?![^{\n]*?})(.+?))?(?:\\\\)?$",
        RegexOptions.Compiled);
    private readonly LatexParser _latexParser = new();

    private (string title, string credit)? GetNextSubsection(StreamReader file, string currentLine = "")
    {
        while (!_sectionHeader.IsMatch(currentLine))
        {
            string? line = file.ReadLine();
            if (line is null)
            {
                return null;
            }

            currentLine = line;
        }

        int titleStartIndex = currentLine.IndexOf('{', @"\section".Length);
        int titleEndIndex = currentLine.IndexOf('}', titleStartIndex);
        Debug.Assert(titleEndIndex != -1);
        string title = currentLine.Substring(titleStartIndex + 1, titleEndIndex);
        string credit = currentLine.Substring(titleEndIndex + 1);
        return (title, credit);
    }

    public void ParseFile(StreamReader file)
    {
        ArgumentNullException.ThrowIfNull(file);

        string currentLine;
        for (var subsectionHeader = GetNextSubsection(file);
             subsectionHeader is not null;
             subsectionHeader = GetNextSubsection(file, currentLine))
        {
            string? line = file.ReadLine();
            if (line is null)
            {
                break;
            }

            List<string> rules;
            (rules, currentLine) = ParseSubsection(file);
        }
    }

    private (List<string> rules, string nextLine) ParseSubsection(StreamReader file)
    {
        List<string> rules = new();
        string? line;
        while (true)
        {
            line = file.ReadLine();
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

            // Todo: handle used chars.
            if (!TryParseRule(line, out string rule, out _, out _, out _))
            {
                continue;
            }
            rules.Add(rule);
        }

        return (rules, line);

        bool IsSectionEnder(string line)
        {
            return _sectionHeader.IsMatch(line);
        }
    }

    private bool TryParseRule(string line,
        out string rule, out List<string> inputChars, out List<string> outputChars, out List<string> contextChars)
    {
        Match result = _ruleDecomposer.Match(line);
        if (!result.Success)
        {
            rule = null!;
            inputChars = null!;
            outputChars = null!;
            contextChars = null!;
            return false;
        }

        GroupCollection groups = result.Groups;
        Debug.Assert(groups.Count == 5);

        Group input = groups[1];
        Group output = groups[2];
        Group context = groups[3];
        Group exception = groups[4];

        inputChars = [];
        outputChars = [];
        contextChars = [];

        StringBuilder ruleBuilder = new();

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
        rule = ruleBuilder.ToString();
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




    private void SkipLine(StreamReader file)
    {
        int c;
        do
        {
            c = file.Read();
        } while (c is not (-1 or '\n' or '\r'));

        if (c == '\n' && file.Peek() == '\r')
        {
            _ = file.Read();
        }
    }
}
