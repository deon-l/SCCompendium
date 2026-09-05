using SCCompendium.Presentation.App.ArgumentModels;

namespace SCCompendium.Tests.Presentation.App;

public partial class AppParseTests
{
    public class DataSource
    {
        public IEnumerable<Func<PrintOptions>> AllNonselectedPrintOptionCases()
        {
            for (int i = 0; i <= 0b1111; i++)
            {
                PrintOptions options = new()
                {
                    PrintGroups = (i & 0b0001) > 0,
                    PrintRules = (i & 0b0010) > 0,
                    PrintCharacters = (i & 0b0100) > 0,
                    PrintDiacritics = (i & 0b1000) > 0,
                };
                if (options.NoOptionsSelected())
                {
                    continue;
                }

                yield return () => options;
            }
        }
    }
}
