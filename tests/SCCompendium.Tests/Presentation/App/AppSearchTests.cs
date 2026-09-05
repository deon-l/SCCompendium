using SCCompendium.Application.CliIO;
using SCCompendium.Application.DbAccess;
using SCCompendium.Application.Parser;
using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Presentation.App;
using SCCompendium.Presentation.App.ArgumentModels;
using SCCompendium.Tests.Domain.ValueObjects.DbValues;

namespace SCCompendium.Tests.Presentation.App;

public partial class AppSearchTests
{
    private class Mocks
    {
        public Mock<IDbConnectionRepository> ConnectionRepo { get; } = IDbConnectionRepository.Mock();
        public Mock<IDbReader> DbReader { get; } = IDbReader.Mock();
        public Mock<ICharacterSearchParser> CharSearchParser { get; } = ICharacterSearchParser.Mock();
        public Mock<IRuleGroupPrinter> RuleGroupPrinter { get; } = IRuleGroupPrinter.Mock();
    }

    private (Mocks mocks, AppSearch command) SetupApp()
    {
        Mocks mocks = new();
        AppSearch command = new(
            mocks.ConnectionRepo.Object,
            mocks.DbReader.Object,
            mocks.CharSearchParser.Object,
            mocks.RuleGroupPrinter.Object);
        return (mocks, command);
    }

    [Test]
    public async Task Search_NoOptionsGiven_PrintsRulesAndGroups()
    {
        const string connectionString = "key1=value1;key2=value 2; key3 = value3";
        const string filter = "";
        PrintOptions options = default;
        CharacterSearch defaultSearch = new();
        var (mocks, command) = SetupApp();
        List<PhonologicalRuleGroup> rules = [new("a", "a", [])];
        mocks.CharSearchParser.GetCharacterSearch(Any()).Returns(defaultSearch);
        mocks.DbReader.FindRules(AnyArgs()).Returns(rules);

        command.Search(filter, options, connectionString);

        mocks.ConnectionRepo.CreateConnection(connectionString).WasCalled(Times.Exactly(1));
        mocks.CharSearchParser.GetCharacterSearch(filter).WasCalled(Times.Exactly(1));
        mocks.DbReader.FindRules(mocks.ConnectionRepo.Object.Equals, defaultSearch.Equals).WasCalled(Times.Exactly(1));
        mocks.RuleGroupPrinter.PrintCharacters(Any()).WasNeverCalled();
        mocks.RuleGroupPrinter.PrintDiacritics(Any()).WasNeverCalled();
        mocks.RuleGroupPrinter.PrintRuleGroups(rules, true, true).WasCalled();
        mocks.RuleGroupPrinter.PrintRuleGroups(rules, Any(), Any()).WasCalled(Times.Exactly(1));
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.AllNonselectedPrintOptionCases))]
    public async Task Search_FilterAndAnyPrintOptions_PrintsOnlySelected(PrintOptions options)
    {
        const string connectionString = "key1=value1;key2=value 2; key3 = value3";
        const string filter = "";
        CharacterSearch defaultSearch = new();
        var (mocks, command) = SetupApp();
        List<PhonologicalRuleGroup> rules = [new("a", "a", [])];
        mocks.CharSearchParser.GetCharacterSearch(Any()).Returns(defaultSearch);
        mocks.DbReader.FindRules(AnyArgs()).Returns(rules);

        command.Search(filter, options, connectionString);


        mocks.ConnectionRepo.CreateConnection(connectionString).WasCalled(Times.Exactly(1));
        mocks.CharSearchParser.GetCharacterSearch(filter).WasCalled(Times.Exactly(1));
        mocks.DbReader.FindRules(mocks.ConnectionRepo.Object.Equals, defaultSearch.Equals).WasCalled(Times.Exactly(1));
        if (options.PrintCharacters)
            mocks.RuleGroupPrinter.PrintCharacters(rules).WasCalled(Times.Exactly(1));
        else
            mocks.RuleGroupPrinter.PrintCharacters(Any()).WasNeverCalled();
        if (options.PrintDiacritics)
            mocks.RuleGroupPrinter.PrintDiacritics(rules).WasCalled(Times.Exactly(1));
        else
            mocks.RuleGroupPrinter.PrintDiacritics(Any()).WasNeverCalled();
        if (options.PrintGroups || options.PrintRules)
            mocks.RuleGroupPrinter.PrintRuleGroups(rules, options.PrintGroups, options.PrintRules).WasCalled(Times.Exactly(1));
        else
            mocks.RuleGroupPrinter.PrintRuleGroups(rules, Any(), Any()).WasNeverCalled();
    }
}
