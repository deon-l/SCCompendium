using CommandDotNet;
using SCCompendium.Application.DbAccess;
using SCCompendium.Application.Parser;
using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Tests.Domain.ValueObjects.DbValues;

namespace SCCompendium.Presentation.App;

[Subcommand]
public class AppSearch(IDbConnectionRepository connectionRepo, IDbReader dbReader)
{
    [DefaultCommand]
    public void Character(
        string character,
        [Option] string connectionString)
    {
        // TODO: proper method to create search from string
        CharacterSearch search = new() { Character = character };
        connectionRepo.CreateConnection(connectionString);

        List<PhonologicalRuleGroup> groups = dbReader.FindRules(connectionRepo, search);
        // TODO: Add some other Application interface for this.
        foreach (PhonologicalRuleGroup group in groups)
        {
            Console.WriteLine(group);
        }
    }
}
