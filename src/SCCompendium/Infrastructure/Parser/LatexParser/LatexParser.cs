using System.Text;
using SCCompendium.Application.Parser;
using SCCompendium.Domain.Exceptions;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

/// <summary>
/// Provides parsing of Latex into a string.
/// Assumes the TIPA package is being used.
/// </summary>
/// <remarks>
/// As the class parses Latex in strings, not all functionality can be represented.
/// Furthermore, various other features aren't supported.
/// Generally, this class assumes what is being parsed is "content" part of a Latex document,
/// and not things like macro/command definition, document specification, other package using, etc.
/// </remarks>
public partial class LatexParser : ILatexParser
{
    // The core logic of the parser. These methods are likely to be used (if indirectly) by
    // essentially every macro / command.

    /// <summary>
    /// Pops the next command name from the source of <paramref name="context"/>, and adds it to the result sb.
    /// It returns a <see cref="StringSlice"/> of that command name in the result sb.
    /// </summary>
    /// <remarks>
    /// A command name is either a contiguous sequence of alphabetic chars, or 1 non-alphabetic char.
    /// </remarks>
    private static StringSlice LoadCommandName(Context context)
    {
        if (context.LengthSource == 0)
        {
            throw context.CreateParseError("Ran out of source for command name");
        }

        int commandNameStart = context.LengthResult;
        char firstC = context.ConsumeSource();
        // Todo: should be IsLetter.
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

    /// <summary>
    /// Pop a command argument from <paramref name="context"/>'s source sb and onto the result sb.
    /// Returns said command argument as a <see cref="StringSlice"/>
    /// </summary>
    /// <remarks>
    /// An argument is either 1 character, or multiple enclosed in curly braces. Leading whitespace is ignored.
    /// </remarks>
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
                throw context.CreateParseError("Expected chars for argument, but end of source");
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
                    throw context.CreateParseError("unexpected '}' closing an unopened group (argument)");
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
                context.AppendResult('\\');
                LoadCommandName(context);
                continue;
            }

            context.AppendResult(c);
        } while (groupDepth > 0);

        int argumentLength = context.LengthResult - argumentStartI;
        return context.SliceResult(argumentStartI, argumentLength);
    }

    /// <summary>
    /// Preps the next <paramref name="argumentCount"/> arguments (as defined in <see cref="LoadArgument"/>)
    /// by surrounding them in curly braces and removing leading/in-between whitespace
    /// in <paramref name="context"/>'s source sb.
    /// </summary>
    private static void PrepArguments(Context context, int argumentCount, bool addEndingBrace = false)
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

        if (addEndingBrace)
        {
            context.AppendSource('}');
        }
        for (int i = argumentCount - 1; i >= 0; i--)
        {
            context.AppendSource('}');
            context.ConsumeResult(argumentLengths[i]);
            context.AppendSource('{');
        }
    }

    /// <summary>
    /// Executes the next command in <see cref="Context"/>'s source sb, consuming the appropriate command name/arguments
    /// and putting the result back onto the source sb.
    /// Also handles escaped characters by instead appending them (still escaped) to the result sb.
    /// </summary>
    /// <remarks>
    /// Assumes the leading '\' has already been consumed
    /// </remarks>
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

        int baseDepth = context.GroupDepth;
        bool incrementDepth = commandData.AutoSurroundGroup;
        if (incrementDepth)
        {
            context.IncrementGroupDepth();
        }

        PrepArguments(context, commandData.Arguments, incrementDepth);

        if (commandData.Typeset is not null)
        {
            context.AddTypeset(commandData.Typeset.Value);
        }

        int addedStartI = context.LengthResult;
        commandData.Command(context);
        int addedLength = context.LengthResult - addedStartI;
        if (addedLength > 0)
        {
            context.ConsumeResult(addedLength);
        }

        if (context.GroupDepth != baseDepth)
        {
            context.CreateParseError($"Command didn't return group depth (now {context.GroupDepth} to initial depth ({baseDepth})");
        }
    }

    /// <summary>
    /// Parses the next token (e.g. ligatures, replaced chars, commands, groups).
    /// This provides default parsing implementation.
    /// Ligatures, Commands, math mode, Group parsed results are added to <paramref name="context"/>'s source sb.
    /// Everything else (including escaped chars) are added to the result sb.
    /// </summary>
    private static void ParseCharacter(Context context)
    {
        char c = context.PopSource();

        if (context.TryGetLigature(c, context.PeekSource(), out string ligature))
        {
            _ = context.PopSource();
            foreach (char ligC in ligature.Reverse())
            {
                // append to source exclusively as em-dash ligature uses on en-dash as source
                context.AppendSource(ligC);
            }
            return;
        }
        if (context.TryGetReplacement(c, out string replacement))
        {
            foreach (char replace in replacement)
            {
                context.AppendResult(replace);
            }
            return;
        }
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
            context.AppendSource('{');
            ParseGroup(context);
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

    /// <summary>
    /// Parses the following group, then reconsumes the result back to <paramref name="context"/>'s Source.
    /// </summary>
    /// <remarks>
    /// Assumes initial <c>{</c> <i>hasn't</i> been consumed, and group depth incremented.
    /// Will do nothing otherwise.
    /// </remarks>
    private static void ParseGroup(Context context)
    {
        if (context.LengthSource == 0)
        {
            return;
        }
        if (context.PeekSource() != '{')
        {
            return;
        }

        _ = context.PopSource();
        context.IncrementGroupDepth();
        int baseDepth = context.GroupDepth;
        int baseLength = context.LengthResult;
        while (context.GroupDepth >= baseDepth)
        {
            // ParseCharacter decrements depth on '}'.
            ParseCharacter(context);
        }
        context.ConsumeResult(context.LengthResult - baseLength);
    }

    /// <summary>
    /// Parse latex segment in Paragraph Mode.
    /// Entry point to begin parsing latex if <paramref name="isRoot"/> is <see langword="true"/>.
    /// Otherwise, assumes it is parsing inside a group, where leading '{' has already been consumed.
    /// </summary>
    /// <param name="isRoot">
    /// If <see langword="true"/>, signals this is entry point for latex parsing,
    /// and that no other parsing methods parse its results.
    /// So it will do additional tasks, such as finally handling escaped characters.
    /// </param>

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
            throw context.CreateParseError("erroneous '}'.");
        }
    }

    /// <summary>
    /// Parses the section of Latex that has been denoted as being in Math Mode (by '$')
    /// </summary>
    /// <remarks>
    /// Assumes the opening '$' has already been consumed.
    /// </remarks>
    private static void ParseMathMode(Context context)
    {
        context.IncrementGroupDepth();
        context.AddTypeset(_mathModeTypeset);
        int baseDepth = context.GroupDepth;
        while (context.GroupDepth >= baseDepth)
        {
            if (context.LengthSource == 0)
            {
                throw context.CreateParseError("Unclosed math segment.");
            }

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
                Debug.Assert(context.PeekSource() == '{');
                _ = context.PopSource();
                while (context.PeekSource() != '}')
                {
                    c = context.PopSource();
                    context.AppendResult(c switch
                    {
                        // code value for superscript 1/2/3 is not in sequence with 4-0.
                        '1' => '¹',
                        '2' => '²',
                        '3' => '³',
                        _ => (char)(c - '0' + '⁰')
                    });
                }
                Debug.Assert(context.PeekSource() == '}');
                _ = context.PopSource();
                continue;
            }
            if (c == '_')
            {
                _ = context.PopSource();
                PrepArguments(context, 1);
                Debug.Assert(context.PeekSource() == '{');
                _ = context.PopSource();
                while (context.PeekSource() != '}')
                {
                    c = context.PopSource();
                    context.AppendResult((char)(c - '0' + '₀'));
                }
                Debug.Assert(context.PeekSource() == '}');
                _ = context.PopSource();
                continue;
            }
            if (c == '$')
            {
                _ = context.PopSource();
                break;
            }
            ParseCharacter(context);
        }

        if (context.GroupDepth != baseDepth)
        {
            throw context.CreateParseError("Math segment has improperly closed group");
        }
        context.DecrementGroupDepth();
    }

    /// <summary>
    /// Parse the latex <paramref name="segment"/> into a string and return it.
    /// </summary>
    public string ParseLatexSegment(ReadOnlySpan<char> segment)
    {
        StringBuilder sb = new();
        ParseLatexSegment(segment, sb);
        return sb.ToString();
    }

    /// <summary>
    /// Parse the latex <paramref name="segment"/>, appending the result onto <paramref name="builder"/>.
    /// </summary>
    public void ParseLatexSegment(ReadOnlySpan<char> segment, StringBuilder builder)
    {
        Context context = new(segment, builder);
        try
        {
            ParseParagraphMode(context, isRoot: true);
        }
        catch (LatexParsingException e)
        {
            throw new LatexParsingException(e, segment.ToString(), e.ErrorMessage,
                segment.Length - context.LengthSource);
        }
    }
}
