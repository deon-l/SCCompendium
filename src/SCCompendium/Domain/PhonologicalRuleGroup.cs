namespace SCCompendium.Domain;

public record struct PhonologicalRuleGroup(string Title, string Credit, List<PhonologicalRule> Rules)
{
    public override string ToString()
        => $"{Title} - {Credit}\n\t- {String.Join("\n\t- ", Rules.Select(rule => rule.Rule))}";
}
