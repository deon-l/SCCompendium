using System.Data.Common;
using Apps72.Dev.Data.DbMocker;
using SCCompendium.Application;
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
    }

    [Test]
    public async Task WriteSections_SampleSource_CorrectWriteAndCount()
    {
        string longCredit = new ('d', 100);
        string longTitle = new string('e', 100);
        DbWriter writer = new();
        List<PhonologicalRuleGroup> source = new()
        {
            new("", "", [], ""),
            new("aaa", "bbb", []),
            new("ccc", longCredit, []),
            new(longTitle, "fff", [default, default])
        };
        MockDbConnection connection = new();
        CommandCapturer capturer = new();
        connection.Mocks
            .HasValidSqlServerCommandText()
            .When(capturer.Any)
            .ReturnsScalar(source.Count);

       int modifiedCount = writer.WriteGroups(connection, source);

       await Assert.That(capturer).IsNotNull();
       await Assert.That(modifiedCount).IsEqualTo(source.Count);

       string[] capturedCommandParts = capturer[0].CommandText.Split(' ', StringSplitOptions.TrimEntries);
       await Assert.That(capturedCommandParts[0]).IsEqualTo("INSERT", StringComparison.CurrentCultureIgnoreCase);
       await Assert.That(capturedCommandParts[1]).IsEqualTo("INTO", StringComparison.CurrentCultureIgnoreCase);
       await Assert.That(capturedCommandParts[2]).IsEqualTo(new DbNames().RuleGroupTable);
       await Assert.That(capturer[0].CommandText).Contains("VALUE", StringComparison.CurrentCultureIgnoreCase);
       await Assert.That(capturer[0].Parameters)
           .Contains(p => "aaa".Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase))
           .And.Contains(p => "bbb".Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase))
           .And.Contains(p => "ccc".Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase))
           .And.Contains(p => longTitle.Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase))
           .And.Contains(p => longCredit.Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase))
           .And.Contains(p => "fff".Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase));
    }
}
