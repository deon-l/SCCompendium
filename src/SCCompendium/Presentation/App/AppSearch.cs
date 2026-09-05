using CommandDotNet;
using SCCompendium.Application.CliIO;
using SCCompendium.Application.DbAccess;
using SCCompendium.Application.Parser;
using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Presentation.App.ArgumentModels;
using SCCompendium.Tests.Domain.ValueObjects.DbValues;

namespace SCCompendium.Presentation.App;

[Subcommand]
[Command(Description = "Search and filter data stored in the database")]
public class AppSearch(IDbConnectionRepository connectionRepo, IDbReader dbReader, ICharacterSearchParser searchParser, IRuleGroupPrinter ruleGroupPrinter)
{
    [DefaultCommand]
    public void Character(
        [Option('f', "filter")] string searchFilter,
        PrintOptions printOptions,
        [Operand] string connectionString)
    {
        if (printOptions.NoOptionsSelected())
        {
            printOptions.PrintGroups = true;
            printOptions.PrintRules = true;
        }
        CharacterSearch search = searchParser.GetCharacterSearch(searchFilter);
        connectionRepo.CreateConnection(connectionString);

        List<PhonologicalRuleGroup> groups = dbReader.FindRules(connectionRepo, search);

        if (printOptions.PrintGroups || printOptions.PrintRules)
        {
            ruleGroupPrinter.PrintRuleGroups(groups, printOptions.PrintGroups, printOptions.PrintRules);
        }
        if (printOptions.PrintCharacters)
        {
            ruleGroupPrinter.PrintCharacters(groups);
        }
        if (printOptions.PrintDiacritics)
        {
            ruleGroupPrinter.PrintDiacritics(groups);
        }
    }
}
