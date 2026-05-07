using System.Text;
using OneOf;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private const char TipaIgnoreNextChar = (char)26; // 'Substitute' character, used as it seems unused and thematically similar.

    private static readonly HashSet<char> _escapedChars = new("#$&%{} ");
    private static readonly HashSet<char> _spacingWhitespace = new(" \t");

    private const string TipaInput  = ":;\"0123456789@ABCDEFGHIJKLMNOPQRSTUVWXYZ|";
    private const string TipaOutput = "ː\u02D1ˈʉɨʌɜɥɐɒɤɵɘəɑβɕðɛɸɣɦɪʝʁʎɱŋɔʕɾʃθʊʋɯχʏʒ|";

    private static readonly Dictionary<char, string> _tipaSingleCharConversions =
        Enumerable.Zip(TipaInput, TipaOutput)
            .Select(pair => (pair.First, pair.Second.ToString()))
            .ToDictionary();

    private static readonly Dictionary<string, CommandData> _normalCommands = new();
    private static readonly Dictionary<string, CommandData> _tipaCommands = new();
    private static readonly Dictionary<string, CommandData> _mathCommands = new();

    static LatexParser()
    {
        _normalCommands.Add("ipa", _commandTextIpaData);
        _normalCommands.Add("textipa", _commandTextIpaData);
    }

    private static readonly CommandData _commandTextIpaData = new(1, CommandTextIpa,
        new Typeset(_tipaCommands,
            new() { { ('|', '|'), "‖" }, {('\"', '\"'), "ˌ"} },
            _tipaSingleCharConversions));
}
