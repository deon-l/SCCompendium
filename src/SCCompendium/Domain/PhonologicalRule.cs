namespace SCCompendium.Domain;

public record struct PhonologicalRule(
    string Rule,
    IpaCharacter[] InputCharacters,
    IpaCharacter[] OutputCharacters,
    PhonologicalContext ContextCharacters,
    PhonologicalContext ExceptionCharacters,
    string Note = "");
