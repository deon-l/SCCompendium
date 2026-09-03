using SCCompendium.Presentation.App.ArgumentModels;

namespace SCCompendium.Tests.Presentation.App.ArgumentModels;

public partial class PrintOptionsTest
{
    [Test]
    public async Task NoOptionsSelected_Default_ReturnsTrue()
    {
        PrintOptions options = default;

        bool result = options.NoOptionsSelected();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task NoOptionsSelected_MembersSetToFalse_ReturnsTrue()
    {
        PrintOptions options = new()
        {
            PrintDiacritics = false,
            PrintGroups = false,
            PrintCharacters = false,
            PrintRules = false,
        };

        bool result = options.NoOptionsSelected();

        await Assert.That(result).IsTrue();
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.NonEmptyOptionsCases))]
    public async Task NoOptionsSelected_SomeMembersSetTrue_ReturnsFalse(PrintOptions options)
    {
        // no setup

        bool result = options.NoOptionsSelected();

        await Assert.That(result).IsFalse();
    }
}
