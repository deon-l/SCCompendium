using CommandDotNet;

namespace SCCompendium.Presentation.App;

/// <remarks>
/// Bass command.
/// </remarks>
[Command(Description = "Utility to parse Phonological data, upload to MySQL databases, and read from them.")]
public class App
{
    [Subcommand(RenameAs = "parse")]
    public AppParse Parse { get; set; } = null!;
    [Subcommand(RenameAs = "search")]
    public AppSearch Search { get; set; } = null!;
}
