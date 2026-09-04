
using System.Text;
using CommandDotNet.TestTools;
using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Infrastructure.CliOutput;

namespace SCCompendium.Tests.Infrastructure.CliOutput;

public partial class RuleGroupPrinterTests
{
    private string SomeName(int id) => $"(name-{id})";

    [Test]
    [Arguments(true, false)]
    [Arguments(false, true)]
    [Arguments(true, true)]
    public async Task PrintRuleGroup_EmptyCollection_PrintsHeader(bool printGroupTitles, bool printRules)
    {
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);
        List<PhonologicalRuleGroup> groups = new();

        printer.PrintRuleGroups(groups, printGroupTitles, printRules);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        await Assert.That(output!.Count('\n'.Equals)).IsEqualTo(1);
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    public async Task PrintRuleGroup_EmptyGroupAndPrintRules_PrintsOnlyHeader()
    {
        const bool printGroupTitles = false;
        const bool printRules = true;
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);
        List<PhonologicalRuleGroup> groups =
        [
            new(DataSource.BadIndicator, DataSource.BadIndicator, []),
            new(DataSource.BadIndicator, DataSource.BadIndicator, []),
            new(DataSource.BadIndicator, DataSource.BadIndicator, []),
            new(DataSource.BadIndicator, DataSource.BadIndicator, []),
            new(DataSource.BadIndicator, DataSource.BadIndicator, []),
        ];

        printer.PrintRuleGroups(groups, printGroupTitles, printRules);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        await Assert.That(output).DoesNotContain(DataSource.BadIndicator);
        await Assert.That(output!.Count('\n'.Equals)).IsEqualTo(1);
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    public async Task PrintRules_SampleCollectionAndPrintRules_PrintsOnlyRules()
    {
        const bool printGroupTitles = false;
        const bool printRules = true;
        const string badIndicator = "z";
        Debug.Assert(!SomeName(0).Contains(badIndicator));
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);
        List<PhonologicalRuleGroup> groups = new()
        {
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(SomeName(1), [], [], []),
                new(SomeName(2), [], [], []),
                new(SomeName(3), [], [], []),
            ]),
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(SomeName(4), [], [], []),
            ]),
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(SomeName(5), [], [], []),
                new(SomeName(6), [], [], []),
            ]),
        };

        printer.PrintRuleGroups(groups, printGroupTitles, printRules);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        await Assert.That(output).DoesNotContain(badIndicator);
        foreach (int i in Enumerable.Range(1, 6))
        {
            Console.WriteLine("case: " + i);
            await Assert.That(output).Contains(SomeName(i));
        }
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    public async Task PrintRules_SampleCollectionAndPrintGroups_PrintsOnlyGroupDetails()
    {
        const bool printGroupTitles = true;
        const bool printRules = false;
        const string badIndicator = "z";
        Debug.Assert(!SomeName(0).Contains(badIndicator));
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);
        List<PhonologicalRuleGroup> groups = new()
        {
            new PhonologicalRuleGroup(SomeName(1), SomeName(2), [
                new(badIndicator, [], [], [], badIndicator),
                new(badIndicator, [], [], [], badIndicator),
                new(badIndicator, [], [], [], badIndicator),
            ]),
            new PhonologicalRuleGroup(SomeName(3), SomeName(4), [
                new(badIndicator, [], [], [], badIndicator),
            ]),
            new PhonologicalRuleGroup(SomeName(5), SomeName(6), [
                new(badIndicator, [], [], [], badIndicator),
                new(badIndicator, [], [], [], badIndicator),
            ]),
        };

        printer.PrintRuleGroups(groups, printGroupTitles, printRules);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        await Assert.That(output).DoesNotContain(badIndicator);
        foreach (int i in Enumerable.Range(1, 6))
        {
            Console.WriteLine("case: " + i);
            await Assert.That(output).Contains(SomeName(i));
        }
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    public async Task PrintRules_SampleCollectionPrintGroupsAndRules_PrintsEverything()
    {
        const bool printGroupTitles = true;
        const bool printRules = false;
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);
        List<PhonologicalRuleGroup> groups = new()
        {
            new PhonologicalRuleGroup(SomeName(1), SomeName(2), [
                new(SomeName(3), [], [], []),
                new(SomeName(4), [], [], []),
                new(SomeName(5), [], [], []),
            ]),
            new PhonologicalRuleGroup(SomeName(6), SomeName(7), [
                new(SomeName(8), [], [], []),
            ]),
            new PhonologicalRuleGroup(SomeName(9), SomeName(10), [
                new(SomeName(11), [], [], []),
                new(SomeName(12), [], [], []),
            ]),
        };

        printer.PrintRuleGroups(groups, printGroupTitles, printRules);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        foreach (int i in Enumerable.Range(1, 12))
        {
            Console.WriteLine("case: " + i);
            await Assert.That(output).Contains(SomeName(i));
        }
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.PrintCharactersEmptyCollectionCases))]
    public async Task PrintCharacters_EmptyCollection_PrintsHeader(List<PhonologicalRuleGroup> groups)
    {
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);

        printer.PrintCharacters(groups);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        await Assert.That(output.Count('\n'.Equals)).IsEqualTo(1);
        await Assert.That(output).EndsWith("\n");
        await Assert.That(output).DoesNotContain(DataSource.BadIndicator);
    }

    [Test]
    public async Task PrintCharacters_1SampleCharacterPerRule_PrintsAllCharacters()
    {
        const string badIndicator = "z";
        Debug.Assert(!SomeName(0).Contains(badIndicator));
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);
        List<PhonologicalRuleGroup> groups = new()
        {
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(badIndicator, [new(SomeName(1))], [], []),
                new(badIndicator, [new(SomeName(2), [SomeName(3)])], [], []),
            ]),
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(badIndicator, [], [new(SomeName(4))], []),
                new(badIndicator, [], [new(SomeName(5), [SomeName(6)])], []),
            ]),
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(badIndicator, [], [], [new(SomeName(7), [SomeName(8)])]),
            ]),
        };

        printer.PrintCharacters(groups);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        foreach (int i in Enumerable.Range(1, 7))
        {
            Console.WriteLine("case: " + i);
            await Assert.That(output).Contains(SomeName(i));
        }
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    public async Task PrintCharacters_ManySampleCharacterPerRule_PrintsAllCharacters()
    {
        const string badIndicator = "z";
        Debug.Assert(!SomeName(0).Contains(badIndicator));
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);
        List<PhonologicalRuleGroup> groups = new()
        {
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(badIndicator,
                    [new(SomeName(1)), new(SomeName(2), [SomeName(3)])],
                    [new(SomeName(3))],
                    [new(SomeName(4), [SomeName(5)]), new(SomeName(6))]
                ),
                new(badIndicator,
                    [],
                    [],
                    [new(SomeName(7), [SomeName(8)]), new(SomeName(9))]
                ),
            ]),
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(badIndicator,
                    [],
                    [new(SomeName(10), [SomeName(11)])],
                    []
                ),
            ]),
        };

        printer.PrintCharacters(groups);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        foreach (int i in Enumerable.Range(1, 11))
        {
            Console.WriteLine("case: " + i);
            await Assert.That(output).Contains(SomeName(i));
        }
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    public async Task PrintCharacters_DuplicateCharacters_PrintsEachOnce()
    {
        const string badIndicator = "z";
        Debug.Assert(!SomeName(0).Contains(badIndicator));
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);
        List<PhonologicalRuleGroup> groups = new()
        {
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(badIndicator,
                    [new(SomeName(1)), new(SomeName(1))],
                    [new(SomeName(1))],
                    [new(SomeName(1))]
                ),
                new (badIndicator, [new(SomeName(1))], [], []),
            ]),
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new (badIndicator, [new(SomeName(1))], [], []),
            ]),
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new (badIndicator, [new(SomeName(1), [SomeName(2)])], [], []),
                new (badIndicator, [new(SomeName(1), [SomeName(3)])], [], []),
                new (badIndicator, [new(SomeName(1), [SomeName(2), SomeName(3)])], [], []),
            ]),
        };

        printer.PrintCharacters(groups);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        foreach (int i in Enumerable.Range(1, 3))
        {
            Console.WriteLine("case: " + i);
            await Assert.That(output).Contains(SomeName(i));
        }
        await Assert.That(SubstringCount(output, SomeName(1))).IsEqualTo(3);
        await Assert.That(SubstringCount(output, SomeName(2))).IsEqualTo(2);
        await Assert.That(SubstringCount(output, SomeName(3))).IsEqualTo(3);
        await Assert.That(output).EndsWith("\n");

        int SubstringCount(string str, string sub)
        {
            int count = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str.AsSpan()[i..].StartsWith(sub))
                {
                    count++;
                }
            }

            return count;
        }
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.PrintDiacriticsEmptyCollectionCases))]
    public async Task PrintDiacritics_EmptyCollection_PrintsOnlyHeader(List<PhonologicalRuleGroup> groups)
    {
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);

        printer.PrintCharacters(groups);
        string output = console.OutText()!;


        await Assert.That(output).IsNotNullOrWhiteSpace();
        await Assert.That(output).DoesNotContain(DataSource.BadIndicator);
        await Assert.That(output.Count('\n'.Equals)).IsEqualTo(1);
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    public async Task PrintDiacritics_NoDiacritics_PrintsOnlyHeader()
    {
        const string badIndicatorScope = "zz-1";
        const string badIndicatorChar = "zz-2";
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);
        List<PhonologicalRuleGroup> groups =
        [
            new PhonologicalRuleGroup(badIndicatorScope, badIndicatorScope, [
                new PhonologicalRule(badIndicatorScope, [new(badIndicatorChar)], [], [])
            ]),
            new PhonologicalRuleGroup(badIndicatorScope, badIndicatorScope, [
                new PhonologicalRule(badIndicatorScope, [], [new(badIndicatorChar)], [])
            ]),
            new PhonologicalRuleGroup(badIndicatorScope, badIndicatorScope, [
                new PhonologicalRule(badIndicatorScope, [], [], [new(badIndicatorChar)])
            ]),
            new PhonologicalRuleGroup(badIndicatorScope, badIndicatorScope, [
                new PhonologicalRule(badIndicatorScope, [new(badIndicatorChar)], [], []),
                new PhonologicalRule(badIndicatorScope, [], [new(badIndicatorChar)], []),
                new PhonologicalRule(badIndicatorScope, [], [], [new(badIndicatorChar)]),
            ]),
            new PhonologicalRuleGroup(badIndicatorScope, badIndicatorScope, [
                new PhonologicalRule(badIndicatorScope,
                    [new(badIndicatorChar)],
                    [new (badIndicatorChar)],
                    [new (badIndicatorChar)]),
            ]),
            new PhonologicalRuleGroup(badIndicatorScope, badIndicatorScope, [
                new PhonologicalRule(badIndicatorScope,
                    [new(badIndicatorChar), new(badIndicatorChar)],
                    [new (badIndicatorChar), new(badIndicatorChar)],
                    [new (badIndicatorChar), new(badIndicatorChar)]),
            ]),
        ];

        printer.PrintCharacters(groups);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        await Assert.That(output).DoesNotContain(badIndicatorScope);
        await Assert.That(output).DoesNotContain(badIndicatorChar);
        await Assert.That(output.Count('\n'.Equals)).IsEqualTo(1);
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    public async Task PrintDiacritics_SampleSource_PrintsAllDiacritics()
    {
        const string badIndicator = "zz";
        Debug.Assert(!SomeName(0).Contains(badIndicator));
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);
        List<PhonologicalRuleGroup> groups = new()
        {
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(badIndicator, [new(badIndicator)], [], []),
                new(badIndicator, [new(badIndicator, [SomeName(1)])], [], []),
                new(badIndicator, [new(badIndicator, [SomeName(2), SomeName(3)])], [], []),
            ]),
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(badIndicator, [], [new(badIndicator)], [new (badIndicator)]),
                new(badIndicator, [], [new (badIndicator,[SomeName(4)])], [new (badIndicator,[SomeName(5), SomeName(6)])]),
            ]),
        };

        printer.PrintCharacters(groups);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        foreach (int i in Enumerable.Range(1, 6))
        {
            Console.WriteLine("case: " + i);
            await Assert.That(output).Contains(SomeName(i));
        }
        await Assert.That(output).DoesNotContain(badIndicator);
        await Assert.That(output).EndsWith("\n");
    }

    [Test]
    public async Task PrintDiacritics_DuplicateDiacritics_PrintsNoDuplicates()
    {
        const string badIndicator = "zz";
        Debug.Assert(!SomeName(0).Contains(badIndicator));
        TestConsole console = new(false);
        RuleGroupPrinter printer = new(console);
        List<PhonologicalRuleGroup> groups = new()
        {
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(badIndicator, [new(badIndicator, [SomeName(1)])], [], []),
                new(badIndicator, [new(badIndicator)], [], []),
                new(badIndicator, [new(badIndicator, [SomeName(1)])], [], []),
            ]),
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new(badIndicator, [new(badIndicator, [SomeName(1)])], [], []),

                new(badIndicator, [new(badIndicator, [SomeName(2)])], [], []),
                new(badIndicator, [], [new(badIndicator, [SomeName(2)])], []),
                new(badIndicator, [], [], [new(badIndicator, [SomeName(2)])]),

                new(badIndicator,
                    [new(badIndicator, [SomeName(3)])],
                    [new(badIndicator, [SomeName(3)])],
                    [new(badIndicator, [SomeName(3)])]
                ),
            ]),
            new PhonologicalRuleGroup(badIndicator, badIndicator, [
                new (badIndicator, [new(badIndicator, [SomeName(5)])], [], []),
                new (badIndicator, [new(badIndicator, [SomeName(4), SomeName(5)])], [], []),
            ]),
        };

        printer.PrintCharacters(groups);
        string output = console.OutText()!;

        await Assert.That(output).IsNotNullOrWhiteSpace();
        foreach (int i in Enumerable.Range(1, 5))
        {
            Console.WriteLine("case: " + i);
            await Assert.That(output).Contains(SomeName(i));
            await Assert.That(output.IndexOf(SomeName(i), StringComparison.Ordinal))
                .IsEqualTo(output.LastIndexOf(SomeName(i), StringComparison.Ordinal));
        }
        await Assert.That(output).DoesNotContain(badIndicator);
        await Assert.That(output).EndsWith("\n");
    }
}
