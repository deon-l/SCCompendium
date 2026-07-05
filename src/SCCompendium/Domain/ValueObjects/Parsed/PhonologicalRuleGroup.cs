namespace SCCompendium.Domain.ValueObjects.Parsed;

public record struct PhonologicalRuleGroup(string Title, string Credit, List<PhonologicalRule> Rules, string Note = "")
{
    /// <summary>Returns semi-formated string of all data contained in this instance.</summary>
    public override string ToString()
        => $"{Title} - {Credit}\n  ({Note})\n\t- {String.Join("\n\t- ", Rules.Select(rule => rule.ToString()))}";

    public bool Equals(PhonologicalRuleGroup other)
        => Title == other.Title
           && Credit == other.Credit
           && Note == other.Note
           && Rules.Count == other.Rules.Count
           && Rules.All(other.Rules.Contains);

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(Title, Credit, Note, Rules.Count);
    }
}
