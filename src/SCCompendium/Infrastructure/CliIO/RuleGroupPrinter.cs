using CommandDotNet;
using SCCompendium.Application.CliIO;
using SCCompendium.Domain.ValueObjects.Parsed;

namespace SCCompendium.Infrastructure.CliIO;

public class RuleGroupPrinter(IConsoleIO console) : IRuleGroupPrinter
{
    public IConsoleIO Console { get; } = console;

    public void PrintRuleGroups(List<PhonologicalRuleGroup> ruleGroups, bool printGroupTitles, bool printRules)
    {
        Debug.Assert(printGroupTitles || printRules);
        if (printRules)
        {
            Console.WriteLine("--- Rules ---");
        }
        else
        {
            Console.WriteLine("--- Rule Groups ---");
        }

        foreach (var ruleGroup in ruleGroups)
        {
            if (!printGroupTitles && (!printRules || ruleGroup.Rules.Count == 0))
            {
                continue;
            }
            Console.WriteLine();
            if (printGroupTitles)
            {
                Console.WriteLine($"# {ruleGroup.Title}\n{ruleGroup.Credit}");
                if (!String.IsNullOrEmpty(ruleGroup.Note))
                {
                    Console.WriteLine($"{ruleGroup.Note}");
                }
            }
            if (!printRules)
            {
                continue;
            }

            foreach (var rule in ruleGroup.Rules)
            {
                Console.WriteLine($"\t{rule.Rule}");
            }
        }
    }

    public void PrintCharacters(List<PhonologicalRuleGroup> ruleGroups)
    {
        HashSet<IpaCharacter> characters = new();
        foreach (var ruleGroup in ruleGroups)
        {
            foreach (PhonologicalRule rule in ruleGroup.Rules)
            {
                characters.UnionWith(rule.InputCharacters);
                characters.UnionWith(rule.OutputCharacters);
                characters.UnionWith(rule.ContextCharacters);
            }
        }

        Console.WriteLine($"--- Characters ---");
        foreach (IpaCharacter c in characters)
        {
            Console.WriteLine($"\t{c.Character}{String.Concat(c.Diacritics)}");
        }
    }

    public void PrintDiacritics(List<PhonologicalRuleGroup> ruleGroups)
    {
        HashSet<string> diacritics = new();
        foreach (var ruleGroup in ruleGroups)
        {
            foreach (PhonologicalRule rule in ruleGroup.Rules)
            {
                AddDiacritics(diacritics, rule.InputCharacters);
                AddDiacritics(diacritics, rule.OutputCharacters);
                AddDiacritics(diacritics, rule.ContextCharacters);
            }
        }

        Console.WriteLine($"--- Diacritics ---{String.Concat(diacritics.Select(str => $"\n\t{str}"))}");

        static void AddDiacritics(HashSet<string> diacritics, IEnumerable<IpaCharacter> characters)
        {
            foreach (var character in characters)
            {
                foreach (string dia in character.Diacritics)
                {
                    diacritics.Add(dia.Normalize());
                }
            }
        }
    }
}
