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

    public bool Equals(PhonologicalRule other)
        => Rule == other.Rule
           && Note == other.Note
           && InputCharacters.Length == other.InputCharacters.Length &&
           InputCharacters.All(other.InputCharacters.Contains)
           && OutputCharacters.Length == other.OutputCharacters.Length &&
           OutputCharacters.All(other.OutputCharacters.Contains)
           && ContextCharacters.Length == other.ContextCharacters.Length &&
           ContextCharacters.All(other.ContextCharacters.Contains);

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(Rule, Note, InputCharacters.Length, OutputCharacters.Length, ContextCharacters.Length);
    }
}
