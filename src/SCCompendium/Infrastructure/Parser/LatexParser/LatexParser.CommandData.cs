using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    /// <summary>
    /// Represents what a command does
    /// </summary>
    /// <param name="Arguments">Number of arguments it takes</param>
    /// <param name="Command">Method defining a commands behaviour</param>
    /// <param name="Typeset">Typeset a command defines, applied automatically</param>
    /// <param name="AutoSurroundGroup">
    /// Whether to surround command in a group (i.e. automatically increase indent at start, and decrease at end.
    /// </param>
    private record struct CommandData(
        int Arguments,
        Action<Context> Command,
        Typeset? Typeset = null,
        bool AutoSurroundGroup = true);
}
