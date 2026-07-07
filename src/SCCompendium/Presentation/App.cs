using CommandDotNet;
using SCCompendium.Application.DbAccess;
using SCCompendium.Application.Parser;

namespace SCCompendium.Tests.Presentation;

public class App
{
    public static IDbConnectionRepository DbConnectionRepository { get; set; } = null!;
    public static IDiachronicaParser DiachronicaParser { get; set; } = null!;
    public static IDbReader DbReader { get; set; } = null!;
    public static IDbWriter DbWriter { get; set; } = null!;

    [Subcommand]
    public AppParse Parse { get; set; } = null!;
}
