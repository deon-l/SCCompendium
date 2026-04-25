namespace SCCompendium.Infrastructure.Parser;

public partial class LatexParser
{
    private const string TipaInput  = ":;0123456789@ABCDEFGHIJKLMNOPQRSTUVWXYZ|";
    private const string TipaOutput = "ː\u02D1ˈʉɨʌɜɥɐɒɤɵɘəɑβɕðɛɸɣɦɪʝʁʎɱŋɔʕɾʃθʊʋɯχʏʒ|";

    private static readonly Dictionary<char, char> _tipaSingleCharConversions =
        Enumerable.Zip(TipaInput, TipaOutput).ToDictionary();
}
