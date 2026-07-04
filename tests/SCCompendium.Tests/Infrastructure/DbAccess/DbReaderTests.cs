using System.Data.Common;
using Apps72.Dev.Data.DbMocker;
using SCCompendium.Application.DbAccess;
using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Infrastructure.DbAccess;
using SCCompendium.Tests.Domain.ValueObjects.DbValues;
using SCCompendium.Tests.Infrastructure.DbAccess.Helper;

namespace SCCompendium.Tests.Infrastructure.DbAccess;

public class DbReaderTests
{
    [Test]
    public async Task GetAllGroupDetails_SampleDb_ReturnsAllGroups()
    {
        BetterMockDbConnection connection = new();
        CommandCapturer capturer = new();
        DbNames n = new();
        var repo = IDbConnectionRepository.Mock();
        repo.GetConnection<DbConnection>().Returns(connection);
        PhonologicalRuleGroup g1 = new("Group 1", "Credit 1", [], "Note 1");
        PhonologicalRuleGroup g2 = new("Group 2", "Credit 2", [], "Note 2");
        PhonologicalRuleGroup g3 = new("A to B", "\thttps://asdf", []);
        connection.Mocks
            .HasValidSqlServerCommandText()
            .When(capturer.Any)
            .ReturnsTable(MockTable.WithColumns(n.RuleGroupColKey, n.RuleGroupColName, n.RuleGroupColCredit, n.RuleGroupColNote)
                .AddRow(1, g1.Title, g1.Credit, g1.Note)
                .AddRow(2, g2.Title, g2.Credit, g2.Note)
                .AddRow(1024, g3.Title, g3.Credit, g3.Note));
        DbReader reader = new();

        var result = reader.GetAllGroupDetails(repo);

        Console.WriteLine(capturer);
        await Assert.That(capturer).All().Satisfy(cmd => cmd.Satisfies(cmd => cmd.VerifyParameters()));
        await Assert.That(result).Count().IsEqualTo(3);
        foreach (var expectedGroup in (PhonologicalRuleGroup[])[g1, g2, g3])
        {
            await Assert.That(result).Contains(expectedGroup);
        }
    }

    [Test]
    public async Task GetAllCharacterDetails_SampleDb_ReturnsAllCharacters()
    {
        BetterMockDbConnection connection = new();
        DbNames n = new();
        CommandCapturer capturer = new();
        var repo = IDbConnectionRepository.Mock();
        repo.GetConnection<DbConnection>().Returns(connection);
        connection.Mocks
            .HasValidSqlServerCommandText()
            .When(capturer.Any)
            .ReturnsTable(MockTable.WithColumns(n.IpaCharacterColKey, n.IpaCharacterColSymbol, n.IpaCharacterColDiacritics)
                .AddRow(0, "a", "")
                .AddRow(1, "b", ":")
                .AddRow(11, "ch", ":;[+high]"));
        IpaCharacter[] expectedChars = [new("a", []), new("b", [":"]), new("ch", [":", "[+high]"])];
        DbReader reader = new();

        var results = reader.GetAllCharacterDetails(repo);

        Console.WriteLine(capturer);
        await Assert.That(capturer).All().Satisfy(cmd => cmd.Satisfies(cmd => cmd.VerifyParameters()));
        await Assert.That(results).Count().IsEqualTo(3);
        foreach (IpaCharacter expectedChar in expectedChars)
        {
            await Assert.That(results).Contains(expectedChar);
        }
    }

    private PhonologicalRule StripReferenceChars(PhonologicalRule rule)
        => rule with { InputCharacters = [], OutputCharacters = [], ContextCharacters = [] };

    [Test]
    public async Task FindRules_NoFilter_ReturnsAllRules()
    {
        BetterMockDbConnection connection = new() { HasValidSqlServerCommandText = true };
        CommandCapturer capturer = new();
        var repo = IDbConnectionRepository.Mock();
        repo.GetConnection<DbConnection>().Returns(connection);
        IpaCharacter
            charA = new("a", []),
            charB = new("b", [":"]),
            charC = new("c", [":", "[+high]"]);
        List<PhonologicalRuleGroup> g =
        [
            new("English to French", "credit 1", [
                new("a > b:", [charA], [charB], []),
                new("a > c:[+high] / b:_", [charA], [charC], [charB]),
            ]),
            new("English to French", "credit2", Note: "NB: hi", Rules:[
                new("ab > cc", [charA, charB], [charC], [], "note")
            ])
        ];
        DbNames n = new();
        connection.Mocks
            .When(capturer.If(cmd => cmd.CommandTextStartsWith(["SELECT"]) && cmd.SymbolAtContains(3, n.RuleGroupTable)))
            .ReturnsTable(MockTable
                .WithColumns(n.RuleGroupColKey, n.RuleGroupColName, n.RuleGroupColCredit, n.RuleGroupColNote)
                .AddRow(0, g[0].Title, g[0].Credit, g[0].Note)
                .AddRow(1, g[1].Title, g[1].Credit, g[1].Note));
        connection.Mocks
            .When(capturer.If(cmd => cmd.CommandTextStartsWith(["SELECT"]) && cmd.SymbolAtContains(3, n.IpaCharacterTable)))
            .ReturnsTable(MockTable
                .WithColumns(n.IpaCharacterColKey, n.IpaCharacterColSymbol, n.IpaCharacterColDiacritics)
                .AddRow(0, "a", "")
                .AddRow(1, "b", ":")
                .AddRow(2, "c", ":;[+high]"));
        connection.Mocks
            .When(capturer.Any)
            .ReturnsTable(MockTable.WithColumns(n.PhonologicalRuleColKey, n.PhonologicalRuleColRule, n.PhonologicalRuleColRuleNote, n.PhonologicalRuleColGroupKey)
                .AddRow(0, g[0].Rules[0].Rule, g[0].Rules[0].Note, 0)
                .AddRow(1, g[0].Rules[1].Rule, g[0].Rules[1].Note, 0)
                .AddRow(23, g[1].Rules[0].Rule, g[1].Rules[0].Note, 1)
            );
        DbReader reader = new();

        var results = reader.FindRules(repo, new());

        await Assert.That(results).Count().IsEqualTo(2);
        Console.WriteLine(results[0]);
        Console.WriteLine(results[1]);
        Console.WriteLine("-----");
        Console.WriteLine(capturer);
        await Assert.That(capturer).All().Satisfy(cmd => cmd.Satisfies(cmd => cmd.VerifyParameters()));
        foreach (var group in g)
        {
            Console.WriteLine(group);
            await Assert.That(results).Contains(g => g.Title == group.Title
                && g.Rules.Count == group.Rules.Count
                && g.Rules.All(group.Rules.Select(StripReferenceChars).Contains));
        }
    }

    [Test]
    public async Task FindRules_EnvironmentFilter_UsedInSqlCode()
    {
        BetterMockDbConnection connection = new() { HasValidSqlServerCommandText = true };
        var repo = IDbConnectionRepository.Mock();
        repo.GetConnection<DbConnection>().Returns(connection);
        IpaCharacter
            charA = new("a", []),
            charB = new("b", [":"]),
            charC = new("c", [":", "[+high]"]);
        List<PhonologicalRuleGroup> g =
        [
            new("English to French", "credit 1", [
                new("a > b:", [charA], [charB], []),
                new("a > c:[+high] / b:_", [charA], [charC], [charB]),
            ]),
            new("English to French", "credit2", Note: "NB: hi", Rules:[
                new("ab > cc", [charA, charB], [charC], [], "note")
            ])
        ];
        DbNames n = new();
        CommandCapturer miscCapturer = new();
        CommandCapturer capturer = new();
        connection.Mocks
            .When(miscCapturer.If(cmd => cmd.CommandTextStartsWith(["SELECT"]) && cmd.SymbolAtContains(3, n.RuleGroupTable)))
            .ReturnsTable(MockTable
                .WithColumns(n.RuleGroupColKey, n.RuleGroupColName, n.RuleGroupColCredit, n.RuleGroupColNote)
                .AddRow(0, g[0].Title, g[0].Credit, g[0].Note)
                .AddRow(1, g[1].Title, g[1].Credit, g[1].Note));
        connection.Mocks
            .When(miscCapturer.If(cmd => cmd.CommandTextStartsWith(["SELECT"]) && cmd.SymbolAtContains(3, n.IpaCharacterTable)))
            .ReturnsTable(MockTable
                .WithColumns(n.IpaCharacterColKey, n.IpaCharacterColSymbol, n.IpaCharacterColDiacritics)
                .AddRow(1, "b", ":"));
        connection.Mocks
            .When(capturer.Any)
            .ReturnsTable(MockTable.WithColumns(n.PhonologicalRuleColKey, n.PhonologicalRuleColRule, n.PhonologicalRuleColRuleNote, n.PhonologicalRuleColGroupKey)
                // .AddRow(0, g[0].Rules[0].Rule, g[0].Rules[0].Note, 0)
                .AddRow(1, g[0].Rules[1].Rule, g[0].Rules[1].Note, 0)
                .AddRow(23, g[1].Rules[0].Rule, g[1].Rules[0].Note, 1)
            );
        DbReader reader = new();
        CharacterSearch search = new() { Character = "b", Environment = CharacterEnvironment.Input | CharacterEnvironment.Context};

        var results = reader.FindRules(repo, search);

        Console.WriteLine(miscCapturer);
        Console.WriteLine("---");
        Console.WriteLine(capturer);
        Debug.Assert(capturer.Count == 1);
        await Assert.That(miscCapturer).All().Satisfy(cmd => cmd.Satisfies(trace => trace!.VerifyParameters()));
        await Assert.That(capturer).All().Satisfy(cmd => cmd.Satisfies(trace => trace!.VerifyParameters()));
        await Assert.That(capturer[0]).Satisfies(cmd =>
            cmd!.CommandText.Contains("input") && cmd.CommandText.Contains("context") && !cmd.CommandText.Contains("output")
            || (cmd.Parameters.Any(p => p.Value as string == "input")
                && cmd.Parameters.Any(p => p.Value as string == "context")
                && cmd.Parameters.All(p => p.Value as string != "output")));
        await Assert.That(results).Count().IsEqualTo(2);
        foreach (var group in (PhonologicalRuleGroup[])[g[1], g[0] with {Rules = [g[0].Rules[1]]}])
        {
            Console.WriteLine(group);
            await Assert.That(results).Contains(g => g.Title == group.Title
                && g.Rules.Count == group.Rules.Count
                && g.Rules.All(group.Rules.Select(StripReferenceChars).Contains));
        }
    }

    [Test]
    public async Task FindRules_DiacriticFilter_ValidSqlCode()
    {
        BetterMockDbConnection connection = new() { HasValidSqlServerCommandText = true };
        var repo = IDbConnectionRepository.Mock();
        repo.GetConnection<DbConnection>().Returns(connection);
        IpaCharacter
            charA = new("a", []),
            charB = new("b", [":"]),
            charC = new("c", [":", "[+high]"]);
        List<PhonologicalRuleGroup> g =
        [
            new("English to French", "credit 1", [
                new("a > b:", [charA], [charB], []),
                new("a > c:[+high] / b:_", [charA], [charC], [charB]),
            ]),
            new("English to French", "credit2", Note: "NB: hi", Rules:[
                new("ab > cc", [charA, charB], [charC], [], "note")
            ])
        ];
        DbNames n = new();
        CommandCapturer miscCapturer = new();
        CommandCapturer capturer = new();
        connection.Mocks
            .When(miscCapturer.If(cmd => cmd.CommandTextStartsWith(["SELECT"]) && cmd.SymbolAtContains(3, n.RuleGroupTable)))
            .ReturnsTable(MockTable
                .WithColumns(n.RuleGroupColKey, n.RuleGroupColName, n.RuleGroupColCredit, n.RuleGroupColNote)
                .AddRow(0, g[0].Title, g[0].Credit, g[0].Note)
                .AddRow(1, g[1].Title, g[1].Credit, g[1].Note));
        connection.Mocks
            .When(capturer.If(cmd => cmd.CommandTextStartsWith(["SELECT"]) && cmd.SymbolAtContains(3, n.IpaCharacterTable)))
            .ReturnsTable(MockTable
                .WithColumns(n.IpaCharacterColKey, n.IpaCharacterColSymbol, n.IpaCharacterColDiacritics)
                .AddRow(1, "b", ":"));
        connection.Mocks
            .When(miscCapturer.Any)
            .ReturnsTable(MockTable.WithColumns(n.PhonologicalRuleColKey, n.PhonologicalRuleColRule, n.PhonologicalRuleColRuleNote, n.PhonologicalRuleColGroupKey)
                .AddRow(0, g[0].Rules[0].Rule, g[0].Rules[0].Note, 0)
                .AddRow(1, g[0].Rules[1].Rule, g[0].Rules[1].Note, 0)
                .AddRow(23, g[1].Rules[0].Rule, g[1].Rules[0].Note, 1)
            );
        DbReader reader = new();
        CharacterSearch search = new() { Diacritics = [":", "[+high]"]};

        var results = reader.FindRules(repo, search);

        Console.WriteLine(miscCapturer);
        Console.WriteLine("---");
        Console.WriteLine(capturer);
        await Assert.That(miscCapturer).All().Satisfy(cmd => cmd.Satisfies(trace => trace!.VerifyParameters()));
        await Assert.That(capturer).All().Satisfy(cmd => cmd.Satisfies(trace => trace!.VerifyParameters()));
        await Assert.That(results).Count().IsEqualTo(2);
        foreach (var group in g)
        {
            Console.WriteLine(group);
            await Assert.That(results).Contains(g => g.Title == group.Title
                && g.Rules.Count == group.Rules.Count
                && g.Rules.All(group.Rules.Select(StripReferenceChars).Contains));
        }
    }
}
