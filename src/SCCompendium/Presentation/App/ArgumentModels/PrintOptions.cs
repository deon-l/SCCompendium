using CommandDotNet;
using SCCompendium.Domain.ValueObjects.Parsed;

namespace SCCompendium.Presentation.App.ArgumentModels;

/// <summary>
/// Options for how to print the received data.
/// </summary>
public record struct PrintOptions : IArgumentModel
{
    /// <summary>Whether other data of <see cref="PhonologicalRuleGroup"/> (e.g. titles, credit) should be printed.</summary>
    [Option('g', "print-groups")]
    public bool PrintGroups { get; set; }

    /// <summary>Whether the string representation of the <see cref="PhonologicalRule"/>s should be printed.</summary>
    [Option('r', "print-rules")]
    public bool PrintRules { get; set; }

    /// <summary>Whether to print all characters used in a collection of rules</summary>
    [Option('c', "print-chars")]
    public bool PrintCharacters { get; set; }

    /// <summary>Whether to print all diacritics/modifiers found in the collection of rules.</summary>
    [Option('d', "print-diacritics")]
    public bool PrintDiacritics { get; set; }

    /// <remarks>i.e. every member is <see langword="false"/></remarks>
    public bool NoOptionsSelected() => this == default;
}
