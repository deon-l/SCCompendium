namespace SCCompendium.Domain.ValueObjects.Parsed;

public record struct PhonologicalRule(
    string Rule,
    IpaCharacter[] InputCharacters,
    IpaCharacter[] OutputCharacters,
    IpaCharacter[] ContextCharacters,
    string Note = "")
{
    /// <summary>Returns the string representation of all values in <see cref="PhonologicalRule"/>.</summary>
    public override string ToString()
    {
        return
            $"\"{Rule}\" {{Note: \"{Note}\", input: [{String.Join(',', InputCharacters)}], output: [{String.Join(',', OutputCharacters)}], context: [{String.Join(',', ContextCharacters)}]}}";
    }
}
