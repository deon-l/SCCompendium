
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
    /// If true, automatically increments depth at start, and appends a closing brace after all parameters.
    /// This can be used to tell when arguments end.
    /// </param>
    private record struct CommandData(
        int Arguments,
        Action<Context> Command,
        // ReSharper disable once MemberHidesStaticFromOuterClass
        Typeset? Typeset = null,
        bool AutoSurroundGroup = true);
}
