using System.Data;
using System.Data.Common;
using System.Text;
using SCCompendium.Application.DbAccess;
using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Tests.Domain.ValueObjects.DbValues;

namespace SCCompendium.Infrastructure.DbAccess;

public class DbReader : IDbReader
{
    // Todo: (with DbWriter) move this constant to Domain in proper class.
    private const char DiacriticDelimiter = ';';
    private readonly DbNames _names = new();

    private Dictionary<int, PhonologicalRuleGroup> GetGroups(DbConnection connection)
    {
        Dictionary<int, PhonologicalRuleGroup> groups = new();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = $@"SELECT * FROM {_names.RuleGroupTable}";

        using DbDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            Debug.Assert(reader.FieldCount == 4);
            int id = reader.GetInt32(_names.RuleGroupColKey);
            string name = reader.GetString(_names.RuleGroupColName);
            string credit = reader.GetString(_names.RuleGroupColCredit);
            string note = reader.GetString(_names.RuleGroupColNote);
            groups.Add(id, new(name, credit, [], note));
        }

        return groups;
    }
    public List<PhonologicalRuleGroup> GetAllGroupDetails(IDbConnectionRepository connectionRepo)
    {
        return GetGroups(connectionRepo.GetConnection<DbConnection>()).Values.ToList();
    }

    private string CreateCharFilter(CharacterSearch filter, DbCommand cmd)
    {
        string conditions = String.Empty;

        if (!String.IsNullOrWhiteSpace(filter.Character))
        {
            conditions = $" {_names.IpaCharacterColSymbol} = @character ";
            DbParameter character = cmd.CreateParameter();
            character.ParameterName = "character";
            character.Value = filter.Character;
            character.DbType = DbType.String;
            cmd.Parameters.Add(character);
        }

        if (filter.Diacritics.Length > 0)
        {
            if (conditions.Length > 0)
            {
                conditions += " AND ";
            }
            conditions += String.Join(" AND ", filter.Diacritics.Select(dia =>
                $"({_names.IpaCharacterColDiacritics} LIKE '{DiacriticDelimiter}%{dia}%{DiacriticDelimiter}')"));
        }

        if (conditions.Length > 0)
        {
            return "WHERE " + conditions;
        }

        return String.Empty;
    }

    /// <remarks><paramref name="filter"/> only filters on character and diacritics, not on Environment.</remarks>
    private Dictionary<int, IpaCharacter> GetCharacters(DbConnection connection, CharacterSearch filter)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = $@"SELECT * FROM {_names.IpaCharacterTable} {CreateCharFilter(filter, cmd)}";

        Dictionary<int, IpaCharacter> characters = new();
        using DbDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int id = reader.GetInt32(_names.IpaCharacterColKey);
            string character = reader.GetString(_names.IpaCharacterColSymbol);
            string diacriticsRaw = reader.GetString(_names.IpaCharacterColDiacritics);
            string[] diacritics = diacriticsRaw.Split(DiacriticDelimiter,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            characters.Add(id, new(character, diacritics));
        }

        return characters;
    }

    public List<IpaCharacter> GetAllCharacterDetails(IDbConnectionRepository connectionRepo)
    {
        return GetCharacters(connectionRepo.GetConnection<DbConnection>(), new()).Values.ToList();
    }

    public List<PhonologicalRuleGroup> FindRules(IDbConnectionRepository connectionRepo, CharacterSearch filter)
    {
        var connection = connectionRepo.GetConnection<DbConnection>();
        var groups = GetGroups(connection);
        var characters = GetCharacters(connection, filter);

        using DbCommand cmd = connection.CreateCommand();
        StringBuilder cmdText = new();
        cmdText.Append($@"
SELECT * FROM {_names.PhonologicalRuleTable}");

        if (!String.IsNullOrWhiteSpace(filter.Character) || filter.Diacritics.Length > 0 ||
            filter.Environment != CharacterEnvironment.All)
        {
            cmdText.Append($@"
WHERE EXISTS (
    SELECT {_names.RuleIpaReferenceColRuleKey} FROM {_names.PhonologicalRuleTable} 
    WHERE {_names.PhonologicalRuleColKey} = {_names.RuleIpaReferenceColRuleKey}");
            if (!(String.IsNullOrWhiteSpace(filter.Character) && filter.Diacritics.Length == 0))
            {
                cmdText.Append($@"
        AND {_names.RuleIpaReferenceColCharKey} IN ({String.Join(',', characters.Keys)})");
            }
            if (filter.Environment != CharacterEnvironment.All)
            {
                List<string> envFilters = new();
                if (filter.Environment.HasFlag(CharacterEnvironment.Input))
                    envFilters.Add("'input'");
                if (filter.Environment.HasFlag(CharacterEnvironment.Output))
                    envFilters.Add("'output'");
                if (filter.Environment.HasFlag(CharacterEnvironment.Context))
                    envFilters.Add("'context'");

                cmdText.Append($@"
        AND {_names.RuleIpaReferenceColType} IN ({String.Join(',', envFilters)})");
            }

            cmdText.Append($@"
)");
        }

        cmd.CommandText = cmdText.ToString();

        using DbDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int groupId = reader.GetInt32(_names.PhonologicalRuleColGroupKey);
            string rule = reader.GetString(_names.PhonologicalRuleColRule);
            string note = reader.GetString(_names.PhonologicalRuleColRuleNote);

            if (groups.TryGetValue(groupId, out var group))
            {
                group.Rules.Add(new(rule, [], [], [], note));
            }
            else
            {
                Console.Error.WriteLine($"Got a rule belonging to a non-existent group (Gid: {groupId}, rule: {rule}, note: {note})");
            }
        }

        return groups.Values.ToList();
    }
}
