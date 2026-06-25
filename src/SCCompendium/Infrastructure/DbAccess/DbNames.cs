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
    public string PhonologicalRuleColKey => "Key";
    public string PhonologicalRuleColRule => "Rule";
    public string PhonologicalRuleColRuleNote => "Note";
    public string PhonologicalRuleColGroupKey => "GroupKey";

    public string IpaCharacterTable => "IpaCharacters";
    public string IpaCharacterColKey => "Key";
    public string IpaCharacterColSymbol => "Symbol";
    public string IpaCharacterColDiacritics => "Diacritics";

    public string RuleIpaReferenceTable => "RuleIpaReferences";
    public string RuleIpaReferenceColRuleKey => "RuleKey";
    public string RuleIpaReferenceColCharKey => "CharKey";
    public string RuleIpaReferenceColType => "Type";
}
