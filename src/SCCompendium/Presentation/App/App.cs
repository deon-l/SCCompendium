using CommandDotNet;
using SCCompendium.Presentation.App.ArgumentModels;

namespace SCCompendium.Presentation.App;

/// <remarks>
/// Base command.
/// </remarks>
[Command(Description = "Utility to parse Phonological data, upload to MySQL databases, and read from them.")]
public class App(AppParse parseCommand)
{
    public static IEnumerable<Type> GetCommandImplementationDependencies()
    {
        yield return typeof(AppParse);
    }

    [Command(Description = "Parses the specified file for latex phonological rules and redirects it elsewhere.")]
    public void Parse(
        [Operand]string fileName,
        [Option('u', "upload-to-db")]string? connectionString,
        PrintOptions printOptions)
    {
       parseCommand.Parse(fileName, connectionString, printOptions);
    }

    [Subcommand(RenameAs = "search")]
    public AppSearch Search { get; set; } = null!;
}
