using SCCompendium.Domain.ValueObjects.Parsed;

namespace SCCompendium.Tests.Infrastructure.CliIO;

public partial class RuleGroupPrinterTests
{
    public class DataSource
    {
        public const string BadIndicator = "zz";
        public static IEnumerable<Func<List<PhonologicalRuleGroup>>> PrintCharactersEmptyCollectionCases()
        {
            yield return () => [];
            yield return () => [new PhonologicalRuleGroup(BadIndicator, BadIndicator, [])];
        }
        public static IEnumerable<Func<List<PhonologicalRuleGroup>>> PrintDiacriticsEmptyCollectionCases()
        {
            yield return () => [];
            yield return () => [new PhonologicalRuleGroup(BadIndicator, BadIndicator, [])];
            yield return () =>
            [
                new PhonologicalRuleGroup(BadIndicator, BadIndicator, [
                    new PhonologicalRule(BadIndicator, [], [], [])
                ])
            ];
        }
    }
}
