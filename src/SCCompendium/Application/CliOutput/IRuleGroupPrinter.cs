using SCCompendium.Domain.ValueObjects.Parsed;

namespace SCCompendium.Application.CliOutput;

/// <summary>
/// Prints out the data stored in a list of <see cref="PhonologicalRuleGroup"/>
/// </summary>
/// <remarks>Each method here <i><b>must</b></i> also print an associated header.</remarks>
public interface IRuleGroupPrinter
{
    /// <summary>
    /// Print out the data of the groups in <paramref name="ruleGroups"/>,
    /// printing out the rules and/or other data of <see cref="PhonologicalRuleGroup"/>.
    /// </summary>
    /// <param name="ruleGroups">The data to print.</param>
    /// <param name="printGroupTitles">Choose to print out non-rule data of <see cref="PhonologicalRuleGroup"/>.</param>
    /// <param name="printRules">Choose to print out the rules themselves.</param>
    public void PrintRuleGroups(List<PhonologicalRuleGroup> ruleGroups, bool printGroupTitles, bool printRules);

    /// <summary>
    /// Print out all characters and diacritic variations used in <paramref name="ruleGroups"/>
    /// </summary>
    public void PrintCharacters(List<PhonologicalRuleGroup> ruleGroups);

    /// <summary>
    /// Print out all diacritics used in <paramref name="ruleGroups"/>
    /// </summary>
    public void PrintDiacritics(List<PhonologicalRuleGroup> ruleGroups);
}
