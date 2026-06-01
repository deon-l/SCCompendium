namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    /// <summary>
    /// Defines a list of commands, ligatures, and replacements that a command defines (or other feature) defines.
    /// </summary>
    /// <param name="CommandList">Defined commands. Keys are the command names</param>
    /// <param name="Ligatures">Defined ligatures. Keys are the 2 chars which are converted into the corresponding value.</param>
    /// <param name="Replacements">Defined replacements. Keys are the char replaced with the corresponding value.</param>
    private record struct Typeset(
        Dictionary<string, CommandData>? CommandList,
        Dictionary<(char, char), string>? Ligatures,
        Dictionary<char, string>? Replacements);
}
