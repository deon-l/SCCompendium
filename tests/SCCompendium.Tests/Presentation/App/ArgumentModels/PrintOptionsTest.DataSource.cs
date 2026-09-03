using SCCompendium.Presentation.App.ArgumentModels;

namespace SCCompendium.Tests.Presentation.App.ArgumentModels;

public partial class PrintOptionsTest
{
    public class DataSource
    {
        public static IEnumerable<Func<PrintOptions>> NonEmptyOptionsCases()
        {
            yield return () => new PrintOptions { PrintGroups = true };
            yield return () => new PrintOptions { PrintCharacters = true };
            yield return () => new PrintOptions { PrintDiacritics = true };
            yield return () => new PrintOptions { PrintRules = true };
            yield return () => new PrintOptions { PrintCharacters = true, PrintDiacritics = true };
            yield return () => new PrintOptions
                { PrintGroups = true, PrintRules = true, PrintCharacters = true, PrintDiacritics = true };
        }
    }
}
