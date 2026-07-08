using CommandDotNet;
using SCCompendium.Application.DbAccess;
using SCCompendium.Application.Parser;

namespace SCCompendium.Tests.Presentation;

public class App
{
    // static members to be used in subcommands, to emulate dependency injection.
    public static IDbConnectionRepository DbConnectionRepository { get; set; } = null!;
    public static IDiachronicaParser DiachronicaParser { get; set; } = null!;
    public static IDbReader DbReader { get; set; } = null!;
    public static IDbWriter DbWriter { get; set; } = null!;

    [Subcommand(RenameAs = "parse")]
    public AppParse Parse { get; set; } = null!;
    [Subcommand(RenameAs = "search")]
    public AppSearch Search { get; set; } = null!;
}
