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


    private static readonly CommandData _commandBfData = new(0, _nullCommand, new(null, null,
            CreateReplacements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",
                "𝐀𝐁𝐂𝐃𝐄𝐅𝐆𝐇𝐈𝐉𝐊𝐋𝐌𝐍𝐎𝐏𝐐𝐑𝐒𝐓𝐔𝐕𝐖𝐗𝐘𝐙𝐚𝐛𝐜𝐝𝐞𝐟𝐠𝐡𝐢𝐣𝐤𝐥𝐦𝐧𝐨𝐩𝐪𝐫𝐬𝐭𝐮𝐯𝐰𝐱𝐲𝐳")
            ),false);

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
                    Debug.Assert(_escapedChars.Contains(context.PeekSource(1)));
                    context.ConsumeSource();
                    context.ConsumeSource();
                    continue;
                }

                context.ConsumeSource();
            }
            ParseCharacter(context);
        } while (context.GroupDepth > baseDepth);
    }

    // private static void CommandSuper(Context context)
    // {
    //     int baseDepth = context.GroupDepth;
    //     do
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
    //             _ => throw new ArgumentException(
    //                 $"Cannot raise '{argument[i]}' (limitation of encoding or not implemented)", nameof(segment))
    //         });
    //     } while (context.GroupDepth > baseDepth);
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
}
