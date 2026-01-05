public record struct PhonologicalRule(
    string Rule,
    List<string> InputCharacters,
    List<string> OutputCharacters,
    List<string> ContextCharacters,
    string Prenote = "");
