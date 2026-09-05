using SCCompendium.Application.CliIO;
using SCCompendium.Application.DbAccess;
using SCCompendium.Application.Parser;
using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Presentation.App;
using SCCompendium.Presentation.App.ArgumentModels;

namespace SCCompendium.Tests.Presentation.App;

public partial class AppParseTests
{
    private class Mocks
    {
        public Mock<IDbConnectionRepository> ConnectionRepo { get; } = IDbConnectionRepository.Mock();
        public Mock<IDbWriter> DbWriter { get; } = IDbWriter.Mock();
        public Mock<IDiachronicaParser> DiaParser { get; } = IDiachronicaParser.Mock();
        public Mock<IRuleGroupPrinter> RuleGroupPrinter { get; } = IRuleGroupPrinter.Mock();
        public Mock<IFileFinder> FileFinder { get; } = IFileFinder.Mock();
    }

    private (Mocks mocks, AppParse command) SetupApp()
    {
        Mocks mocks = new();
        AppParse command = new(
            mocks.ConnectionRepo.Object,
            mocks.DbWriter.Object,
            mocks.DiaParser.Object,
            mocks.RuleGroupPrinter.Object,
            mocks.FileFinder.Object);
        return (mocks, command);
    }

    private const String ExamplePath = "example/path/file.txt";

    [Test]
    public async Task Parse_GivenOnlyConnectionString_OnlyUploadsToDb()
    {
        const string connectionString = "key=value;key2=value too ; key3=value";
        TextReader sampleReader = new StringReader("aaaaaaaaaa");
        List<PhonologicalRuleGroup> rules = [new("a", "a", [])];
        var (mocks, command) = SetupApp();
        mocks.FileFinder.GetFile(Any()).Returns(sampleReader);
        mocks.DiaParser.Parse(Any()).Returns(rules);

        command.Parse(ExamplePath, connectionString, default);

        mocks.FileFinder.GetFile(ExamplePath).WasCalled();
        mocks.DiaParser.Parse(sampleReader).WasCalled();
        mocks.ConnectionRepo.CreateConnection(connectionString).WasCalled();
        mocks.DbWriter.Write(Any(), rules).WasCalled();
        mocks.RuleGroupPrinter.PrintCharacters(Any()).WasNeverCalled();
        mocks.RuleGroupPrinter.PrintDiacritics(Any()).WasNeverCalled();
        mocks.RuleGroupPrinter.PrintRuleGroups(Any(), Any(), Any()).WasNeverCalled();
    }

    [Test]
    public async Task Parse_GivenNoOptions_DefaultsToPrintAllRules()
    {
        const string? connectionString = null;
        PrintOptions options = default;
        var (mocks, command) = SetupApp();
        TextReader sampleReader = new StringReader("aaaaaaaaaa");
        List<PhonologicalRuleGroup> rules = [new("a", "a", [])];
        mocks.FileFinder.GetFile(Any()).Returns(sampleReader);
        mocks.DiaParser.Parse(Any()).Returns(rules);

        command.Parse(ExamplePath, connectionString, options);

        mocks.FileFinder.GetFile(ExamplePath).WasCalled();
        mocks.DiaParser.Parse(sampleReader).WasCalled();
        mocks.ConnectionRepo.CreateConnection(Any()).WasNeverCalled();
        mocks.DbWriter.Write(Any(), Any()).WasNeverCalled();
        mocks.RuleGroupPrinter.PrintCharacters(Any()).WasNeverCalled();
        mocks.RuleGroupPrinter.PrintDiacritics(Any()).WasNeverCalled();
        mocks.RuleGroupPrinter.PrintRuleGroups(rules, false, true).WasCalled();
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.AllNonselectedPrintOptionCases))]
    public async Task Parse_AnyPrintOptions_PrintsOnlySelected(PrintOptions options)
    {
        const string? connectionString = null;
        var (mocks, command) = SetupApp();
        TextReader sampleReader = new StringReader("aaaaaaaaaa");
        List<PhonologicalRuleGroup> rules = [new("a", "a", [])];
        mocks.FileFinder.GetFile(Any()).Returns(sampleReader);
        mocks.DiaParser.Parse(Any()).Returns(rules);

        command.Parse(ExamplePath, connectionString, options);

        mocks.FileFinder.GetFile(ExamplePath).WasCalled();
        mocks.DiaParser.Parse(sampleReader).WasCalled();
        mocks.ConnectionRepo.CreateConnection(Any()).WasNeverCalled();
        mocks.DbWriter.Write(Any(), Any()).WasNeverCalled();
        if (options.PrintCharacters)
            mocks.RuleGroupPrinter.PrintCharacters(rules).WasCalled();
        else
            mocks.RuleGroupPrinter.PrintCharacters(Any()).WasNeverCalled();
        if (options.PrintDiacritics)
            mocks.RuleGroupPrinter.PrintDiacritics(rules).WasCalled();
        else
            mocks.RuleGroupPrinter.PrintDiacritics(Any()).WasNeverCalled();
        if (options.PrintGroups || options.PrintRules)
            mocks.RuleGroupPrinter.PrintRuleGroups(rules, options.PrintGroups, options.PrintRules).WasCalled();
        else
            mocks.RuleGroupPrinter.PrintRuleGroups(rules, Any(), Any()).WasNeverCalled();
    }

    [Test]
    public async Task Parse_APrintOptionsAndConnectionString_UploadsAndPrints()
    {
        const string? connectionString = "key=value;key2=value too ; key3=value";
        PrintOptions options = new() { PrintCharacters = true };
        var (mocks, command) = SetupApp();
        TextReader sampleReader = new StringReader("aaaaaaaaaa");
        List<PhonologicalRuleGroup> rules = [new("a", "a", [])];
        mocks.FileFinder.GetFile(Any()).Returns(sampleReader);
        mocks.DiaParser.Parse(Any()).Returns(rules);

        command.Parse(ExamplePath, connectionString, options);

        mocks.FileFinder.GetFile(ExamplePath).WasCalled();
        mocks.DiaParser.Parse(sampleReader).WasCalled();
        mocks.ConnectionRepo.CreateConnection(connectionString).WasCalled();
        mocks.DbWriter.Write(Any(), rules).WasCalled();
        mocks.RuleGroupPrinter.PrintCharacters(rules).WasCalled();
        mocks.RuleGroupPrinter.PrintDiacritics(Any()).WasNeverCalled();
        mocks.RuleGroupPrinter.PrintRuleGroups(Any(), Any(), Any()).WasNeverCalled();
    }
}
