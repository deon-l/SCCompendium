using SCCompendium.Domain.ValueObjects.Parsed;

namespace SCCompendium.Application.Parser;

public interface IPhonologicalRuleParser
{
    /// <remarks>
    /// If returns <see langword="false"/>, <paramref name="rule"/> may be in an invalid state
    /// (i.e. some non-nullable fields are set to null.)
    /// </remarks>
    public bool TryParseRule(string source, out PhonologicalRule rule);
}
