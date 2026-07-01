using SCCompendium.Domain.ValueObjects.Parsed;

namespace SCCompendium.Application.Parser;

public interface IDiachronicaParser
{
    public List<PhonologicalRuleGroup> Parse(TextReader reader);
}
