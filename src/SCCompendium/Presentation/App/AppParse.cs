using SCCompendium.Application.CliIO;
using SCCompendium.Application.DbAccess;
using SCCompendium.Application.Parser;
using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Presentation.App.ArgumentModels;

namespace SCCompendium.Presentation.App;

/// <summary>
/// Methods for execute the <c>parse</c> command.
/// </summary>
public class AppParse(IDbConnectionRepository connectionRepo, IDbWriter dbWriter, IDiachronicaParser diaParser, IRuleGroupPrinter ruleGroupPrinter, IFileFinder fileFinder)
{
    public void Parse(
        string fileName,
        string? connectionString,
        PrintOptions printOptions)
    {
        bool addToDb = connectionString is not null;
        if (!addToDb && printOptions.NoOptionsSelected())
        {
            printOptions.PrintGroups = true;
            printOptions.PrintRules = true;
        }

        List<PhonologicalRuleGroup> rules = diaParser.Parse(fileFinder.GetFile(fileName));

        if (addToDb)
        {
            connectionRepo.CreateConnection(connectionString!);
            dbWriter.Write(connectionRepo, rules);
        }

        if (printOptions.PrintGroups || printOptions.PrintRules)
        {
            ruleGroupPrinter.PrintRuleGroups(rules, printOptions.PrintGroups, printOptions.PrintRules);
        }
        if (printOptions.PrintCharacters)
        {
            ruleGroupPrinter.PrintCharacters(rules);
        }
        if (printOptions.PrintDiacritics)
        {
            ruleGroupPrinter.PrintDiacritics(rules);
        }
    }
}
