using CommandDotNet;

namespace SCCompendium.Presentation.App;

/// <remarks>
/// Base command.
/// </remarks>
[Command(Description = "Utility to parse Phonological data, upload to MySQL databases, and read from them.")]
public class App
{
    [Command(Description = "Parses the specified file for latex phonological rules and redirects it elsewhere.")]
    public void Parse(
        [Operand]string fileName,
        [Option('c', "connection")]string connectionString,
        [Option('r', "print-rules")]bool print-rules)
    {
        connectionRepo.CreateConnection(connectionString);
        StreamReader file = new(File.OpenRead(fileName));

        List<PhonologicalRuleGroup> rules = diaParser.Parse(file);
        dbWriter.Write(connectionRepo, rules);
    }
    [Subcommand(RenameAs = "search")]
    public AppSearch Search { get; set; } = null!;
}
