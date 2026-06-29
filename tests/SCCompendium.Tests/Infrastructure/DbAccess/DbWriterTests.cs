using Apps72.Dev.Data.DbMocker;
using SCCompendium.Domain;
using SCCompendium.Infrastructure.DbAccess;

namespace SCCompendium.Tests.Infrastructure.DbAccess;

public class DbWriterTests
{
    [Test]
    public async Task InitiateDatabase_SampleCall_CorrectWrite()
    {
        MockDbConnection connection = new();
        CommandCapturer capturer = new();
        connection.Mocks
            .When(capturer.Any)
            .ReturnsScalar(-1);
        DbWriter writer = new();

        writer.InitiateDatabase(connection);

        await Assert.That(capturer.Count).IsEqualTo(4);
        await Assert.That(capturer.ToArray()).DoesNotContain(cmd => !(
            cmd.CommandText.TrimStart().StartsWith("CREATE TABLE", StringComparison.CurrentCultureIgnoreCase)
            && cmd.CommandText.TrimEnd().EndsWith(')')));
        DbNames n = new();
        await Assert.That(capturer.ToArray()).Contains(cmd => cmd.CommandText.Contains(n.IpaCharacterTable));
        await Assert.That(capturer.ToArray()).Contains(cmd => cmd.CommandText.Contains(n.PhonologicalRuleTable));
        await Assert.That(capturer.ToArray()).Contains(cmd => cmd.CommandText.Contains(n.RuleIpaReferenceTable));
        await Assert.That(capturer.ToArray()).Contains(cmd => cmd.CommandText.Contains(n.RuleGroupTable));
    }

    [Test]
    public async Task Write_EmptyGroupsList_PerformsNoInserts()
    {
        List<PhonologicalRuleGroup> sourceGroups = new();
        MockDbConnection connection = new();
        CommandCapturer capturer = new();
        connection.Mocks
            .HasValidSqlServerCommandText()
            .When(capturer.Any)
            .ReturnsScalar(0);
        DbWriter writer = new();

        writer.Write(connection, sourceGroups);

        await Assert.That(capturer.ToArray()).DoesNotContain(cmd
            => cmd.CommandText.Split()[0].Equals("INSERT", StringComparison.CurrentCultureIgnoreCase));
    }

    [Test]
    public async Task Write_GroupsWithNoRules_PerformsNoInserts()
    {
        List<PhonologicalRuleGroup> sourceGroups = new();
        MockDbConnection connection = new();
        CommandCapturer capturer = new();
        connection.Mocks
            .HasValidSqlServerCommandText()
            .When(capturer.Any)
            .ReturnsScalar(0);
        DbWriter writer = new();

        writer.Write(connection, sourceGroups);

        await Assert.That(capturer.ToArray()).DoesNotContain(cmd =>
        {
            var symbols = cmd.CommandText.Split();
            return symbols[0].Equals("INSERT", StringComparison.CurrentCultureIgnoreCase);
            // // NOTE: Provided if required behaviour changes to insert no rules (but groups are fine)
            // return symbols[0].Equals("INSERT", StringComparison.CurrentCultureIgnoreCase)
            //        && !symbols[2].Contains({new DbNames().RuleGroupTable});
        });
    }

    [Test]
    public async Task Write_SimpleSource_CorrectWrite()
    {
        const string groupTitle = "aa-title";
        const string groupCredit = "aa-credit";
        const string groupNote = "aa-note";
        const string ruleNote = "rule-note";
        const string rule = "a > b / b_";
        const int bIpaKey = 3;
        const int aIpaKey = 3;
        const int genericKey = 1;
        List<PhonologicalRuleGroup> sourceGroups = new()
        {
            new(groupTitle, groupCredit, Note: groupNote, Rules:
            [
                new PhonologicalRule(rule, [new("a", [])], [new("b", [])], [new("b", [])], ruleNote)
            ])
        };
        MockDbConnection connection = new() { HasValidSqlServerCommandText = true };
        CommandCapturer capturer = new();
        DbNames n = new();
        connection.Mocks
            .When(capturer.If(cmd => cmd.CommandTextStartsWith(["select", n.IpaCharacterColKey, "from", n.IpaCharacterTable])))
            .ReturnsScalar(cmd => cmd.Parameters.Any(p => "b" == p.Value as string) ? bIpaKey : aIpaKey);
        connection.Mocks
            .When(capturer.If(cmd => cmd.CommandTextStartsWith(["select"])))
            .ReturnsScalar(genericKey);
        connection.Mocks
            .When(capturer.Any)
            .ReturnsScalar(-1);
        DbWriter writer = new();

        writer.Write(connection, sourceGroups);

        foreach (MockCommand cmd in capturer.ToArray())
        {
            await Assert.That(cmd.Parameters).Count().IsEqualTo(cmd.CommandText.Count(c => c =='@'));
            await Assert.That(cmd.Parameters).All().Satisfy(that =>
                that.Satisfies(param => cmd.CommandText.Contains(param!.ParameterName)));
        }
        await Assert.That(capturer.ToArray()).Contains(cmd =>
            cmd.CommandTextStartsWith(["insert", "into"])
            && cmd.SymbolAtContains(2, n.RuleGroupTable)
            && cmd.Parameters.Any(p => groupTitle == p.Value as string)
            && cmd.Parameters.Any(p => groupCredit == p.Value as string)
            && cmd.Parameters.Any(p => groupNote == p.Value as string));
        await Assert.That(capturer.ToArray()).Contains(cmd =>
            cmd.CommandTextStartsWith(["insert", "into"])
            && cmd.SymbolAtContains(2, n.PhonologicalRuleTable)
            && cmd.Parameters.Any(p => "a > b" == p.Value as string)
            && cmd.Parameters.Any(p => ruleNote == p.Value as string));
        await Assert.That(capturer.ToArray()).Contains(cmd =>
            cmd.CommandTextStartsWith(["insert", "into"])
            && cmd.SymbolAtContains(2, n.IpaCharacterTable)
            && cmd.Parameters.Any(p => "a" == p.Value as string));
        await Assert.That(capturer.ToArray()).Contains(cmd =>
            cmd.CommandTextStartsWith(["insert", "into"])
            && cmd.SymbolAtContains(2, n.IpaCharacterTable)
            && cmd.Parameters.Any(p => "b" == p.Value as string));
        await Assert.That(capturer.ToArray()).Contains(cmd =>
            cmd.CommandTextStartsWith(["insert", "into"])
            && cmd.SymbolAtContains(2, n.RuleIpaReferenceTable)
            && cmd.Parameters.Any(p => genericKey == p.Value as int?)
            && cmd.Parameters.Any(p => "input" == p.Value as string)
            && cmd.Parameters.Any(p => aIpaKey == p.Value as int?));
        await Assert.That(capturer.ToArray()).Contains(cmd =>
            cmd.CommandTextStartsWith(["insert", "into"])
            && cmd.SymbolAtContains(2, n.RuleIpaReferenceTable)
            && cmd.Parameters.Any(p => genericKey == p.Value as int?)
            && cmd.Parameters.Any(p => "output" == p.Value as string)
            && cmd.Parameters.Any(p => bIpaKey == p.Value as int?));
        await Assert.That(capturer.ToArray()).Contains(cmd =>
            cmd.CommandTextStartsWith(["insert", "into"])
            && cmd.SymbolAtContains(2, n.RuleIpaReferenceTable)
            && cmd.Parameters.Any(p => genericKey == p.Value as int?)
            && cmd.Parameters.Any(p => "context" == p.Value as string)
            && cmd.Parameters.Any(p => bIpaKey == p.Value as int?));
    }
}
