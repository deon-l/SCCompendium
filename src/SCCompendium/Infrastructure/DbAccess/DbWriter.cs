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
        throw new NotImplementedException();
    }

    public int WriteGroups(DbConnection connection, List<PhonologicalRuleGroup> groups)
    {
        const string titleParamName = "SectionTitle";
        const string creditParamName = "SectionCredit";

        using DbCommand command = connection.CreateCommand();
        StringBuilder sb = new($"INSERT INTO {_names.RuleGroupTable} ({_names.RuleGroupColName}, {_names.RuleGroupColCredit}) VALUES ");

        int i = 0;
        foreach (PhonologicalRuleGroup group in groups)
        {
            string title = group.Title;
            string credit = group.Credit;

            DbParameter
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
