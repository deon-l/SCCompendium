using CommandDotNet;
using SCCompendium.Domain.ValueObjects.Parsed;

namespace SCCompendium.Presentation.App;

[Subcommand]
public class AppParse
{
    [DefaultCommand]
    [Command(Description = "Parses the specified file phonological rules and uploads it to the SQL database specified by the connection string.")]
    public void All(
        [Operand]string fileName,
        [Option('c', "connection")]string connectionString)
    {
        App.DbConnectionRepository.CreateConnection(connectionString);
        StreamReader file = new(File.OpenRead(fileName));

        List<PhonologicalRuleGroup> rules = App.DiachronicaParser.Parse(file);
        App.DbWriter.Write(App.DbConnectionRepository, rules);
    }
}
