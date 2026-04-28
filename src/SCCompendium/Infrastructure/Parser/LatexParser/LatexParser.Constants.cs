using System.Text;
using OneOf;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

using MacroCollection = Dictionary<string, OneOf<
    Action<StringBuilder>,
    Action<ReadOnlySpan<char>, StringBuilder>,
    Action<ReadOnlySpan<char>, ReadOnlySpan<char>, StringBuilder>
    >>;

public partial class LatexParser
{
    private const string TipaInput  = ":;0123456789@ABCDEFGHIJKLMNOPQRSTUVWXYZ|";
    private const string TipaOutput = "ː\u02D1ˈʉɨʌɜɥɐɒɤɵɘəɑβɕðɛɸɣɦɪʝʁʎɱŋɔʕɾʃθʊʋɯχʏʒ|";

    private static readonly Dictionary<char, char> _tipaSingleCharConversions =
        Enumerable.Zip(TipaInput, TipaOutput).ToDictionary();

    private static readonly MacroCollection _normalMacros = new();
    private static readonly MacroCollection _tipaMacros = new();
    private static readonly MacroCollection _mathMacros = new();
}
