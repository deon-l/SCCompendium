namespace SCCompendium.Domain;

public record struct PhonologicalRule(
    string Rule,
    IpaCharacter[] InputCharacters,
    IpaCharacter[] OutputCharacters,
    IpaCharacter[] ContextCharacters,
    string Note = "");
