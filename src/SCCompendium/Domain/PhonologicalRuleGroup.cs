namespace SCCompendium.Domain;

public record struct PhonologicalRuleGroup(string Title, string Credit, List<PhonologicalRule> Rules);
