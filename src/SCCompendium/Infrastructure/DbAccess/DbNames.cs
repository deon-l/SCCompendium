namespace SCCompendium.Infrastructure.DbAccess;

/// <summary>
/// Represents the names of the tables and columns
/// </summary>
/// <seealso cref="DbWriter.InitiateDatabase"/>
public class DbNames
{
    public string RuleGroupTable => "RuleGroups";
    public string RuleGroupTableKey => "Key";
    public string RuleGroupTableName => "Name";
    public string RuleGroupTableCredit => "Credit";
    public string RuleGroupTableNote => "Note";

    public string PhonologicalRuleTable => "PhonologicalRules";

    public string IpaCharacterTable => "IpaCharacters";

    public string RuleIpaReferenceTable => "RuleIpaReferences";
}
