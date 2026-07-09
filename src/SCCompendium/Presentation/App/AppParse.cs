using CommandDotNet;
using SCCompendium.Application.DbAccess;
using SCCompendium.Application.Parser;
using SCCompendium.Domain.ValueObjects.Parsed;

namespace SCCompendium.Presentation.App;

[Subcommand]
[Command(Description = "Parse phonological data stored in latex")]
public class AppParse(IDbConnectionRepository connectionRepo, IDiachronicaParser diaParser, IDbWriter dbWriter)
{
    [DefaultCommand]
    [Command(Description = "Parses the specified file phonological rules and uploads it to the SQL database specified by the connection string.")]
    public void All(
        [Operand]string fileName,
        [Option('c', "connection")]string connectionString)
    {
        connectionRepo.CreateConnection(connectionString);
        StreamReader file = new(File.OpenRead(fileName));

        List<PhonologicalRuleGroup> rules = diaParser.Parse(file);
        dbWriter.Write(connectionRepo, rules);
    }
}
