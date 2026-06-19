using System.Data.Common;
using Apps72.Dev.Data.DbMocker;
using SCCompendium.Application;
using SCCompendium.Domain;
using SCCompendium.Infrastructure.DbAccess;

namespace SCCompendium.Tests.Infrastructure.DbAccess;

public class DbWriterTests
{
    public class CommandCapturer
    {
        public string? CommandText { get; private set; }
        public IEnumerable<DbParameter>? Parameters { get; private set; }
        public bool HasCapturedMultipleCommands { get; private set; }
        public bool Capture(MockCommand command)
        {
            if (CommandText is not null)
            {
                HasCapturedMultipleCommands = true;
            }
            CommandText = command.CommandText;
            Parameters = command.Parameters;
            return true;
        }
    }

    [Test]
    public async Task WriteSections_SampleSource_CorrectWriteAndCount()
    {
        const string sampleTableName = "abcdef";
        string longCredit = new ('d', 100);
        string longTitle = new string('e', 100);
        DbWriter writer = new();
        Dictionary<string, (string, List<PhonologicalRule>)> source = new()
        {
            {"", ("", null!)},
            {"aaa", ("bbb", null!)},
            {"ccc", (longCredit, [])},
            {longTitle, ("fff", [default, default])}
        };
        MockDbConnection connection = new();
        CommandCapturer capturer = new();
        connection.Mocks
            .HasValidSqlServerCommandText()
            .When(capturer.Capture)
            .ReturnsScalar(source.Count);

       int modifiedCount = writer.WriteGroups(source, connection, sampleTableName);

       await Assert.That(capturer).IsNotNull();
       await Assert.That(modifiedCount).IsEqualTo(source.Count);
       string[] capturedCommandParts = capturer.CommandText.Split(' ', StringSplitOptions.TrimEntries);
       await Assert.That(capturedCommandParts[0]).IsEqualTo("INSERT", StringComparison.CurrentCultureIgnoreCase);
       await Assert.That(capturedCommandParts[1]).IsEqualTo("INTO", StringComparison.CurrentCultureIgnoreCase);
       await Assert.That(capturedCommandParts[2]).IsEqualTo(sampleTableName);
       await Assert.That(capturer.CommandText).Contains("VALUE", StringComparison.CurrentCultureIgnoreCase);
       await Assert.That(capturer.Parameters)
           .Contains(p => "aaa".Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase))
           .And.Contains(p => "bbb".Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase))
           .And.Contains(p => "ccc".Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase))
           .And.Contains(p => longTitle.Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase))
           .And.Contains(p => longCredit.Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase))
           .And.Contains(p => "fff".Equals((string)p.Value!, StringComparison.CurrentCultureIgnoreCase));
    }
}
