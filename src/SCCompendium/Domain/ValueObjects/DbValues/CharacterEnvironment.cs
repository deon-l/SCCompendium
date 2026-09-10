namespace SCCompendium.Tests.Domain.ValueObjects.DbValues;

/// <summary>
/// Represents placement in a phonological rule.
/// </summary>
[Flags]
public enum CharacterEnvironment
{
    None = 0,
    Input   = 0b_001,
    Output  = 0b_010,
    Context = 0b_100,
    All = Input | Output | Context
}
