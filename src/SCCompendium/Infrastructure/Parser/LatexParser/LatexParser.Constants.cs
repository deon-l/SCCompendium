using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

// Todo: Move much of this stuff to LatexParser.Commands.cs

// This file stores Miscellaneous constants,
// and performs other setup such as populating command lists.
public partial class LatexParser
{
    /// <summary>
    /// Char used to signal to <see cref="CommandTextIpa"/> to not apply replacements to next character.
    /// </summary>
    private const char TipaIgnoreNextChar = (char)26; // 'Substitute' character, used as it seems unused and thematically similar.
    /// <summary>Signals next character is a normal char, and should not be interpreted as a special char.</summary>
    private const char ForceNormalToken = '\e';

    /// <remarks>Some chars here aren't usually escapable by \, but it is easier this way.</remarks>
    private static readonly HashSet<char> _escapedChars = new("#$&%{} ");
    private static readonly HashSet<char> _spacingWhitespace = new(" \t\u2009");

    private static readonly Dictionary<string, CommandData> _normalCommands = new();
    private static readonly Dictionary<string, CommandData> _tipaCommands;
    private static readonly Dictionary<string, CommandData> _mathCommands = new();

    private static readonly Dictionary<(char, char), string> _paragraphLigatures = new()
    {
        { ('`', '`'), "“" },
        { ('\'', '\''), "”"},
        { ('-', '-'), "–"},
        { ('–', '-'), "—"},
    };

    private static readonly Typeset _paragraphTypeset = new(_normalCommands, _paragraphLigatures, null);
    private static readonly Typeset _mathModeTypeset = new(_mathCommands, null, null);

    static LatexParser()
    {
        _normalCommands.Add("ipa", _commandTextIpaData);
        _normalCommands.Add("textipa", _commandTextIpaData);
        _normalCommands.Add("change", _textrightarrowCommandData);
        _normalCommands.Add("textrightarrow", _textrightarrowCommandData);
        _normalCommands.Add("bf", _commandBfData);
        _normalCommands.Add("textbf", _commandTextBf);
        _normalCommands.Add("it", _itCommandData);
        _normalCommands.Add("textit", _textitCommandData);
        _normalCommands.Add("tt", _commandTtData);
        _normalCommands.Add("texttt", _textttComandData);
        _normalCommands.Add("sc", _scCommandData);
        // `\tab` is a cmd defined by the Index Diachronica.
        _normalCommands.Add("tab", NewSymbolicCommandData("\e\t"));
        _normalCommands.Add("hspace", _commandHSpaceData);
        _normalCommands.Add("raisebox", _raiseboxCommandData);
        _normalCommands.Add("LaTeX", _commandLatexData);
        _normalCommands.Add("textpolhook", _commandTextPolHook);
        _normalCommands.Add("newpage", _nullCommandData);
        _normalCommands.Add("tilde", _commandTildeData);
        _normalCommands.Add("~", _commandTildeData);
        _normalCommands.Add("'", _commandApostropheData);
        _normalCommands.Add("c", _commandCData);
        _normalCommands.Add("i", _commandIData);
        _normalCommands.Add("d", _dCommandData);
        _normalCommands.Add("O", _OCommandData);
        _normalCommands.Add("o", _oCommandData);
        _normalCommands.Add("l", _lCommandData);
        _normalCommands.Add("L", _LCommandData);
        _normalCommands.Add("^", _caretCommandData);
        _normalCommands.Add("\"", _quoteCommandData);
        _normalCommands.Add("ae", _aeCommandData);
        _normalCommands.Add("oe", _oeCommandData);
        _normalCommands.Add("AA", _AACommandData);
        _normalCommands.Add("aa", _aaCommandData);
        _normalCommands.Add(".", _symPeriodCommandData);
        _normalCommands.Add("`", _symGraveAccentCommandData);
        _normalCommands.Add("-", _symMinusCommandData);
        _normalCommands.Add(",", _symCommaCommandData);
        _normalCommands.Add("textturnmrleg", _textturnmrleg);
        _normalCommands.Add("textsubcircum", _textsubcircumCommandData);
        _normalCommands.Add("textcircumdot", _textcircumdotCommandData);
        _normalCommands.Add("textellipsis", _textellipsisCommandData);
        _normalCommands.Add("textbardotlessj", _textbardotlessjCommandData);
        _normalCommands.Add("textcrh", _textcrhCommandData);
        _normalCommands.Add("textbeltl", _textbeltlCommandData);
        _normalCommands.Add("textquotedblleft", _textquotedblleftCommandData);
        _normalCommands.Add("textltailn", _textltailnCommandData);
        _normalCommands.Add("textless", _textlessCommandData);
        _normalCommands.Add("textgreater", _textgreaterCommandData);
        _normalCommands.Add("textltilde", _textltildeCommandData);
        _normalCommands.Add("textroundcap", _textroundcapCommandData);
        _normalCommands.Add("textsubbridge", _textsubbridgeCommandData);
        _normalCommands.Add("textinvsubbridge", _textinvsubbridgeCommandData);
        _normalCommands.Add("textsubrhalfring", _textsubrhalfringCommandData);
        _normalCommands.Add("textsublhalfring", _textsublhalfringCommandData);
        _normalCommands.Add("textsubw", _textsubwCommandData);
        _normalCommands.Add("textsubsquare", _textsubsquareCommandData);
        _normalCommands.Add("textseagull", _textseagullCommandData);
        _normalCommands.Add("textovercross", _textovercrossCommandData);
        _normalCommands.Add("textsubplus", _textsubplusCommandData);
        _normalCommands.Add("textraising", _textraisingCommandData);
        _normalCommands.Add("textlowering", _textloweringCommandData);
        _normalCommands.Add("textadvancing", _textadvancingCommandData);
        _normalCommands.Add("textretracting", _textretractingCommandData);
        _normalCommands.Add("textsuperimposedtilde", _textsuperimposedtildeCommandData);
        _normalCommands.Add("textturnw", _textturnwCommandData);
        _normalCommands.Add("textasciitilde", _textasciitildeCommandData);
        _normalCommands.Add("textcorner", _textcornerCommandData);
        _normalCommands.Add("textlyoghlig", _textlyoghligCommandData);
        _normalCommands.Add("textctz", _textctzCommandData);
        _normalCommands.Add("textsoftsign", _textsoftsignCommandData);
        _normalCommands.Add("texthardsign",_texthardsignCommandData);
        _normalCommands.Add("textctn",_textctnCommandData);
        _normalCommands.Add("textleftarrow",_textleftarrowCommandData);
        _normalCommands.Add("textdoublebarpipe",_textdoublebarpipeCommandData);
        _normalCommands.Add("textquoteleft",_textquoteleftCommandData);
        _normalCommands.Add("textrhoticity",_textrhoticityCommandData);
        _normalCommands.Add("textturna",_textturnaCommandData);
        _normalCommands.Add("textlhtlongi",_textlhtlongiCommandData);
        _normalCommands.Add("textraisevibyi",_textraisevibyiCommandData);
        _normalCommands.Add("textbackslash",_textbackslashCommandData);
        _normalCommands.Add("backslash",_textbackslashCommandData);
        _normalCommands.Add("j", _jCommandData);
        _normalCommands.Add("=", _symEqualsCommandData);
        _normalCommands.Add("textsubbar", _textsubbarCommandData);
        _normalCommands.Add("textsubarch", _textsubarchCommandData);
        _normalCommands.Add("v", _vCommandData);
        _normalCommands.Add("textacutewedge", _textacutewedgeCommandData);
        _normalCommands.Add("textsubwedge", _textsubwedgeCommandData);
        _normalCommands.Add("r", _rCommandData);
        _normalCommands.Add("textsubring", _textsubringCommandData);
        _normalCommands.Add("textringmacron", _textringmacronCommandData);
        _normalCommands.Add("u", _uCommandData);
        _normalCommands.Add("textbrevemacron", _textbrevemacronCommandData);

        _tipaCommands = _commandTextIpaData.Typeset!.Value.CommandList!;
        _tipaCommands.Add("*", _symAsteriskTipaCommandData);
        _tipaCommands.Add("super", _superTipaCommandData);
        _tipaCommands.Add("~", _ipaCommandTildeData);
        _tipaCommands.Add("^", _caretIpaCommandData);
        _tipaCommands.Add("t", _tIpaCommandData);
        _tipaCommands.Add(";", _symSemicolonIpaCommandData);
        _tipaCommands.Add(":", _symColonIpaCommandData);
        _tipaCommands.Add("!", _symExclamationPointIpaCommandData);
        _tipaCommands.Add("|", _symVertIpaCommandData);
        _tipaCommands.Add("=", _symEqualsIpaCommandData);
        _tipaCommands.Add("v", _vTipaCommandData);
        _tipaCommands.Add("r", _rTipaCommandData);
        _tipaCommands.Add("s", _sTipaCommandData);
        _tipaCommands.Add("u", _uTipaCommandData);

        _mathCommands.Add("Omega", _OmegaMathCommandData);
        _mathCommands.Add("langle", _langleMathSymbolicCommandData);
        _mathCommands.Add("rangle", _rangleMathSymbolicCommandData);
    }
}
