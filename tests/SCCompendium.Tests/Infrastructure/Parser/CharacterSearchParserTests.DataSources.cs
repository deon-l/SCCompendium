using SCCompendium.Tests.Domain.ValueObjects.DbValues;

namespace SCCompendium.Tests.Infrastructure.Parser;

public partial class CharacterSearchParserTests
{
    public class DataSource
    {
        public IEnumerable<(string input, CharacterSearch search)> SampleCharacterSearchCases()
        {
            yield return ("a", new CharacterSearch{ Character = "a" });
            yield return ("pf", new CharacterSearch{ Character = "pf" });
            yield return ("[any]", new CharacterSearch{ Character = "[any]" });

            yield return ("æ", new CharacterSearch{ Character = "æ" });
            yield return ("ʔ", new CharacterSearch{ Character = "ʔ" });
            yield return ("ɸ", new CharacterSearch{ Character = "ɸ" });
            yield return ("!", new CharacterSearch{ Character = "!" });
            // this is different char from above ("latin letter retroflex click"), not testing as none of the
            // rest of app expects this.
            // yield return ("ǃ", new CharacterSearch{ Character = "ǃ" });
        }

        public IEnumerable<(string input, CharacterSearch search)> SampleDiacriticSearchCases()
        {
            yield return ("[+prop]", new CharacterSearch { Diacritics = ["[+prop]"] });
            yield return ("[+prop 1][+prop 2]", new CharacterSearch { Diacritics = ["[+prop 1]", "[+prop 2]"] });
            yield return ("ʰ", new CharacterSearch { Diacritics = ["ʰ"] });
            yield return ("̞", new CharacterSearch { Diacritics = ["̞"] });
            yield return ("̞̃", new CharacterSearch { Diacritics = ["̃", "̞"] });
            yield return ("[+prop]ʰ̃",  new CharacterSearch { Diacritics = ["[+prop]", "ʰ", "̃"] });
        }

        public IEnumerable<(string input, CharacterSearch search)> SampleEnvironmentSearchCases()
        {
            yield return ("[+input]",  new CharacterSearch { Environment = CharacterEnvironment.Input});
            yield return ("[+output]",  new CharacterSearch { Environment = CharacterEnvironment.Output});
            yield return ("[+context]",  new CharacterSearch { Environment = CharacterEnvironment.Context});
            yield return ("[-input]", new CharacterSearch { Environment = CharacterEnvironment.Output | CharacterEnvironment.Context});
            yield return ("[-output]", new CharacterSearch { Environment = CharacterEnvironment.Input | CharacterEnvironment.Context});
            yield return ("[-context]", new CharacterSearch { Environment = CharacterEnvironment.Input | CharacterEnvironment.Output});

            yield return ("[+input][+context]", new CharacterSearch { Environment = CharacterEnvironment.Input | CharacterEnvironment.Context});
            yield return ("[-input][-output]",  new CharacterSearch { Environment = CharacterEnvironment.Context});
        }
    }
}
