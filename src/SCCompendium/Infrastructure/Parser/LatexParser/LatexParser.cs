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
            ParseMathMode(context);
            return;
        }

        context.AppendResult(c);
    }

    private static void ParseParagraphMode(Context context, bool isRoot = false)
    {
        int baseDepth = context.GroupDepth;
        context.AddTypeset(_paragraphTypeset);
        while (context.GroupDepth >= baseDepth && context.LengthSource > 0)
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

    private static void ParseMathMode(Context context)
    {
        context.IncrementGroupDepth();
        // Todo: add Math typeset
        int baseDepth = context.GroupDepth;
        while (true)
        {
            char c = context.PeekSource();
            if (Char.IsWhiteSpace(c))
            {
                context.PopSource();
                continue;
            }
            if (c == '^')
            {
                _ = context.PopSource();
                PrepArguments(context, 1);
                while (context.PeekSource() != '}')
                {
                    c = context.PopSource();
                    context.AppendSource(c switch
                    {
                        // code value for superscript 1/2/3 is not in sequence with 4-0.
                        '1' => '¹',
                        '2' => '²',
                        '3' => '³',
                        _ => (char)(c - '0' + '⁰')
                    });
                }
                continue;
            }
            if (c == '_')
            {
                _ = context.PopSource();
                PrepArguments(context, 1);
                while (context.PeekSource() != '}')
                {
                    c = context.PopSource();
                    context.AppendSource((char)(c - '0' + '₀'));
                }
                continue;
            }
            if (c == '$')
            {
                break;
            }
            ParseCharacter(context);
        }

        if (context.GroupDepth != baseDepth)
        {
            throw new ArgumentException("Math segment has improperly closed group");
        }
        context.DecrementGroupDepth();
    }

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
}
