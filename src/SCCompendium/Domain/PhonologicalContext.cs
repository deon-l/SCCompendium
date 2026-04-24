namespace SCCompendium.Domain;

public record struct PhonologicalContext(IpaCharacter[] PreContext, IpaCharacter[] PostContext);
