namespace SCCompendium.Infrastructure.DbAccess;

/// <summary>
/// Represents the names of the tables and columns
/// </summary>
/// <seealso cref="DbWriter.InitiateDatabase"/>
public class DbNames
{
    public string RuleGroupTable => "RuleGroups";
    public string RuleGroupColKey => "Id";
    public string RuleGroupColName => "Name";
    public string RuleGroupColCredit => "Credit";
    public string RuleGroupColNote => "Note";

    public string PhonologicalRuleTable => "PhonologicalRules";
    public string PhonologicalRuleColKey => "Id";
    public string PhonologicalRuleColRule => "RuleStr";
    public string PhonologicalRuleColRuleNote => "Note";
    public string PhonologicalRuleColGroupKey => "GroupId";

    public string IpaCharacterTable => "IpaCharacters";
    public string IpaCharacterColKey => "Id";
    public string IpaCharacterColSymbol => "Symbol";
    public string IpaCharacterColDiacritics => "Diacritics";

    public string RuleIpaReferenceTable => "RuleIpaReferences";
    public string RuleIpaReferenceColRuleKey => "RuleId";
    public string RuleIpaReferenceColCharKey => "CharId";
    public string RuleIpaReferenceColType => "Type";
}
