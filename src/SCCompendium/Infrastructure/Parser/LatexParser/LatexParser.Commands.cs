namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private static readonly Action<Context> _nullCommand = context => { };

    private static Dictionary<char, string> CreateReplacements(string input, string output)
    {
        Debug.Assert(output.Length % input.Length == 0);
        int charsPerReplace = output.Length / input.Length;
        Dictionary<char, string> replacements = new();
        for (int i = 0; i < input.Length; i++)
        {
            replacements.Add(input[i], output.Substring(i * charsPerReplace, charsPerReplace));
        }

        return replacements;
    }


    private static readonly CommandData _commandBfData = new(0,
        context => _ = Char.IsWhiteSpace(context.PeekSource()) ? context.PopSource() : '\0',
        new(null, null,
            CreateReplacements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",
                "𝐀𝐁𝐂𝐃𝐄𝐅𝐆𝐇𝐈𝐉𝐊𝐋𝐌𝐍𝐎𝐏𝐐𝐑𝐒𝐓𝐔𝐕𝐖𝐗𝐘𝐙𝐚𝐛𝐜𝐝𝐞𝐟𝐠𝐡𝐢𝐣𝐤𝐥𝐦𝐧𝐨𝐩𝐪𝐫𝐬𝐭𝐮𝐯𝐰𝐱𝐲𝐳")
        ), false);

    private static void CommandTextIpa(Context context)
    {
        int baseDepth = context.GroupDepth;
        do
        {
            char c = context.PeekSource();
            if (c == TipaIgnoreNextChar)
            {
                _ = context.PopSource();
                c = context.PeekSource();
                if (c == '\\')
                {
                    context.ConsumeSource();
                    LoadCommandName(context);
                    continue;
                }

                context.ConsumeSource();
                continue;
            }
            ParseCharacter(context);
        } while (context.GroupDepth > baseDepth);
    }

    private static void CommandSuper(Context context)
    {
        int baseDepth = context.GroupDepth;
        do
        {
            char c = context.PopSource();
            if (c == '\\')
            {
                if (!_escapedChars.TryGetValue(context.PeekSource(), out c))
                {
                    ExecuteCommand(context);
                    continue;
                }
            }
            if (c == '{')
            {
                context.IncrementGroupDepth();
                continue;
            }
            if (c == '}')
            {
                context.DecrementGroupDepth();
                continue;
            }

            if (c == '$')
            {
                throw new NotSupportedException("Command Super doesn't support math mode");
            }
            
            context.AppendResult(c switch
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
                _ => throw new ArgumentException(
                    $"Cannot raise '{c}' (limitation of encoding or not implemented)", nameof(context))
            });
        } while (context.GroupDepth > baseDepth);
    }

    private static void CommandAsterisk(Context context)
    {
        int baseDepth = context.GroupDepth;
        do
        {
            char c = context.PopSource();
            if (c == '\\')
            {
                context.AppendResult(TipaIgnoreNextChar);
                context.AppendResult('\\');
                LoadCommandName(context);
                continue;
            }
            if (c == '{')
            {
                context.IncrementGroupDepth();
                context.AppendResult(c);
                continue;
            }
            if (c == '}')
            {
                context.DecrementGroupDepth();
                context.AppendResult(c);
                continue;
            }
            if (c == '$')
            {
                int startLength = context.LengthResult;
                ParseMathMode(context);
                int length = context.LengthResult - startLength;

                context.AppendSource('}');
                context.ConsumeResult(length);
                context.AppendSource('{');
                CommandAsterisk(context);
                continue;
            }

            char replace = c switch
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
            };

            context.AppendResult(replace);
            if (replace == TipaIgnoreNextChar)
            {
                context.AppendResult(c);
            }
        } while (context.GroupDepth > baseDepth);
    }
}
