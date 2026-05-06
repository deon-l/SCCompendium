using OneOf;
using System.Text;
using SCCompendium.Application.Parser;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser : ILatexParser
{
    private static StringSlice LoadArgument(Context context)
    {
        while (Char.IsWhiteSpace(context.PeekSource()))
        {
            _ = context.PopSource();
        }

        int argumentStartI = context.LengthResult;
        int groupDepth = 0;
        do
        {
            if (context.LengthSource == 0)
            {
                throw new ArgumentException("Unclosed argument", nameof(context));
            }

            char c = context.PopSource();
            if (c == '{')
            {
                if (groupDepth != 0)
                {
                   context.AppendResult(c);
                }
                groupDepth++;
                continue;
            }
            if (c == '}')
            {
                if (groupDepth == 0)
                {
                    throw new ArgumentException("unexpected '}' closing an unopened group (argument)");
                }
                groupDepth--;
                if (groupDepth != 0)
                {
                    context.AppendResult(c);
                }
                continue;
            }
            if (c == '\\')
            {
                // I think this is incorrect: if not grouped, only the command (and none of its arguments)
                // are collected, leading to errors if it needs arguments.
                ExecuteCommand(context);
                continue;
            }
            if (c == '$')
            {
                if (groupDepth == 0)
                {
                    throw new ArgumentException("'$' cannot be used on an ungrouped argument");
                }
                // ParseMathMode(context);
                continue;
            }

            context.AppendResult(c);
        } while (groupDepth > 0);

        int argumentLength = context.LengthResult - argumentStartI;
        return context.SliceResult(argumentStartI, argumentLength);
    }

    private static string GetCommandName(Context context)
    {
        if (context.LengthSource == 0)
        {
            throw new ArgumentException("Ran out of source for command name", nameof(context));
        }

        int commandNameStart = context.LengthResult;
        char firstC = context.ConsumeSource();
        Debug.Assert(!_escapedChars.Contains(firstC), "Does not handle chars escaped with '\\'");
        if (Char.IsLetterOrDigit(firstC))
        {
            while (Char.IsLetterOrDigit(context.PeekSource()))
            {
                context.ConsumeSource();
            }
        }
        int commandNameLength = context.LengthResult - commandNameStart;

        string commandName = context.SliceResult(commandNameStart, commandNameLength).ToString();
        context.RemoveResult(commandNameStart, commandNameLength);
        return commandName;
    }

    private static void ExecuteCommand(Context context)
    {
        if (_escapedChars.Contains(context.PeekSource()))
        {
            context.AppendResult('\\');
            context.ConsumeSource();
            return;
        }

        string commandName = GetCommandName(context);

        CommandData commandData = context.GetCommand(commandName);

        var arguments = commandData.Arguments != 0
            ? new StringSlice[commandData.Arguments]
            : Array.Empty<StringSlice>();
        bool incrementDepth = commandData.Arguments != 0 && commandData.Typeset is not null;
        if (incrementDepth)
        {
            context.IncrementGroupDepth();
            context.AddTypeset(commandData.Typeset!.Value);
        }

        for (int i = 0; i < arguments.Length; i++)
        {
            arguments[i] = LoadArgument(context);
        }

        int addedStartI = context.LengthResult;
        commandData.Command(context, arguments);
        int addedLength = context.LengthResult - addedStartI;

        context.ConsumeResult(addedLength);
        for (int i = arguments.Length - 1; i >= 0; i--)
        {
            StringSlice argument = arguments[i];
            context.RemoveResult(argument.Start, argument.Length);
        }

        if (incrementDepth)
        {
            context.DecrementGroupDepth();
        }
    }

    private static void ParseParagraphMode(Context context, bool isRoot = false)
    {
        while (true)
        {
            char c = context.PopSource();
            if (Char.IsWhiteSpace(c))
            {
                if (_spacingWhitespace.Contains(c))
                {
                    context.AppendResult(c);
                }
                continue;
            }
            if (c == '}')
            {
                if (isRoot)
                {
                    throw new ArgumentException("erroneous '}'.", nameof(context));
                }
                return;
            }
            if (c == '\\')
            {
                if (isRoot && _escapedChars.Contains(context.PeekSource()))
                {
                    context.ConsumeSource();
                    continue;
                }
                ExecuteCommand(context);
                continue;
            }
            if (c == '$')
            {
                // ParseMathMode(context);
                continue;
            }

            context.AppendResult(c);
        }
    }

    private static ReadOnlySpan<char> ParseTipaSection(ReadOnlySpan<char> segment, Context context)
    {
        StringBuilder argument = new();
        segment = GetArgument(segment, context, argument);

        for (int i = 0; i < argument.Length; i++)
        {
            char c = argument[i];
            if (c == TipaIgnoreNextChar)
            {
                context.Result.Append(argument[i]);
                context.Result.Append(argument[i + 1]);
                i++;
                continue;
            }
            if (c == '\"' && i + 1 < argument.Length && argument[i + 1] == '\"')
            {
                context.Result.Append('ˌ');
                i++;
                continue;
            }
            if (c == '|' && i + 1 < argument.Length && argument[i + 1] == '|')
            {
                context.Result.Append('‖');
                i++;
                continue;
            }
            if (Char.IsWhiteSpace(c))
            {
                context.Result.Append(c);
                continue;
            }
            if (_tipaSingleCharConversions.TryGetValue(c, out char converted))
            {
                context.Result.Append(converted);
                continue;
            }
            if (c == '$')
            {
                throw new NotImplementedException();
            }

            context.Result.Append(c);
        }

        return segment;
    }

    private static ReadOnlySpan<char> CommandSuper(ReadOnlySpan<char> segment, Context context)
    {
        StringBuilder argument = new();
        segment = GetArgument(segment, context, argument);

        for (int i = 0; i < argument.Length; i++)
        {
            context.Result.Append(argument[i] switch
            {
                'h' => 'ʰ',
                'l' => 'ˡ',
                'm' => 'ᵐ',
                'n' => 'ⁿ',
                'j' => 'ʲ',
                'w' => 'ʷ',
                'x' => 'ˣ',
                'y' => 'ʸ',
                // This command is TIPA exclusive, so the capital conversions are preemptively applied.
                'H' => 'ʱ',
                'M' => 'ᶬ',
                'N' => 'ᵑ',
                'P' => 'ˀ',
                'Q' => 'ˤ',
                'W' => 'ᵚ',
                _ => throw new ArgumentException($"Cannot raise '{argument[i]}' (limitation of encoding or not implemented)", nameof(segment))
            });
        }

        return segment;
    }

    private static ReadOnlySpan<char> CommandAsterisk(ReadOnlySpan<char> segment, Context context)
    {
        StringBuilder argument = new();
        segment = GetArgument(segment, context, argument);

        for (int i = 0; i < argument.Length; i++)
        {
            context.Result.Append(argument[i] switch
            {
                'f' => 'ⅎ',
                'k' => 'ʞ',
                'r' => 'ɹ',
                't' => 'ʇ',
                'w' => 'ʍ',
                'j' => 'ɟ',
                'n' => 'ɲ',
                'h' => 'ħ',
                'l' => 'ɬ',
                'z' => 'ɮ',
                _ => TipaIgnoreNextChar
            });
            if (context.Result[^1] == TipaIgnoreNextChar)
            {
                context.Result.Append(argument[i]);
            }
        }

        return segment;
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
