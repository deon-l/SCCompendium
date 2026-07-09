using CommandDotNet;

namespace SCCompendium.Presentation.App;

public class App
{
    [Subcommand(RenameAs = "parse")]
    public AppParse Parse { get; set; } = null!;
    [Subcommand(RenameAs = "search")]
    public AppSearch Search { get; set; } = null!;
}
