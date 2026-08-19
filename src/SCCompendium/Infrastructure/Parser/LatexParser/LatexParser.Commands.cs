namespace SCCompendium.Infrastructure.Parser.LatexParser;

// This file stores all other commands and their associated data.
// Also holds a few helper methods in that regard.
public partial class LatexParser
{
    // Holds the data (including methods) for creating commands.

    /// <summary>Creates a <see cref="CommandData"/> representing a command that stands in for a character.</summary>
    private static CommandData NewSymbolicCommandData(char c) =>
        new CommandData(0, context =>
        {
            context.AppendResult(c);
            if (Char.IsWhiteSpace(context.PeekSource())) _ = context.PopSource();
        }, null, false);

    /// <inheritdoc cref="NewSymbolicCommandData(char)"/>
    private static CommandData NewSymbolicCommandData(string str) =>
        new CommandData(0, context =>
        {
            context.AppendResult(str);
            if (Char.IsWhiteSpace(context.PeekSource())) _ = context.PopSource();
        }, null, false);

    /// <summary>Command implementation that does nothing.</summary>
    private static readonly Action<Context> _nullCommand = _ => { };

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

    /// <summary>
    /// Parses and gets a length measurement specifier, returning the size in pt.
    /// </summary>
    private static double GetMeasurement(Context context)
    {
        PopWhitespace(context);
        bool hasBrace = false;
        if (context.PeekSource() == '{')
        {
            hasBrace = true;
            context.PopSource();
        }

        PopWhitespace(context);
        bool isNegative = false;
        if (context.PeekSource() == '-')
        {
            isNegative = true;
            _ = context.PopSource();
        }

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

        if (isNegative)
        {
            ptSize *= -1;
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

        if (hasBrace)
        {
            PopWhitespace(context);
            if (context.PeekSource() != '}')
            {
                throw context.CreateParseError("Expected a closing brace after specs");
            }

            context.PopSource();
        }

        return ptSize;
    }

    private static readonly CommandData _hspaceCommandData = new(
        0, CommandHSpace, null, false);

    private const double PtPerSpace = 6.5;

    private static void CommandHSpace(Context context)
    {
        double ptSize = GetMeasurement(context);
        if (ptSize < 0)
        {
            Console.Error.WriteLine("Command 'hspace' current cannot handle negative values");
        }

        context.AppendResult(new String(' ', (int)Math.Ceiling(ptSize / PtPerSpace)));
    }

    private static readonly CommandData _raiseboxCommandData = new(1, CommandRaisebox);
    private static void CommandRaisebox(Context context)
    {
        double ptSize = GetMeasurement(context);
        if (Math.Abs(ptSize) > 4)
        {
            Console.Error.WriteLine("Latex Command 'raisebox' currently does nothing, but is invoked with a significant vertical displacement.");
        }
    }

    private static readonly CommandData _bfCommandData = new(0,
        context => _ = Char.IsWhiteSpace(context.PeekSource()) ? context.PopSource() : '\0',
        new Typeset()
            .AddLigatures(
                "𝐴𝐵𝐶𝐷𝐸𝐹𝐺𝐻𝐼𝐽𝐾𝐿𝑀𝑁𝑂𝑃𝑄𝑅𝑆𝑇𝑈𝑉𝑊𝑋𝑌𝑍𝑎𝑏𝑐𝑑𝑒𝑓𝑔𝑖𝑗𝑘𝑙𝑚𝑛𝑜𝑝𝑞𝑟𝑠𝑡𝑢𝑣𝑤𝑥𝑦𝑧",
                "𝑨𝑩𝑪𝑫𝑬𝑭𝑮𝑯𝑰𝑱𝑲𝑳𝑴𝑵𝑶𝑷𝑸𝑹𝑺𝑻𝑼𝑽𝑾𝑿𝒀𝒁𝒂𝒃𝒄𝒅𝒆𝒇𝒈𝒊𝒋𝒌𝒍𝒎𝒏𝒐𝒑𝒒𝒓𝒔𝒕𝒖𝒗𝒘𝒙𝒚𝒛")
            .AddReplacements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ℎ",
                "𝐀𝐁𝐂𝐃𝐄𝐅𝐆𝐇𝐈𝐉𝐊𝐋𝐌𝐍𝐎𝐏𝐐𝐑𝐒𝐓𝐔𝐕𝐖𝐗𝐘𝐙𝐚𝐛𝐜𝐝𝐞𝐟𝐠𝐡𝐢𝐣𝐤𝐥𝐦𝐧𝐨𝐩𝐪𝐫𝐬𝐭𝐮𝐯𝐰𝐱𝐲𝐳𝟎𝟏𝟐𝟑𝟒𝟓𝟔𝟕𝟖𝟗𝒉"),
        false);

    private static readonly CommandData _textbfCommandData = new(1,
        con => ParseParagraphMode(con), _bfCommandData.Typeset);

    private static readonly CommandData _itCommandData = new(0,
        context => _ = Char.IsWhiteSpace(context.PeekSource()) ? context.PopSource() : '\0',
        new Typeset()
            .AddLigatures(
                "𝐀𝐁𝐂𝐃𝐄𝐅𝐆𝐇𝐈𝐉𝐊𝐋𝐌𝐍𝐎𝐏𝐐𝐑𝐒𝐓𝐔𝐕𝐖𝐗𝐘𝐙𝐚𝐛𝐜𝐝𝐞𝐟𝐠𝐡𝐢𝐣𝐤𝐥𝐦𝐧𝐨𝐩𝐪𝐫𝐬𝐭𝐮𝐯𝐰𝐱𝐲𝐳",
                "𝑨𝑩𝑪𝑫𝑬𝑭𝑮𝑯𝑰𝑱𝑲𝑳𝑴𝑵𝑶𝑷𝑸𝑹𝑺𝑻𝑼𝑽𝑾𝑿𝒀𝒁𝒂𝒃𝒄𝒅𝒆𝒇𝒈𝒉𝒊𝒋𝒌𝒍𝒎𝒏𝒐𝒑𝒒𝒓𝒔𝒕𝒖𝒗𝒘𝒙𝒚𝒛")
            .AddReplacements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefgijklmnopqrstuvwxyz",
                "𝐴𝐵𝐶𝐷𝐸𝐹𝐺𝐻𝐼𝐽𝐾𝐿𝑀𝑁𝑂𝑃𝑄𝑅𝑆𝑇𝑈𝑉𝑊𝑋𝑌𝑍𝑎𝑏𝑐𝑑𝑒𝑓𝑔𝑖𝑗𝑘𝑙𝑚𝑛𝑜𝑝𝑞𝑟𝑠𝑡𝑢𝑣𝑤𝑥𝑦𝑧"),
        false);

    private static readonly CommandData _textitCommandData = new(1,
        con => ParseParagraphMode(con), _itCommandData.Typeset);


    private static readonly CommandData _ttCommandData = new(0,
        context => _ = Char.IsWhiteSpace(context.PeekSource()) ? context.PopSource() : '\0',
        new Typeset()
            .AddReplacements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",
                "𝙰𝙱𝙲𝙳𝙴𝙵𝙶𝙷𝙸𝙹𝙺𝙻𝙼𝙽𝙾𝙿𝚀𝚁𝚂𝚃𝚄𝚅𝚆𝚇𝚈𝚉𝚊𝚋𝚌𝚍𝚎𝚏𝚐𝚑𝚒𝚓𝚔𝚕𝚖𝚗𝚘𝚙𝚚𝚛𝚜𝚝𝚞𝚟𝚠𝚡𝚢𝚣𝟶𝟷𝟸𝟹𝟺𝟻𝟼𝟽𝟾𝟿")
            .AddReplacements(@"/\:;,.", "\u2009/\u2009\u2009\\\u2009\u2009:\u2009\u2009;\u2009\u2009,\u2009\u2009.\u2009"),
        false);

    private static readonly CommandData _textttComandData = new(1, DefaultParse, _ttCommandData.Typeset);

    private static readonly CommandData _scCommandData = new(0,
        context => _ = Char.IsWhiteSpace(context.PeekSource()) ? context.PopSource() : '\0',
        new Typeset()
            .AddReplacements("ABCDEFGHIJKLMNOPQRSTUVWYZ", "ᴀʙᴄᴅᴇꜰɢʜɪᴊᴋʟᴍɴᴏᴘꞯʀꜱᴛᴜᴠᴡʏᴢ"),
            false);

    private static readonly CommandData _clearpageCommandData =
        new(0, c => Console.Error.WriteLine("cmd clearpage currently does nothing."));

    private static readonly CommandData _tabCommandData = NewSymbolicCommandData("\e\t");

    private static readonly CommandData _textellipsisCommandData = NewSymbolicCommandData("…");

    private static readonly CommandData _capLaTeXCommandData = NewSymbolicCommandData("LaTeX");

    private static readonly CommandData _textpolhookCommandData = new(1, DiacriticApplierMethod("̨"));

    private static readonly CommandData _textbardotlessjCommandData = NewSymbolicCommandData("ɟ");

    private static readonly CommandData _textcrhCommandData = NewSymbolicCommandData("ħ");

    private static readonly CommandData _textbeltlCommandData = NewSymbolicCommandData("ɬ");

    private static readonly CommandData _textquotedblleftCommandData = NewSymbolicCommandData("“");

    private static readonly CommandData _textltailnCommandData = NewSymbolicCommandData("ɲ");

    private static readonly CommandData _textlessCommandData = NewSymbolicCommandData("<");

    private static readonly CommandData _textgreaterCommandData = NewSymbolicCommandData(">");

    private static readonly CommandData _textturnmrleg = NewSymbolicCommandData("ɰ");

    private static readonly CommandData _textsubarchCommandData = new(1, DiacriticApplierMethod("̯"));

    private static readonly CommandData _textsubsquareCommandData = new(1, DiacriticApplierMethod("̻"));

    private static readonly CommandData _textturnwCommandData = NewSymbolicCommandData("ʍ");

    private static readonly CommandData _textasciitildeCommandData = NewSymbolicCommandData("\e~");

    private static readonly CommandData _textcornerCommandData = NewSymbolicCommandData("̚");

    private static readonly CommandData _textlyoghligCommandData = NewSymbolicCommandData("ɮ");

    private static readonly CommandData _textctzCommandData = NewSymbolicCommandData("ʑ");

    private static readonly CommandData _textsoftsignCommandData = NewSymbolicCommandData("Ь");

    private static readonly CommandData _texthardsignCommandData = NewSymbolicCommandData("Ъ");

    private static readonly CommandData _textctnCommandData = NewSymbolicCommandData('ȵ');

    private static readonly CommandData _textleftarrowCommandData = NewSymbolicCommandData('←');

    private static readonly CommandData _textrightarrowCommandData = NewSymbolicCommandData('→');

    private static readonly CommandData _textdoublebarpipeCommandData = NewSymbolicCommandData('ⱡ');

    private static readonly CommandData _textquoteleftCommandData = NewSymbolicCommandData('“');

    private static readonly CommandData _textrhoticityCommandData = NewSymbolicCommandData('˞');

    private static readonly CommandData _textturnaCommandData = NewSymbolicCommandData('ɐ');

    private static readonly CommandData _textlhtlongiCommandData = NewSymbolicCommandData('ɿ');

    private static readonly CommandData _textraisevibyiCommandData = NewSymbolicCommandData('ʅ');

    private static readonly CommandData _textbackslashCommandData = NewSymbolicCommandData("\e\\");

    private static readonly CommandData _jCommandData = NewSymbolicCommandData("ȷ");

    private static readonly CommandData _lCommandData = NewSymbolicCommandData("ł");

    private static readonly CommandData _capitalLCommandData = NewSymbolicCommandData("ł".ToUpper());

    private static readonly CommandData _symApostropheCommandData = new(
        1, DiacriticApplierMethod("́"));

    private static readonly CommandData _cCommandData = new(1, DiacriticApplierMethod("̧"));

    private static readonly CommandData _iCommandData = NewSymbolicCommandData('ı');

    private static readonly CommandData _dCommandData = new(1, DiacriticApplierMethod("̣"));

    private static readonly CommandData _capitalOCommandData = NewSymbolicCommandData("Ø");

    private static readonly CommandData _oCommandData = NewSymbolicCommandData("ø");

    private static readonly CommandData _aeCommandData = NewSymbolicCommandData("æ");

    private static readonly CommandData _oeCommandData = NewSymbolicCommandData("œ");

    private static readonly CommandData _capitalAACommandData = NewSymbolicCommandData("Å");

    private static readonly CommandData _aaCommandData = NewSymbolicCommandData("å");

    private static readonly CommandData _symQuoteCommandData = new(1, DiacriticApplierMethod("̈"));

    private static readonly CommandData _symPeriodCommandData = new(1, DiacriticApplierMethod("̇"));

    private static readonly CommandData _symGraveAccentCommandData = new(1, DiacriticApplierMethod("̀"));

    private static readonly CommandData _symMinusCommandData =
        new(0, _ => Console.Error.WriteLine("command '\\-' does nothing."));

    private static readonly CommandData _symCommaCommandData = NewSymbolicCommandData("\u2009"); // thin space

    private static readonly CommandData _symBackslashCommandData = NewSymbolicCommandData("\e\n");

    private const string TipaInput  = ":;\"0123456789@ABCDEFGHIJKLMNOPQRSTUVWXYZ|";
    private const string TipaOutput = "ː\u02D1ˈʉɨʌɜɥɐɒɤɵɘəɑβɕðɛɸɣɦɪʝʁʎɱŋɔʔʕɾʃθʊʋɯχʏʒ|";

    private static readonly Dictionary<char, string> _tipaSingleCharConversions =
        TipaInput.Zip(TipaOutput)
            .Select(pair => (pair.First, pair.Second.ToString()))
            .ToDictionary();

    private static readonly CommandData _textipaCommandData = new(1, CommandTextIpa,
        new Typeset(new(),
                new() { { ('|', '|'), "‖" }, { ('\"', '\"'), "ˌ" } },
                _tipaSingleCharConversions)
            .AddReplacements("ᴴᴹᴺᴾꟴᵂ", "ʱᶬᵑˀˤᵚ")
    );

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

    private static readonly CommandData _sTipaCommandData = new(1, DiacriticApplierMethod("̩"));

    private static readonly CommandData _textsuperscriptCommandData = new(1, DefaultParse, new Typeset()
        .AddReplacements("ABCDEFGHIJKLMNOPQRTUVW", "ᴬᴮꟲᴰᴱꟳᴳᴴᴵᴶᴷᴸᴹᴺᴼᴾꟴᴿᵀᵁⱽᵂ")
        .AddReplacements("aɐɑᴂɒbβcɕdðeəɛɜfghɦɥiɨɪjɟʝklɭɫʟmɱnɲɳɴoœɔprɹɻʁsʂʃtθuʉʊvʋʌwɯɰxyγɣzʐʑʒʕ",
            "ᵃᵄᵅᵆᶛᵇᵝᶜᶝᵈᶞᵉᵊᵋᵌᶠᵍʰʱᶣⁱᶤᶦʲᶡᶨᵏˡᶩꭞᶫᵐᶬⁿᶮᶯᶰᵒꟹᵓᵖʳʴʵʶˢᶳᶴᵗᶿᵘᶶᶷᵛᶹᶺʷᵚᶭˣʸᵞˠᶻᶼᶽᶾˤ"));

    private static readonly CommandData _superTipaCommandData = _textsuperscriptCommandData;

    private static readonly CommandData _symAsteriskTipaCommandData = new(1, TipaCommandSymAsterisk);
    /// <summary>
    /// Transforms a select few chars, and marks the rest to not be replaced by Tipa defined replacements
    /// </summary>
    private static void TipaCommandSymAsterisk(Context context)
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

    private static readonly CommandData _symSemicolonTipaCommandData = new(1, DefaultParse, new Typeset()
        .AddReplacements("ABCDEFGHIJKLMNOPQRSTUVWYZ", "ᴀʙᴄᴅᴇꜰɢʜɪᴊᴋʟᴍɴᴏᴘꞯʀꜱᴛᴜᴠᴡʏᴢ"));

    private static readonly CommandData _symColonTipaCommandData = new(1, DefaultParse, new Typeset()
        .AddReplacements("tdsznlr", "ʈɖʂʐɳɭɽ"));

    private static readonly CommandData _symExclamationPointTipaCommandData = new(1, DefaultParse, new Typeset()
        .AddReplacements("bdɖjgGo", "ɓɗᶑʄɠʛʘ"));

    private static readonly CommandData _symTildeCommandData = new(
        1, DiacriticApplierMethod("̃"));

    private static readonly CommandData _symTildeTipaCommandData = new(0,
        TipaCommandSymTilde, null, false);

    private static readonly CommandData _ipaSubcommandTildeDotData = new(1,
        DiacriticApplierMethod("̇̃"));

    private static readonly CommandData _ipaSubcommandSubscriptTildeData = new(1, DiacriticApplierMethod("̰"));

    private static void TipaCommandSymTilde(Context context)
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
                ExecuteCommand(context, _symTildeCommandData);
                break;
        }
    }

    private static readonly CommandData _symCaretCommandData = new(1, DiacriticApplierMethod("̂"));
    private static readonly CommandData _textsubcircumCommandData = new(1, DiacriticApplierMethod("̭"));
    private static readonly CommandData _textcircumdotCommandData = new(1, DiacriticApplierMethod("̇̂"));
    private static readonly CommandData _symCaretTipaCommandData = new(0, TipaCommandSymCaret, null, false);

    private static void TipaCommandSymCaret(Context context)
    {
        char c = context.PopSource();
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
                ExecuteCommand(context, _symCaretCommandData);
                break;
        }
    }

    private static readonly CommandData _tTipaCommandData = new(1, TipaCommandT);

    private static void TipaCommandT(Context context)
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

    private static readonly CommandData _textltildeCommandData = NewSymbolicCommandData("ɫ");
    private static readonly CommandData _textroundcapCommandData = new(1, DiacriticApplierMethod("̑"));
    private static readonly CommandData _textsubbridgeCommandData = new(1, DiacriticApplierMethod("̪"));
    private static readonly CommandData _textinvsubbridgeCommandData = new(1, DiacriticApplierMethod("̺"));
    private static readonly CommandData _textsubrhalfringCommandData = new(1, DiacriticApplierMethod("̹"));
    private static readonly CommandData _textsublhalfringCommandData = new(1, DiacriticApplierMethod("̜"));
    private static readonly CommandData _textsubwCommandData = new(1, DiacriticApplierMethod("̫"));
    private static readonly CommandData _textseagullCommandData = new(1, DiacriticApplierMethod("̼"));
    private static readonly CommandData _textovercrossCommandData = new(1, DiacriticApplierMethod("̽"));
    private static readonly CommandData _textsubplusCommandData = new(1, DiacriticApplierMethod("̟"));
    private static readonly CommandData _textraisingCommandData = new(1, DiacriticApplierMethod("̝"));
    private static readonly CommandData _textloweringCommandData = new(1, DiacriticApplierMethod("̞"));
    private static readonly CommandData _textadvancingCommandData = new(1, DiacriticApplierMethod("̘"));
    private static readonly CommandData _textretractingCommandData = new(1, DiacriticApplierMethod("̘"));
    private static readonly CommandData _textsuperimposetildeCommandData = new(1, DiacriticApplierMethod("̴"));
    private static readonly CommandData _symVertTipaCommandData = new(0, TipaCommandSymVert, null, false);

    private static void TipaCommandSymVert(Context context)
    {
        char c = context.PopSource();
        switch (c)
        {
            case 'c': ExecuteCommand(context, _textroundcapCommandData); break;
            case '[': ExecuteCommand(context, _textsubbridgeCommandData); break;
            case ']': ExecuteCommand(context, _textinvsubbridgeCommandData); break;
            case '(': ExecuteCommand(context, _textsubrhalfringCommandData); break;
            case ')': ExecuteCommand(context, _textsublhalfringCommandData); break;
            case 'w': ExecuteCommand(context, _textsubwCommandData); break;
            case 'm': ExecuteCommand(context, _textseagullCommandData); break;
            case 'x': ExecuteCommand(context, _textovercrossCommandData); break;
            case '+': ExecuteCommand(context, _textsubplusCommandData); break;
            case '\'': ExecuteCommand(context, _textraisingCommandData); break;
            case '`': ExecuteCommand(context, _textloweringCommandData); break;
            case '<': ExecuteCommand(context, _textadvancingCommandData); break;
            case '>': ExecuteCommand(context, _textretractingCommandData); break;
            case '~': ExecuteCommand(context, _textsuperimposetildeCommandData); break;
            default:
                context.AppendSource(c);
                throw context.CreateParseError("Command '\\|' requires a subcommand");
        }
    }

    private static readonly CommandData _symEqualsCommandData = new(1, DiacriticApplierMethod("̄"));
    private static readonly CommandData _textsubbarCommandData = new(1, DiacriticApplierMethod("̠"));
    private static readonly CommandData _symEqualsTipaCommandData = new(0, TypaCommandSymEquals, null, false);

    private static void TypaCommandSymEquals(Context context)
    {
        if (context.PeekSource() == '*')
        {
            _ = context.PopSource();
            ExecuteCommand(context, _textsubbarCommandData);
        }
        else
        {
            ExecuteCommand(context, _symEqualsCommandData);
        }
    }

    private static readonly CommandData _vCommandData = new(1, DiacriticApplierMethod("̌"));
    private static readonly CommandData _textacutewedgeCommandData = new(1, DiacriticApplierMethod("̌́"));
    private static readonly CommandData _textsubwedgeCommandData = new(1, DiacriticApplierMethod("̬"));
    private static readonly CommandData _vTipaCommandData = new(0, TipaCommandV, null, false);

    private static void TipaCommandV(Context context)
    {
        char c = context.PopSource();
        switch (c)
        {
            case '\'': ExecuteCommand(context, _textacutewedgeCommandData); break;
            case '*': ExecuteCommand(context, _textsubwedgeCommandData); break;
            default:
                context.AppendSource(c);
                ExecuteCommand(context, _vCommandData);
                break;
        }
    }

    private static readonly CommandData _rCommandData = new(1, DiacriticApplierMethod("̊"));
    private static readonly CommandData _textsubringCommandData = new(1, DiacriticApplierMethod("̥"));
    private static readonly CommandData _textringmacronCommandData = new(1, DiacriticApplierMethod("̄̊"));
    private static readonly CommandData _rTipaCommandData = new(0, TipaCommandR,null, false);

    private static void TipaCommandR(Context context)
    {
        char c = context.PopSource();
        switch (c)
        {
            case '=': ExecuteCommand(context, _textringmacronCommandData); break;
            case '*': ExecuteCommand(context, _textsubringCommandData); break;
            default:
                context.AppendSource(c);
                ExecuteCommand(context, _rCommandData);
                break;
        }
    }

    private static readonly CommandData _uCommandData = new(1, DiacriticApplierMethod("̆"));
    private static readonly CommandData _textbrevemacronCommandData = new(1, DiacriticApplierMethod("̄̆"));
    private static readonly CommandData _uTipaCommandData = new(0, TipaCommandU, null, false);
    private static void TipaCommandU(Context context)
    {
        if (context.PeekSource() == '=')
        {
            _ = context.PopSource();
            ExecuteCommand(context, _textbrevemacronCommandData);
        }
        else
        {
            ExecuteCommand(context, _uCommandData);
        }
    }


    // ReSharper disable once InconsistentNaming
    private static readonly CommandData _OmegaMathCommandData = NewSymbolicCommandData('Ω');

    private static readonly CommandData _langleMathSymbolicCommandData = NewSymbolicCommandData('⟨');

    private static readonly CommandData _rangleMathSymbolicCommandData = NewSymbolicCommandData('⟩');
}
