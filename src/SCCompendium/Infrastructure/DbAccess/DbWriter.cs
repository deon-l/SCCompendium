using System.Data;
using System.Data.Common;
using System.Text;
using SCCompendium.Application.DbAccess;
using SCCompendium.Domain;

namespace SCCompendium.Infrastructure.DbAccess;

public class DbWriter : IDbWriter
{
    private readonly DbNames _names = new();
    public void InitiateDatabase(IDbConnection connection)
    {
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = @$"CREATE TABLE {_names.RuleGroupTable} (
  {_names.RuleGroupColKey} int PRIMARY KEY,
  {_names.RuleGroupColName} varchar(63) NOT NULL,
  {_names.RuleGroupColCredit} varchar(255) NOT NULL,
  {_names.RuleGroupColNote} varchar(255),
  UNIQUE ({_names.RuleGroupColName})
)";
            command.ExecuteNonQuery();
        }

        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = @$"CREATE TABLE {_names.PhonologicalRuleTable} (
  RuleId int PRIMARY KEY,
  Rule varchar(255) NOT NULL,
  Notes varchar(255),
  Unique (Rule),
  GroupId int FOREIGN KEY REFERENCES RuleGroups(Id)
)";
            command.ExecuteNonQuery();
        }

        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = @$"CREATE TABLE {_names.IpaCharacterTable} (
  CharId int PRIMARY KEY,
  Symbol varchar(2) NOT NULL,
  Diacritics varchar(255)
)
";
            command.ExecuteNonQuery();
        }

        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = @$"CREATE TABLE {_names.RuleIpaReferenceTable} (
  RuleId int,
  CharId int,
  Type set(input, output, context) NOT NULL,
  CONSTRAINT FKey_Rules
  FOREIGN KEY (RuleId) REFERENCES PhonologicalRules(RuleId),
  CONSTRAINT FKey_IpaChar
  FOREIGN KEY (CharId) REFERENCES IpaCharacters(CharId),
  CONSTRAINT Uniq UNIQUE (RuleId, CharId)
)";
            command.ExecuteNonQuery();
        }
    }

    public int WriteGroups(IDbConnection connection, List<PhonologicalRuleGroup> groups)
    {
        const string titleParamName = "SectionTitle";
        const string creditParamName = "SectionCredit";

        using IDbCommand command = connection.CreateCommand();
        StringBuilder sb = new($"INSERT INTO {_names.RuleGroupTable} ({_names.RuleGroupColName}, {_names.RuleGroupColCredit}) VALUES ");

        int i = 0;
        foreach (PhonologicalRuleGroup group in groups)
        {
            string title = group.Title;
            string credit = group.Credit;

            IDbDataParameter
                titleParam = command.CreateParameter(),
                creditParam = command.CreateParameter();

            titleParam.DbType = creditParam.DbType = DbType.AnsiString;
            titleParam.Value = title;
            creditParam.Value = credit;
            titleParam.ParameterName = $"@{titleParamName}{i}";
            creditParam.ParameterName = $"@{creditParamName}{i}";

            sb.Append($" ({titleParam.ParameterName}, {creditParam.ParameterName}),");
            command.Parameters.Add(titleParam);
            command.Parameters.Add(creditParam);
            i++;
        }

        sb[^1] = ';';
        command.CommandText = sb.ToString();
        return command.ExecuteNonQuery();
    }

    public void Write(IDbConnection connection, List<PhonologicalRuleGroup> rules)
    {
        throw new NotImplementedException();
    }
}
