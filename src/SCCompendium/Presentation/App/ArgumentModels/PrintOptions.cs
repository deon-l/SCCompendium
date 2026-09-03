using CommandDotNet;

namespace SCCompendium.Presentation.App.ArgumentModels;

/// <summary>
/// Options for how to print the received data.
/// </summary>
public record struct PrintOptions : IArgumentModel
{
    [Option('g', "print-groups")]
    public bool PrintGroups { get; set; }

    [Option('r', "print-rules")]
    public bool PrintRules { get; set; }

    [Option('c', "print-chars")]
    public bool PrintCharacters { get; set; }

    [Option('d', "print-diacritics")]
    public bool PrintDiacritics { get; set; }

    public bool NoOptionsSelected() => this == default;
}
