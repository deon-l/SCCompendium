using System.Text;

namespace DiachronicaParserSearcher.Parser;

public class LatexParser
{
    public const int ContextNone = 0;
    public const int ContextIpa = 1;

    private readonly Dictionary<char, char> _tipaSingleCharConversions = new()
    {
        {':', 'ː'},
        {';', '\u02D1'},
        {'"', 'ˈ'},
        {'0', 'ʉ'},
        {'1', 'ɨ'},
        {'2', 'ʌ'},
        {'3', 'ɜ'},
        {'4', 'ɥ'},
        {'5', 'ɐ'},
        {'6', 'ɒ'},
        {'7', 'ɤ'},
        {'8', 'ɵ'},
        {'9', 'ɘ'},
        {'@', 'ə'},
        {'A', 'ɑ'},
        {'B', 'β'},
        {'C', 'ɕ'},
        {'D', 'ð'},
        {'E', 'ɛ'},
        {'F', 'ɸ'},
        {'G', 'ɣ'},
        {'H', 'ɦ'},
        {'I', 'ɪ'},
        {'J', 'ʝ'},
        {'K', 'ʁ'},
        {'L', 'ʎ'},
        {'M', 'ɱ'},
        {'N', 'ŋ'},
        {'O', 'ɔ'},
        {'P', 'ʔ'},
        {'Q', 'ʕ'},
        {'R', 'ɾ'},
        {'S', 'ʃ'},
        {'T', 'θ'},
        {'U', 'ʊ'},
        {'V', 'ʋ'},
        {'W', 'ɯ'},
        {'X', 'χ'},
        {'Y', 'ʏ'},
        {'Z', 'ʒ'},
        {'|', '|'}
    };

    public string ParseLatexSegment(ReadOnlySpan<char> segment)
    {
        StringBuilder sb = new();
        ParseLatexSegment(segment, sb);
        return sb.ToString();
    }

    public void ParseLatexSegment(ReadOnlySpan<char> segment, StringBuilder builder)
    {
        int length = segment.Length;
        for (int i = 0; i < length; i++)
        {
            char c = segment[i];
            if (Char.IsWhiteSpace(c))
            {
                continue;
            }
            if (c == '\\')
            {
                var command = GetCommandName(segment[(i+1)..]);
                i += command.Length;
                if (command.Length == 1 && !Char.IsLetter(command[0]))
                {
                    builder.Append(command[0]);
                    continue;
                }
                if (command is "ipa" or "textipa")
                {
                    Debug.Assert(i + 1 < length && segment[i+1] == '{');
                    ReadOnlySpan<char> ipaSegment = GetCommandArgument(segment[(i + 1)..], out var iIncrement);
                    ParseLatexSegment(ipaSegment, builder);
                    i += iIncrement;
                    continue;
                }
                if (command is "hspace")
                {
                    Debug.Assert(i + 1 < length && segment[i+1] == '{');
                    while (segment[i] != '}') { i++; }
                    builder.Append(' ');
                    continue;
                }

                throw new ArgumentException(
                    $"unexpected command (\\{command}) at {i - command.Length} of segment: {segment}", nameof(segment));
            }
            if (c is '{' or '}')
            {
                throw new ArgumentException($"unexpected '{c}' found at {i} of segment: {segment}", nameof(segment));
            }
            if (c == '$')
            {
                int endI = segment[(i + 1)..].IndexOf('$') + i + 1;
                Debug.Assert(endI > i);

                ParseLatexMathSegment(segment[(i+1)..endI], builder);

                i = endI;
                continue;
            }

            builder.Append(c);
        }
    }

    public void ParseLatexTipaSegment(ReadOnlySpan<char> segment, StringBuilder builder)
    {
        for (int i = 0; i < segment.Length; i++)
        {
            char c = segment[i];
            if (i == '\\')
            {
                // Todo: Alternative algorithm required, since some shortcuts have 2 non-letter symbols.
                ReadOnlySpan<char> command = GetCommandName(segment[(i + 1)..]);

                // Todo: implement commands for Tipa

                throw new ArgumentException(
                    $"unexpected command (\\{command}) at {i - command.Length} of segment: {segment}", nameof(segment));
            }
            if (_tipaSingleCharConversions.TryGetValue(c, out char converted))
            {
                builder.Append(converted);
                continue;
            }
            if (c == '$')
            {
                int endI = segment[(i + 1)..].IndexOf('$') + i + 1;
                Debug.Assert(endI > i);

                ParseLatexMathSegment(segment[(i+1)..endI], builder);

                i = endI;
                continue;
            }
            builder.Append(c);
        }
    }

    // assumes only one command per segment
    public void ParseLatexMathSegment(ReadOnlySpan<char> segment, StringBuilder builder)
    {
        char command = segment[0];
        if (command == '^')
        {
            builder.EnsureCapacity(builder.Length + segment[1..].Length);
            foreach (char num in segment[1..])
            {
                Debug.Assert(Char.IsDigit(num));
                builder.Append(num - '0' + '⁰');
            }
            return;
        }
        if (command == '_')
        {
            builder.EnsureCapacity(builder.Length + segment[1..].Length);
            foreach (char num in segment[1..])
            {
                Debug.Assert(Char.IsDigit(num));
                builder.Append(num - '0' + '₀');
            }
            return;
        }
        if (command != '\\')
        {
            throw new ArgumentException($"unrecognized command ({{command}}) in $$ sequence: '{segment}'",
                nameof(segment));
        }

        if (segment[1..] is "Omega")
        {
            builder.Append('Ω');
        }
        else
        {
            throw new ArgumentException($"unrecognized \\command ({segment[1..]}) in $$ sequence: '{segment}'",
                nameof(segment));
        }
    }

    private ReadOnlySpan<char> GetCommandName(ReadOnlySpan<char> segment)
    {
        int i;
        for (i = 0; i < segment.Length; i++)
        {
            if (!Char.IsAsciiLetterOrDigit(segment[i]))
            {
                if (i == 0)
                {
                    i++;
                }

                break;
            }
        }
        return segment[..i];
    }

    private ReadOnlySpan<char> GetCommandArgument(ReadOnlySpan<char> segment, out int enumeratedCount,
        ReadOnlySpan<char> delimiter = default)
    {
        if (delimiter.Length != 2)
        {
            delimiter = "{}".AsSpan();
        }

        if (segment[0] != delimiter[0])
        {
            for (var i = 0; i < segment.Length; i++)
            {
                if (!Char.IsWhiteSpace(segment[i]))
                {
                    enumeratedCount = i;
                    return segment[i..(i + 1)];
                }
            }
        }

        int endI = segment.IndexOf(delimiter[1]);
        if (endI < 0)
        {
            throw new ArgumentException($"argument isn't properly closed with '{delimiter[1]}'", nameof(segment));
        }

        Debug.Assert(!segment[1..endI].Contains(delimiter[0]));
        enumeratedCount = endI;
        return segment[1..endI];
    }
}
