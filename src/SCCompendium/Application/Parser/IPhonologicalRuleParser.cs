using SCCompendium.Domain.ValueObjects.Parsed;

namespace SCCompendium.Application.Parser;

public interface IPhonologicalRuleParser
{
    public bool TryParseRule(string source, out PhonologicalRule rule);
}
