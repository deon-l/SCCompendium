using System.Text;
using OneOf;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private const string TipaInput  = ":;\"0123456789@ABCDEFGHIJKLMNOPQRSTUVWXYZ|";
    private const string TipaOutput = "ː\u02D1ˈʉɨʌɜɥɐɒɤɵɘəɑβɕðɛɸɣɦɪʝʁʎɱŋɔʕɾʃθʊʋɯχʏʒ|";

    private static readonly Dictionary<char, char> _tipaSingleCharConversions =
        Enumerable.Zip(TipaInput, TipaOutput).ToDictionary();

    private static readonly Dictionary<string, Command> _normalCommands = new();
    private static readonly Dictionary<string, Command> _tipaCommands = new();
    private static readonly Dictionary<string, Command> _mathCommands = new();
}
