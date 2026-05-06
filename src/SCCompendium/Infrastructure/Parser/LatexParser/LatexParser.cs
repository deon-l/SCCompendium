using OneOf;
using System.Text;
using SCCompendium.Application.Parser;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser : ILatexParser
{
    private static StringSlice LoadCommandName(Context context)
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

        StringSlice commandNameSlice = context.SliceResult(commandNameStart, commandNameLength);
        context.RemoveResult(commandNameStart, commandNameLength);
        return commandNameSlice;
    }

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
                throw new ArgumentException("Expected chars for argument, but end of source", nameof(context));
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
                LoadArgument(context);
                continue;
            }

            context.AppendResult(c);
        } while (groupDepth > 0);

        int argumentLength = context.LengthResult - argumentStartI;
        return context.SliceResult(argumentStartI, argumentLength);
    }

    private static void PrepArguments(Context context, int argumentCount)
    {
        if (argumentCount <= 0)
        {
            return;
        }

        var argumentLengths = new int[argumentCount];
        for (int i = 0; i < argumentCount; i++)
        {
            argumentLengths[i] = LoadArgument(context).Length;
        }

        for (int i = argumentCount - 1; i >= 0; i--)
        {
            context.AppendSource('}');
            context.ConsumeResult(argumentLengths[i]);
        }
    }

    private static void ExecuteCommand(Context context)
    {
        if (_escapedChars.Contains(context.PeekSource()))
        {
            context.AppendResult('\\');
            context.ConsumeSource();
            return;
        }

        StringSlice commandNameSlice = LoadCommandName(context);
        string commandName = commandNameSlice.ToString();
        context.RemoveResult(commandNameSlice.Start,commandNameSlice.Length);
        commandNameSlice = default;

        CommandData commandData = context.GetCommand(commandName);

        PrepArguments(context, commandData.Arguments);

        bool incrementDepth = commandData.Arguments != 0 && commandData.Typeset is not null;
        if (incrementDepth)
        {
            context.IncrementGroupDepth();
            context.AddTypeset(commandData.Typeset!.Value);
        }

        int addedStartI = context.LengthResult;
        commandData.Command(context);
        int addedLength = context.LengthResult - addedStartI;
        context.ConsumeResult(addedLength);

        if (incrementDepth)
        {
            context.DecrementGroupDepth();
        }
    }

    private static void ParseCharacter(Context context)
    {
        char c = context.PopSource();
        if (Char.IsWhiteSpace(c))
        {
            if (_spacingWhitespace.Contains(c))
            {
                context.AppendResult(c);
            }
            return;
        }
        if (c == '{')
        {
            context.IncrementGroupDepth();
            return;
        }
        if (c == '}')
        {
            context.DecrementGroupDepth();
            return;
        }
        if (c == '\\')
        {
            ExecuteCommand(context);
            return;
        }
        if (c == '$')
        {
            // ParseMathMode(context);
            return;
        }

        context.AppendResult(c);
    }

    private static void ParseParagraphMode(Context context, bool isRoot = false)
    {
        int baseDepth = context.GroupDepth;
        while (context.GroupDepth > baseDepth && context.LengthSource > 0)
        {
            char c = context.PeekSource();
            if (isRoot && c == '\\' && _escapedChars.Contains(context.PeekSource(1)))
            {
                _ = context.PopSource();
                context.ConsumeSource();
                continue;
            }

            ParseCharacter(context);
        }

        if (isRoot && context.LengthSource > 0)
        {
            throw new ArgumentException("erroneous '}'.", nameof(context));
        }
    }

    // private static ReadOnlySpan<char> ParseTipaSection(ReadOnlySpan<char> segment, Context context)
    // {
    //     StringBuilder argument = new();
    //     segment = GetArgument(segment, context, argument);
    //
    //     for (int i = 0; i < argument.Length; i++)
    //     {
    //         char c = argument[i];
    //         if (c == TipaIgnoreNextChar)
    //         {
    //             context.Result.Append(argument[i]);
    //             context.Result.Append(argument[i + 1]);
    //             i++;
    //             continue;
    //         }
    //         if (c == '\"' && i + 1 < argument.Length && argument[i + 1] == '\"')
    //         {
    //             context.Result.Append('ˌ');
    //             i++;
    //             continue;
    //         }
    //         if (c == '|' && i + 1 < argument.Length && argument[i + 1] == '|')
    //         {
    //             context.Result.Append('‖');
    //             i++;
    //             continue;
    //         }
    //         if (Char.IsWhiteSpace(c))
    //         {
    //             context.Result.Append(c);
    //             continue;
    //         }
    //         if (_tipaSingleCharConversions.TryGetValue(c, out char converted))
    //         {
    //             context.Result.Append(converted);
    //             continue;
    //         }
    //         if (c == '$')
    //         {
    //             throw new NotImplementedException();
    //         }
    //
    //         context.Result.Append(c);
    //     }
    //
    //     return segment;
    // }

    // private static ReadOnlySpan<char> CommandSuper(ReadOnlySpan<char> segment, Context context)
    // {
    //     StringBuilder argument = new();
    //     segment = GetArgument(segment, context, argument);
    //
    //     for (int i = 0; i < argument.Length; i++)
    //     {
    //         context.Result.Append(argument[i] switch
    //         {
    //             'h' => 'ʰ',
    //             'l' => 'ˡ',
    //             'm' => 'ᵐ',
    //             'n' => 'ⁿ',
    //             'j' => 'ʲ',
    //             'w' => 'ʷ',
    //             'x' => 'ˣ',
    //             'y' => 'ʸ',
    //             // This command is TIPA exclusive, so the capital conversions are preemptively applied.
    //             'H' => 'ʱ',
    //             'M' => 'ᶬ',
    //             'N' => 'ᵑ',
    //             'P' => 'ˀ',
    //             'Q' => 'ˤ',
    //             'W' => 'ᵚ',
    //             _ => throw new ArgumentException($"Cannot raise '{argument[i]}' (limitation of encoding or not implemented)", nameof(segment))
    //         });
    //     }
    //
    //     return segment;
    // }

    // private static ReadOnlySpan<char> CommandAsterisk(ReadOnlySpan<char> segment, Context context)
    // {
    //     StringBuilder argument = new();
    //     segment = GetArgument(segment, context, argument);
    //
    //     for (int i = 0; i < argument.Length; i++)
    //     {
    //         context.Result.Append(argument[i] switch
    //         {
    //             'f' => 'ⅎ',
    //             'k' => 'ʞ',
    //             'r' => 'ɹ',
    //             't' => 'ʇ',
    //             'w' => 'ʍ',
    //             'j' => 'ɟ',
    //             'n' => 'ɲ',
    //             'h' => 'ħ',
    //             'l' => 'ɬ',
    //             'z' => 'ɮ',
    //             _ => TipaIgnoreNextChar
    //         });
    //         if (context.Result[^1] == TipaIgnoreNextChar)
    //         {
    //             context.Result.Append(argument[i]);
    //         }
    //     }
    //
    //     return segment;
    // }

    public string ParseLatexSegment(ReadOnlySpan<char> segment)
    {
        StringBuilder sb = new();
        ParseLatexSegment(segment, sb);
        return sb.ToString();
    }

    public void ParseLatexSegment(ReadOnlySpan<char> segment, StringBuilder builder)
    {
        Context context = new(segment, builder);
        ParseParagraphMode(context, isRoot: true);
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
}
