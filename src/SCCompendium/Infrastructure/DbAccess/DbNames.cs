namespace SCCompendium.Infrastructure.DbAccess;

/// <summary>
/// Represents the names of the tables and columns
/// </summary>
/// <seealso cref="DbWriter.InitiateDatabase"/>
public class DbNames
{
    public string RuleGroupTable => "RuleGroups";
    public string RuleGroupColKey => "Key";
    public string RuleGroupColName => "Name";
    public string RuleGroupColCredit => "Credit";
    public string RuleGroupColNote => "Note";

    public string PhonologicalRuleTable => "PhonologicalRules";

    public string IpaCharacterTable => "IpaCharacters";

    public string RuleIpaReferenceTable => "RuleIpaReferences";
}
