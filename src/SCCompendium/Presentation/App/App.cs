using CommandDotNet;
using SCCompendium.Application.CliOutput;
using SCCompendium.Application.DbAccess;
using SCCompendium.Application.Parser;
using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Presentation.App.ArgumentModels;

namespace SCCompendium.Presentation.App;

/// <remarks>
/// Base command.
/// </remarks>
[Command(Description = "Utility to parse Phonological data, upload to MySQL databases, and read from them.")]
public class App(IDbConnectionRepository connectionRepo, IDbWriter dbWriter, IDiachronicaParser diaParser, IRuleGroupPrinter ruleGroupPrinter)
{
    [Command(Description = "Parses the specified file for latex phonological rules and redirects it elsewhere.")]
    public void Parse(
        [Operand]string fileName,
        [Option('u', "add-to-db")]string? connectionString,
        PrintOptions printOptions)
    {
        bool addToDb = connectionString is not null;
        if (!addToDb && printOptions.NoOptionsSelected())
        {
            printOptions.PrintCharacters = true;
        }

        StreamReader file = new(File.OpenRead(fileName));

        List<PhonologicalRuleGroup> rules = diaParser.Parse(file);

        if (addToDb)
        {
            connectionRepo.CreateConnection(connectionString!);
            dbWriter.Write(connectionRepo, rules);
        }

        if (printOptions.PrintGroups || printOptions.PrintGroups)
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

    [Subcommand(RenameAs = "search")]
    public AppSearch Search { get; set; } = null!;
}
