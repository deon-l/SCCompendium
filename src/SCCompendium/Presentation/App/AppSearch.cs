using CommandDotNet;
using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Tests.Domain.ValueObjects.DbValues;

namespace SCCompendium.Presentation.App;

[Subcommand]
public class AppSearch
{
    [DefaultCommand]
    public void Character(
        string character,
        [Option] string connectionString)
    {
        // TODO: proper method to create search from string
        CharacterSearch search = new() { Character = character };
        App.DbConnectionRepository.CreateConnection(connectionString);

        List<PhonologicalRuleGroup> groups = App.DbReader.FindRules(App.DbConnectionRepository, search);
        // TODO: Add some other Application interface for this.
        foreach (PhonologicalRuleGroup group in groups)
        {
            Console.WriteLine(group);
        }
    }
}
