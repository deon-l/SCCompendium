using System.Data;
using System.Data.Common;
using System.Text;

using SCCompendium.Domain;

namespace SCCompendium.Application;

public class DatabaseWriter
{
    public int WriteSections(Dictionary<string, (string credit, List<PhonologicalRule> rules)> source,
        DbConnection connection, string tableName)
    {
        const string titleParamName = "SectionTitle";
        const string creditParamName = "SectionCredit";

        using DbCommand command = connection.CreateCommand();
        StringBuilder sb = new($"INSERT INTO {tableName} (title, credit) VALUES ");

        int i = 0;
        foreach (var pair in source)
        {
            string title = pair.Key;
            string credit = pair.Value.credit;

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
}
