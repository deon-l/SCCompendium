using OneOf;
using System.Text;
using SCCompendium.Application.Parser;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser : ILatexParser
{
    private ReadOnlySpan<char> ExecuteCommand(ReadOnlySpan<char> segment, Context context)
    {
        int commandNameLength;
        for (commandNameLength = 1; commandNameLength < segment.Length; commandNameLength++)
        {
            if (!Char.IsLetterOrDigit(segment[commandNameLength]))
            {
                break;
            }
        }

       Command? command;
        if (!context.Commands.TryGetValue(new String(segment[..commandNameLength]), out command)
            && !(commandNameLength == 1 && segment.Length >= 2 && context.Commands.TryGetValue(new String(segment[..2]), out command)))
        {
            throw new ArgumentException($"Undefined command: {new String(segment[..commandNameLength])}", nameof(segment));
        }

        return command(segment[commandNameLength..], context);
    }

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
                builder.Append(c);
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
                    ParseLatexTipaSegment(ipaSegment, builder);
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
            char ligatureReplace = segment[i..] switch
            {
                ['\"', '\"', ..] => 'ˌ',
                ['|', '|', ..] => '‖',
                ['\"', ..] => 'ˈ',
                ['|', ..] => '|',
                _ => '\0'
            };
            if (ligatureReplace != '\0')
            {
                builder.Append(ligatureReplace);
                if (i + 1 < segment.Length && segment[i] == segment[i + 1])
                {
                    i++;
                }
                continue;
            }

            char c = segment[i];
            if (Char.IsWhiteSpace(c))
            {
                builder.Append(c);
                continue;
            }
            if (c == '\\')
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
        if (segment.Length == 0)
        {
            return;
        }

        char command = segment[0];
        if (command == '^')
        {
            builder.EnsureCapacity(builder.Length + segment[1..].Length);
            foreach (char num in segment[1..])
            {
                Debug.Assert(Char.IsDigit(num));
                builder.Append(num switch
                {
                    // code value for superscript 1/2/3 is
                    '1' => '¹',
                    '2' => '²',
                    '3' => '³',
                    _ => (char)(num - '0' + '⁰')
                });
            }
            return;
        }
        if (command == '_')
        {
            builder.EnsureCapacity(builder.Length + segment[1..].Length);
            foreach (char num in segment[1..])
            {
                Debug.Assert(Char.IsDigit(num));
                builder.Append((char)(num - '0' + '₀'));
            }
            return;
        }
        if (command != '\\')
        {
            throw new ArgumentException($"unrecognized command ({{command}}) in $$ sequence: '{segment}'",
                nameof(segment));
        }

        char simpleTokenResult = segment[1..] switch
        {
            "Omega" => 'Ω',
            "langle" => '⟨',
            "rangle" => '⟩',
            _ => '\0'
        };

        if (simpleTokenResult != '\0')
        {
            builder.Append(simpleTokenResult);
            return;
        }
        throw new ArgumentException($"unrecognized \\command ({segment[1..]}) in $$ sequence: '{segment}'",
            nameof(segment));
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
        if (delimiter.Length == 0)
        {
            delimiter = "{}".AsSpan();
        }

        Debug.Assert(delimiter.Length == 2);

        if (segment[0] != delimiter[0])
        {
            for (var i = 0; i < segment.Length; i++)
            {
                if (!Char.IsWhiteSpace(segment[i]))
                {
                    enumeratedCount = i + 1;
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
        enumeratedCount = endI + 1;
        return segment[1..endI];
    }
}
