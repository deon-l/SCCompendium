namespace SCCompendium.Domain.ValueObjects.Parsed;

public record struct PhonologicalRuleGroup(string Title, string Credit, List<PhonologicalRule> Rules, string Note = "")
{
    /// <summary>Returns semi-formated string of all data contained in this instance.</summary>
    public override string ToString()
        => $"{Title} - {Credit}\n  ({Note})\n\t- {String.Join("\n\t- ", Rules.Select(rule => rule.ToString()))}";
}
