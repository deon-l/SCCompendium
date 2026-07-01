using System.Data;
using System.Data.Common;
using SCCompendium.Application.DbAccess;
using SCCompendium.Domain;

namespace SCCompendium.Infrastructure.DbAccess;

public class DbWriter : IDbWriter
{
    private const char DiacriticDelimiter = ';';
    private readonly DbNames _names = new();
    public void InitiateDatabase(IDbConnectionRepository connectionRepo)
    {
        var connection = connectionRepo.GetConnection<DbConnection>();
        using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = @$"CREATE TABLE {_names.RuleGroupTable} (
  {_names.RuleGroupColKey} int AUTO_INCREMENT PRIMARY KEY,
  {_names.RuleGroupColName} varchar(63) NOT NULL,
  {_names.RuleGroupColCredit} varchar(255) NOT NULL,
  {_names.RuleGroupColNote} varchar(255),
  UNIQUE ({_names.RuleGroupColName})
)";
            command.ExecuteNonQuery();
        }

        using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = @$"CREATE TABLE {_names.PhonologicalRuleTable} (
  {_names.PhonologicalRuleColKey} int AUTO_INCREMENT PRIMARY KEY,
  {_names.PhonologicalRuleColRule} varchar(255) NOT NULL,
  {_names.PhonologicalRuleColRuleNote} varchar(255),
  {_names.PhonologicalRuleColGroupKey} int,
  CONSTRAINT FKey_Groups 
      FOREIGN KEY ({_names.PhonologicalRuleColGroupKey}) REFERENCES {_names.RuleGroupTable}({_names.RuleGroupColKey}),
)";
            command.ExecuteNonQuery();
        }

        using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = @$"CREATE TABLE {_names.IpaCharacterTable} (
  {_names.IpaCharacterColKey} int AUTO_INCREMENT PRIMARY KEY,
  {_names.IpaCharacterColSymbol} varchar(2) NOT NULL,
  {_names.IpaCharacterColDiacritics} varchar(255),
  CONSTRAINT Uniq
      UNIQUE ({_names.IpaCharacterColSymbol}, {_names.IpaCharacterColDiacritics})
)";
            command.ExecuteNonQuery();
        }

        using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = @$"CREATE TABLE {_names.RuleIpaReferenceTable} (
  {_names.RuleIpaReferenceColRuleKey} int,
  {_names.RuleIpaReferenceColCharKey} int,
  {_names.RuleIpaReferenceColType} enum('input', 'output', 'context') NOT NULL,
  CONSTRAINT FKey_Rules
      FOREIGN KEY ({_names.RuleIpaReferenceColRuleKey}) REFERENCES {_names.PhonologicalRuleTable}({_names.PhonologicalRuleColKey}),
  CONSTRAINT FKey_IpaChar
      FOREIGN KEY ({_names.RuleIpaReferenceColCharKey}) REFERENCES {_names.IpaCharacterTable}({_names.IpaCharacterColKey}),
  CONSTRAINT Uniq 
      UNIQUE ({_names.RuleIpaReferenceColRuleKey}, {_names.RuleIpaReferenceColCharKey}, {_names.RuleIpaReferenceColType})
)";
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Populates the <see cref="DbNames.RuleGroupTable"/>.
    /// </summary>
    public void WriteGroups(DbConnection connection, List<PhonologicalRuleGroup> groups)
    {
        const string titleParamName = "SectionTitle";
        const string creditParamName = "SectionCredit";

        if (groups.Count == 0)
        {
            return;
        }

        using DbCommand command = connection.CreateCommand();
        command.CommandText =
            $@"INSERT INTO {_names.RuleGroupTable} ({_names.RuleGroupColName}, {_names.RuleGroupColCredit}, {_names.RuleGroupColNote}) 
VALUES (@name, @credit, @note);";

        DbParameter
            titleParam = command.CreateParameter(),
            creditParam = command.CreateParameter(),
            noteParam = command.CreateParameter();

        titleParam.DbType = creditParam.DbType = noteParam.DbType = DbType.AnsiString;
        titleParam.ParameterName = "@name";
        creditParam.ParameterName = "@credit";
        noteParam.ParameterName = "@note";

        command.Parameters.Add(titleParam);
        command.Parameters.Add(creditParam);
        command.Parameters.Add(noteParam);

        foreach (PhonologicalRuleGroup group in groups)
        {
            titleParam.Value = group.Title;
            creditParam.Value = group.Credit;
            noteParam.Value = group.Note;

            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Populates the <see cref="DbNames.PhonologicalRuleTable"/>.
    /// </summary>
    public void WriteRuleHeaders(DbConnection connection, int groupKey, List<PhonologicalRule> rules)
    {
        using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = $@"INSERT INTO {_names.PhonologicalRuleTable}
VALUES (@rule, @note, @groupKey);";
            DbParameter
                ruleParam = command.CreateParameter(),
                noteParam = command.CreateParameter(),
                groupParam = command.CreateParameter();

            ruleParam.DbType = DbType.AnsiString;
            ruleParam.ParameterName = "@rule";

            noteParam.DbType = DbType.AnsiString;
            noteParam.ParameterName = "@note";

            groupParam.DbType = DbType.Int32;
            groupParam.ParameterName = "@groupKey";
            groupParam.Value = groupKey;

            command.Parameters.Add(ruleParam);
            command.Parameters.Add(noteParam);
            command.Parameters.Add(groupParam);

            foreach (PhonologicalRule rule in rules)
            {
                ruleParam.Value = rule.Rule;
                noteParam.Value = rule.Note;

                command.ExecuteNonQuery();
            }
        }
    }

    private string NormalizeDiacritics(string[] diacritics)
        => String.Join(DiacriticDelimiter, diacritics);

    /// <summary>
    /// Populates the <see cref="DbNames.IpaCharacterTable"/>
    /// </summary>
    public void WriteIpaChars(DbConnection connection, List<PhonologicalRuleGroup> groups)
    {
        var characters = groups.Aggregate(new HashSet<IpaCharacter>(), static (aggregate, group) =>
            group.Rules.Aggregate(aggregate, static (subaggregate, rule) =>
            {
                subaggregate.UnionWith(rule.InputCharacters);
                subaggregate.UnionWith(rule.OutputCharacters);
                subaggregate.UnionWith(rule.ContextCharacters);
                return subaggregate;
            }));

        if (characters.Count == 0)
        {
            return;
        }

        using DbCommand command = connection.CreateCommand();
        command.CommandText = $@"INSERT INTO {_names.IpaCharacterTable} 
    ({_names.IpaCharacterColSymbol}, {_names.IpaCharacterColDiacritics})
    VALUES (@symbol, @diacritics)";

        DbParameter
            symbolParam = command.CreateParameter(),
            diacriticParam = command.CreateParameter();

        symbolParam.DbType = DbType.AnsiString;
        symbolParam.ParameterName = "@symbol";

        diacriticParam.DbType = DbType.AnsiString;
        diacriticParam.ParameterName = "@diacritics";

        command.Parameters.Add(symbolParam);
        command.Parameters.Add(diacriticParam);

        foreach (var character in characters)
        {
            Debug.Assert(character.Diacritics.All(str => !str.Contains(DiacriticDelimiter)));
            string diacritics = NormalizeDiacritics(character.Diacritics);
            Debug.Assert(character.Character.Length <= 2);
            Debug.Assert(diacritics.Length <= 255);

            symbolParam.Value = character.Character;
            diacriticParam.Value = diacritics;

            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Populates the <see cref="DbNames.RuleIpaReferenceTable"/>.
    /// </summary>
    /// <remarks>Assumes <see cref="WriteIpaChars"/> and <see cref="WriteRuleHeaders"/> were called prior.</remarks>
    public void WriteRuleIpaReferences(DbConnection connection, int groupKey, List<PhonologicalRule> rules)
    {
        using DbCommand getRuleKeyCommand = connection.CreateCommand();
        getRuleKeyCommand.CommandText = $@"SELECT {_names.PhonologicalRuleColKey} FROM {_names.PhonologicalRuleTable}
WHERE {_names.PhonologicalRuleColRule} = @rule AND {_names.PhonologicalRuleColGroupKey} = @groupKey";

        DbParameter
            ruleParam = getRuleKeyCommand.CreateParameter(),
            groupKeyParam = getRuleKeyCommand.CreateParameter();

        ruleParam.DbType = DbType.AnsiString;
        ruleParam.ParameterName = "@rule";
        groupKeyParam.DbType = DbType.Int32;
        groupKeyParam.ParameterName = "@groupKey";
        groupKeyParam.Value = groupKey;

        getRuleKeyCommand.Parameters.Add(ruleParam);
        getRuleKeyCommand.Parameters.Add(groupKeyParam);

        using DbCommand getCharKeyCommand = connection.CreateCommand();
        getCharKeyCommand.CommandText = $@"SELECT {_names.IpaCharacterColKey} FROM {_names.IpaCharacterTable}
WHERE {_names.IpaCharacterColSymbol} = @symbol AND {_names.IpaCharacterColDiacritics} = @diacritics";

        DbParameter
            symbolParam = getCharKeyCommand.CreateParameter(),
            diacriticParam = getCharKeyCommand.CreateParameter();

        symbolParam.DbType = DbType.AnsiString;
        symbolParam.ParameterName = "@symbol";
        diacriticParam.DbType = DbType.AnsiString;
        diacriticParam.ParameterName = "@diacritics";
        getCharKeyCommand.Parameters.Add(symbolParam);
        getCharKeyCommand.Parameters.Add(diacriticParam);

        using DbCommand insertRefCommand = connection.CreateCommand();
        insertRefCommand.CommandText = $@"INSERT INTO {_names.RuleIpaReferenceTable}
VALUES (@ruleKey, @charKey, @type)";

        DbParameter
            ruleKeyParam = insertRefCommand.CreateParameter(),
            charKeyParam = insertRefCommand.CreateParameter(),
            typeParam = insertRefCommand.CreateParameter();
        ruleKeyParam.DbType = DbType.Int32;
        ruleKeyParam.ParameterName = "@ruleKey";
        charKeyParam.DbType = DbType.Int32;
        charKeyParam.ParameterName = "@charKey";
        typeParam.DbType = DbType.AnsiString;
        typeParam.ParameterName = "@type";
        insertRefCommand.Parameters.Add(ruleKeyParam);
        insertRefCommand.Parameters.Add(charKeyParam);
        insertRefCommand.Parameters.Add(typeParam);

        foreach (PhonologicalRule rule in rules)
        {
            ruleParam.Value = rule.Rule;
            int ruleKey = (int)getRuleKeyCommand.ExecuteScalar()!;
            ruleKeyParam.Value = ruleKey;

            typeParam.Value = "input";
            foreach (IpaCharacter character in rule.InputCharacters)
            {
                symbolParam.Value = character.Character;
                diacriticParam.Value = NormalizeDiacritics(character.Diacritics);

                int charKey = (int)getCharKeyCommand.ExecuteScalar()!;
                charKeyParam.Value = charKey;

                insertRefCommand.ExecuteNonQuery();
            }
            typeParam.Value = "output";
            foreach (IpaCharacter character in rule.OutputCharacters)
            {
                symbolParam.Value = character.Character;
                diacriticParam.Value = NormalizeDiacritics(character.Diacritics);

                int charKey = (int)getCharKeyCommand.ExecuteScalar()!;
                charKeyParam.Value = charKey;

                insertRefCommand.ExecuteNonQuery();
            }
            typeParam.Value = "context";
            foreach (IpaCharacter character in rule.ContextCharacters)
            {
                symbolParam.Value = character.Character;
                diacriticParam.Value = NormalizeDiacritics(character.Diacritics);

                int charKey = (int)getCharKeyCommand.ExecuteScalar()!;
                charKeyParam.Value = charKey;

                insertRefCommand.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Populates the <see cref="DbNames.PhonologicalRuleTable"/> and <see cref="DbNames.RuleIpaReferenceTable"/>.
    /// </summary>
    /// <remarks>Assumes <see cref="WriteGroups"/> and <see cref="WriteIpaChars"/> was called prior.</remarks>
    public void WriteRules(DbConnection connection, string groupName, List<PhonologicalRule> rules)
    {
        if (rules.Count == 0)
        {
            return;
        }

        int groupKey;
        using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = @$"SELECT {_names.RuleGroupColKey} FROM {_names.RuleGroupTable}
WHERE {_names.RuleGroupColName} = @name";
            DbParameter param = command.CreateParameter();
            param.DbType = DbType.AnsiString;
            param.Value = groupName;
            param.ParameterName = "@name";
            command.Parameters.Add(param);

            object? result = command.ExecuteScalar();
            if (result is not int)
            {
                return;
            }
            groupKey = (int)result;
        }

        WriteRuleHeaders(connection, groupKey, rules);
        WriteRuleIpaReferences(connection, groupKey, rules);
    }

    public void Write(IDbConnectionRepository connectionRepo, List<PhonologicalRuleGroup> groups)
    {
        var connection = connectionRepo.GetConnection<DbConnection>();
        WriteGroups(connection, groups);
        WriteIpaChars(connection, groups);
        foreach (var group in groups)
        {
            WriteRules(connection, group.Title, group.Rules);
        }
    }
}
