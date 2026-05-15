namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private record struct Typeset(
        Dictionary<string, CommandData>? CommandList,
        Dictionary<(char, char), string>? Ligatures,
        Dictionary<char, string>? Replacements);
}
