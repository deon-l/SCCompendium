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

    /// <remarks>Some chars here aren't usually escapable by \, but it is easier this way.</remarks>
    private static readonly HashSet<char> _escapedChars = new("#$&%{} \t");
    private static readonly HashSet<char> _spacingWhitespace = new(" \t");

    private const string TipaInput  = ":;\"0123456789@ABCDEFGHIJKLMNOPQRSTUVWXYZ|";
    private const string TipaOutput = "ː\u02D1ˈʉɨʌɜɥɐɒɤɵɘəɑβɕðɛɸɣɦɪʝʁʎɱŋɔʔʕɾʃθʊʋɯχʏʒ|";

    private static readonly Dictionary<char, string> _tipaSingleCharConversions =
        Enumerable.Zip(TipaInput, TipaOutput)
            .Select(pair => (pair.First, pair.Second.ToString()))
            .ToDictionary();

    private static readonly Dictionary<string, CommandData> _normalCommands = new();
    private static readonly Dictionary<string, CommandData> _tipaCommands = new();
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
        _normalCommands.Add("change", NewSymbolicCommandData('→'));
        _normalCommands.Add("textrightarrow", _normalCommands["change"]);
        _normalCommands.Add("bf", _commandBfData);
        _normalCommands.Add("textbf", _commandTextBf);
        _normalCommands.Add("it", _itCommandData);
        _normalCommands.Add("textit", _textitCommandData);
        _normalCommands.Add("tt", _commandTtData);
        // `\tab` is a cmd defined by the Index Diachronica.
        _normalCommands.Add("tab", NewSymbolicCommandData("\\\t"));
        _normalCommands.Add("hspace", _commandHSpaceData);
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
        _normalCommands.Add("^", _caretCommandData);
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

        _tipaCommands.Add("*", new(1, CommandAsterisk));
        _tipaCommands.Add("super", new (1, CommandSuper));
        _tipaCommands.Add("~", _ipaCommandTildeData);
        _tipaCommands.Add("^", _caretIpaCommandData);
        _tipaCommands.Add("t", _tIpaCommandData);

        _mathCommands.Add("Omega", NewSymbolicCommandData('Ω'));
        _mathCommands.Add("langle", NewSymbolicCommandData('⟨'));
        _mathCommands.Add("rangle", NewSymbolicCommandData('⟩'));
    }

    private static readonly CommandData _commandTextIpaData = new(1, CommandTextIpa,
        new Typeset(_tipaCommands,
            new() { { ('|', '|'), "‖" }, {('\"', '\"'), "ˌ"} },
            _tipaSingleCharConversions));
}
