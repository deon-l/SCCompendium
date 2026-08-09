namespace SCCompendium.Infrastructure.Parser.LatexParser;

// This file stores all other commands and their associated data.
// Also holds a few helper methods in that regard.
// Note that some commands are implemented inline in static ctor in LatexParser.Constants.cs
public partial class LatexParser
{
    // Holds the data (including methods) for creating commands.

    /// <summary>Creates a <see cref="CommandData"/> representing a command that stands in for a character.</summary>
    private static CommandData NewSymbolicCommandData(char c) =>
        new CommandData(0, context => context.AppendResult(c), null, false);
    /// <inheritdoc cref="NewSymbolicCommandData(char)"/>
    private static CommandData NewSymbolicCommandData(string str) =>
        new CommandData(0, context => context.AppendResult(str), null, false);

    /// <summary>Command implementation that does nothing.</summary>
    private static readonly Action<Context> _nullCommand = context => { };
    /// <summary>Command data for a command that does nothing.</summary>
    private static readonly CommandData _nullCommandData = new(
        0, _nullCommand, null, false);

    private static Action<Context> DiacriticApplierMethod(string diacritic) => (context) =>
    {
        int baseDepth = context.GroupDepth;
        while (context.GroupDepth >= baseDepth)
        {
            int oldLength = context.LengthResult;
            ParseCharacter(context);
            int added = context.LengthResult - oldLength;
            if (added == 0)
            {
                continue;
            }

            context.ConsumeResult(added);
            for (int i = 0; i < added; i++)
            {
                context.ConsumeSource();
                context.AppendResult(diacritic);
            }
        }
    };

    private static readonly CommandData _commandHSpaceData = new(
        0, CommandHSpace, null, false);
    private const double PtPerSpace = 6.5;
    private static void CommandHSpace(Context context)
    {
        PopWhitespace(context);
        bool hasBrace = false;
        if (context.PeekSource() == '{')
        {
            hasBrace = true;
            context.PopSource();
        }
        PopWhitespace(context);

        double ptSize = 0;
        if (!Char.IsAsciiDigit(context.PeekSource()))
        {
            throw context.CreateParseError("Expected a number.");
        }
        do
        {
            ptSize *= 10;
            ptSize += context.PopSource() - '0';
        } while (Char.IsAsciiDigit(context.PeekSource()));
        if (context.PeekSource() == '.')
        {
            context.PopSource();
            for (double digitPlace = 0.1; Char.IsAsciiDigit(context.PeekSource()); digitPlace /= 10)
            {
                ptSize += digitPlace * (context.PopSource() - '0');
            }
        }

        PopWhitespace(context);
        if (context.LengthSource < 2 || !Char.IsAsciiLetter(context.PeekSource(0)) ||
            !Char.IsAsciiLetter(context.PeekSource(1)))
        {
            throw context.CreateParseError("Expected a 2 char measurement unit");
        }
        Span<char> measurementUnit = stackalloc char[2];
        measurementUnit[0] = context.PopSource();
        measurementUnit[1] = context.PopSource();
        ptSize *= measurementUnit switch
        {
            "pt" => 1,
            "mm" => 1 / 0.3515,
            "cm" => 1 / 0.03515,
            "in" => 72.27,
            "ex" => 4.5,
            "em" => 10,
            "mu" => 10 / 18.0,
            "sp" => 1 / 65536.0,
            _ => throw context.CreateParseError("Not a char measurement unit.")
        };
        if (ptSize <= 0)
        {
            ptSize = 0.1;
        }

        if (hasBrace)
        {
            PopWhitespace(context);
            if (context.PeekSource() != '}')
            {
                throw context.CreateParseError("Expected a closing brace after specs");
            }
            context.PopSource();
        }
        Console.WriteLine(context.GroupDepth);

        context.AppendResult(new String(' ', (int)Math.Ceiling(ptSize / PtPerSpace)));
    }

    private static readonly CommandData _commandBfData = new(0,
        context => _ = Char.IsWhiteSpace(context.PeekSource()) ? context.PopSource() : '\0',
        new Typeset()
            .AddLigatures("𝐴𝐵𝐶𝐷𝐸𝐹𝐺𝐻𝐼𝐽𝐾𝐿𝑀𝑁𝑂𝑃𝑄𝑅𝑆𝑇𝑈𝑉𝑊𝑋𝑌𝑍𝑎𝑏𝑐𝑑𝑒𝑓𝑔𝑖𝑗𝑘𝑙𝑚𝑛𝑜𝑝𝑞𝑟𝑠𝑡𝑢𝑣𝑤𝑥𝑦𝑧", "𝑨𝑩𝑪𝑫𝑬𝑭𝑮𝑯𝑰𝑱𝑲𝑳𝑴𝑵𝑶𝑷𝑸𝑹𝑺𝑻𝑼𝑽𝑾𝑿𝒀𝒁𝒂𝒃𝒄𝒅𝒆𝒇𝒈𝒊𝒋𝒌𝒍𝒎𝒏𝒐𝒑𝒒𝒓𝒔𝒕𝒖𝒗𝒘𝒙𝒚𝒛")
            .AddReplacements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ℎ",
                "𝐀𝐁𝐂𝐃𝐄𝐅𝐆𝐇𝐈𝐉𝐊𝐋𝐌𝐍𝐎𝐏𝐐𝐑𝐒𝐓𝐔𝐕𝐖𝐗𝐘𝐙𝐚𝐛𝐜𝐝𝐞𝐟𝐠𝐡𝐢𝐣𝐤𝐥𝐦𝐧𝐨𝐩𝐪𝐫𝐬𝐭𝐮𝐯𝐰𝐱𝐲𝐳𝟎𝟏𝟐𝟑𝟒𝟓𝟔𝟕𝟖𝟗𝒉"),
         false);
    private static readonly CommandData _commandTextBf = new(1,
        con => ParseParagraphMode(con), _commandBfData.Typeset);

    private static readonly CommandData _itCommandData = new(0,
        context => _ = Char.IsWhiteSpace(context.PeekSource()) ? context.PopSource() : '\0',
        new Typeset().AddLigatures("𝐀𝐁𝐂𝐃𝐄𝐅𝐆𝐇𝐈𝐉𝐊𝐋𝐌𝐍𝐎𝐏𝐐𝐑𝐒𝐓𝐔𝐕𝐖𝐗𝐘𝐙𝐚𝐛𝐜𝐝𝐞𝐟𝐠𝐡𝐢𝐣𝐤𝐥𝐦𝐧𝐨𝐩𝐪𝐫𝐬𝐭𝐮𝐯𝐰𝐱𝐲𝐳", "𝑨𝑩𝑪𝑫𝑬𝑭𝑮𝑯𝑰𝑱𝑲𝑳𝑴𝑵𝑶𝑷𝑸𝑹𝑺𝑻𝑼𝑽𝑾𝑿𝒀𝒁𝒂𝒃𝒄𝒅𝒆𝒇𝒈𝒉𝒊𝒋𝒌𝒍𝒎𝒏𝒐𝒑𝒒𝒓𝒔𝒕𝒖𝒗𝒘𝒙𝒚𝒛")
            .AddReplacements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefgijklmnopqrstuvwxyz",
                "𝐴𝐵𝐶𝐷𝐸𝐹𝐺𝐻𝐼𝐽𝐾𝐿𝑀𝑁𝑂𝑃𝑄𝑅𝑆𝑇𝑈𝑉𝑊𝑋𝑌𝑍𝑎𝑏𝑐𝑑𝑒𝑓𝑔𝑖𝑗𝑘𝑙𝑚𝑛𝑜𝑝𝑞𝑟𝑠𝑡𝑢𝑣𝑤𝑥𝑦𝑧"),
         false);
    private static readonly CommandData _textitCommandData = new(1,
        con => ParseParagraphMode(con), _itCommandData.Typeset);


    private static readonly CommandData _commandTtData = new(0,
        context => _ = Char.IsWhiteSpace(context.PeekSource()) ? context.PopSource() : '\0',
     new Typeset()
         .AddReplacements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",
            "𝙰𝙱𝙲𝙳𝙴𝙵𝙶𝙷𝙸𝙹𝙺𝙻𝙼𝙽𝙾𝙿𝚀𝚁𝚂𝚃𝚄𝚅𝚆𝚇𝚈𝚉𝚊𝚋𝚌𝚍𝚎𝚏𝚐𝚑𝚒𝚓𝚔𝚕𝚖𝚗𝚘𝚙𝚚𝚛𝚜𝚝𝚞𝚟𝚠𝚡𝚢𝚣𝟶𝟷𝟸𝟹𝟺𝟻𝟼𝟽𝟾𝟿"), false);

    private static readonly CommandData _textellipsisCommandData = NewSymbolicCommandData("…");

    private static readonly CommandData _commandLatexData = NewSymbolicCommandData("LaTeX");

    private static readonly CommandData _commandTextPolHook = new(1, DiacriticApplierMethod("̨") );

    private static readonly CommandData _textbardotlessjCommandData = NewSymbolicCommandData("ɟ");

    private static readonly CommandData _textcrhCommandData = NewSymbolicCommandData("ħ");

    private static readonly CommandData _textbeltlCommandData = NewSymbolicCommandData("ɬ");

    private static readonly CommandData _textquotedblleftCommandData = NewSymbolicCommandData("“");

    private static readonly CommandData _textltailnCommandData = NewSymbolicCommandData("ɲ");

    private static readonly CommandData _textlessCommandData = NewSymbolicCommandData("<");

    private static readonly CommandData _textgreaterCommandData = NewSymbolicCommandData(">");

    private static CommandData _commandApostropheData = new(
        1, DiacriticApplierMethod("́"));

    private static CommandData _commandCData = new(1, DiacriticApplierMethod("̧"));

    private static CommandData _commandIData = NewSymbolicCommandData('ı');

    private static CommandData _dCommandData = new(1, DiacriticApplierMethod("̣"));

    // ReSharper disable once InconsistentNaming
    private static readonly CommandData _OCommandData = NewSymbolicCommandData("∅");

    private static readonly CommandData _aeCommandData = NewSymbolicCommandData("æ");

    private static readonly CommandData _quoteCommandData = new(1, DiacriticApplierMethod("̈"));



    /// <summary>
    /// Parsing for adding IPA characters quickly
    /// </summary>
    /// <seealso cref="TipaIgnoreNextChar"/>
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
        } while (context.GroupDepth >= baseDepth);
    }

    /// <summary>
    /// Convert its argument into superscript text.
    /// </summary>
    private static void CommandSuper(Context context)
    {
        int baseDepth = context.GroupDepth;
        do
        {
            int oldLength = context.LengthResult;
            ParseCharacter(context);
            int added = context.LengthResult - oldLength;
            if (added == 0)
            {
                continue;
            }
            context.ConsumeResult(added);
            for (int i = 0; i < added; i++)
            {
                char c = context.PopSource();
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
                    _ => throw context.CreateParseError(
                        $"Cannot raise '{c}' (limitation of encoding or not implemented)")
                });
            }
        } while (context.GroupDepth >= baseDepth);
    }

    /// <summary>
    /// Transforms a select few chars, and marks the rest to not be replaced by Tipa defined replacements
    /// </summary>
    private static void CommandAsterisk(Context context)
    {
        int baseDepth = context.GroupDepth;
        do
        {
            int oldLength = context.LengthResult;
            ParseCharacter(context);
            int added = context.LengthResult - oldLength;
            if (added == 0)
            {
                continue;
            }
            context.ConsumeResult(added);
            for (int i = 0; i < added; i++)
            {
                char c = context.PopSource();
                if (c == '\\')
                {
                    context.AppendResult(TipaIgnoreNextChar);
                    context.AppendResult('\\');
                    i += LoadCommandName(context).Length;
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
            }
        } while (context.GroupDepth >= baseDepth);
    }

    private static CommandData _commandTildeData = new(
        1, DiacriticApplierMethod("̃"));

    private static CommandData _ipaCommandTildeData = new(0,
        IpaCommandTilde, null, false);
    private static CommandData _ipaSubcommandTildeDotData = new(1,
        DiacriticApplierMethod("̇̃"));
    private static CommandData _ipaSubcommandSubscriptTildeData = new(1, DiacriticApplierMethod("̰"));
    private static void IpaCommandTilde(Context context)
    {
        char c = context.PopSource();
        switch (c)
        {
            case '.':
                ExecuteCommand(context, _ipaSubcommandTildeDotData);
                break;
            case '*':
                ExecuteCommand(context, _ipaSubcommandSubscriptTildeData);
                break;
            default:
                context.AppendSource(c);
                ExecuteCommand(context, _commandTildeData);
                break;
        }
    }

    private static CommandData _caretCommandData = new(1, DiacriticApplierMethod("̂"));
    private static CommandData _textsubcircumCommandData = new(1, DiacriticApplierMethod("̭"));
    private static CommandData _textcircumdotCommandData = new(1, DiacriticApplierMethod("̇̂"));
    private static CommandData _caretIpaCommandData = new(0, CaretIpaCommand, null, false);
    private static void CaretIpaCommand(Context context)
    {
        char c = context.PopSource();
        Console.WriteLine(c);
        Console.WriteLine(context.PeekSource());
        switch (c)
        {
            case '.':
                ExecuteCommand(context, _textcircumdotCommandData);
                break;
            case '*':
                ExecuteCommand(context, _textsubcircumCommandData);
                break;
            default:
                context.AppendSource(c);
                ExecuteCommand(context, _caretCommandData);
                break;
        }
    }

    private static readonly CommandData _tIpaCommandData = new(1, TIpaCommand);
    // ReSharper disable once InconsistentNaming
    private static void TIpaCommand(Context context)
    {
        int baseDepth = context.GroupDepth;
        bool isFirst = true;
        while (context.GroupDepth >= baseDepth)
        {
            int oldLength = context.LengthResult;
            ParseCharacter(context);
            int added = context.LengthResult - oldLength;
            if (added == 0)
            {
                continue;
            }

            context.ConsumeResult(added);
            for (int i = 0; i < added; i++)
            {
                if (!isFirst)
                {
                    context.AppendResult('͡');
                }
                context.ConsumeSource();
                isFirst = false;
            }
        }
    }
}
