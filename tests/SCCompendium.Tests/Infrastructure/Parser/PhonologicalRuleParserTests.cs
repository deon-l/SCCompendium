using SCCompendium.Application.Parser;
using SCCompendium.Domain;
using SCCompendium.Infrastructure.Parser;

namespace SCCompendium.Tests.Infrastructure.Parser;

public class PhonologicalRuleParserTests
{
    [Test]
    [Arguments("")]
    [Arguments("                           ")]
    [Arguments("--- Stress changes:")]
    [Arguments("\\tab{\\it some tab thing}")]
    public async Task TryParseRule_InvalidRule_ReturnsFalseDefault(string invalidRule)
    {
        var latexParserMock = ILatexParser.Mock();

        PhonologicalRuleParser parser = new(latexParserMock);

        var result = parser.TryParseRule(invalidRule, out PhonologicalRule rule);

        await Assert.That(result).IsFalse();
        await Assert.That(rule).IsEqualTo(default(PhonologicalRule));
        latexParserMock.ParseLatexSegment(RefStructArg<ReadOnlySpan<char>>.Any, Any()).WasCalled(Times.Never);
    }

    [Test]
    [Arguments("a")]
    [Arguments("a̟")]
    [Arguments("a[+long]")]
    public async Task TryParseRule_RuleWithDuplicates_ListsNoDuplicateChars(string replacement)
    {
        const string inputRule = @"\ipa{a a} \change\ \ipa{a a} / \ipa{a}_\ipa{a} ! \ipa{aa}_";
        var latexParserMock= ILatexParser.Mock();
        latexParserMock.ParseLatexSegment(RefStructArg<ReadOnlySpan<char>>.Any, Any()).Callback(sb => sb.Append(replacement + replacement));
        PhonologicalRuleParser parser = new(latexParserMock);

        bool success = parser.TryParseRule(inputRule, out PhonologicalRule resultRule);

        await Assert.That(success).IsTrue();
        await Assert.That(resultRule.InputCharacters.Length).IsEqualTo(1);
        await Assert.That(resultRule.OutputCharacters.Length).IsEqualTo(1);
        await Assert.That(resultRule.ContextCharacters.Length).IsEqualTo(1);
    }

    [Test]
    [Arguments("ː", "˞")]
    [Arguments("ː", "[+high]")]
    [Arguments("[+back]", "[+high]")]
    public async Task TryParseRule_RuleWithDuplicateAlternatingDiacritics_ListNoDuplicates(string dia1, string dia2)
    {
        const string inputRule = @"\ipa{a a}\ \change\ \ipa{bb} / \ipa{cc}_\ipa{c}";
        var latexParserMock = MockableILatexParser.Mock();
        int count = 0;
        latexParserMock.ParseLatexSegment(Any(), Any()).Callback((str, sb) =>
        {
            count++;
            if (str.Contains("a a"))
                sb.Append($"a{dia1}{dia2} a{dia2}{dia1}");
            else if (str.Contains("bb"))
                sb.Append($"a{dia1}{dia2}a{dia2}{dia1}");
            else if (str.Contains('_'))
                sb.Append($"a{dia1}{dia2}a{dia2}{dia1}_{dia2}{dia1}");
            else
                sb.Append($"a{dia1}{dia2}a{dia1}{dia2}a{dia1}{dia2}");
        });
        PhonologicalRuleParser parser = new(latexParserMock.Object);

        bool success = parser.TryParseRule(inputRule, out PhonologicalRule resultRule);

        await Assert.That(success).IsTrue();
        await Assert.That(resultRule.InputCharacters.Length).IsEqualTo(1);
        await Assert.That(resultRule.OutputCharacters.Length).IsEqualTo(1);
        await Assert.That(resultRule.ContextCharacters.Length).IsEqualTo(1);
    }

    [Test]
    [Arguments(@"\ipa{a} \change\ \ipa{a} ``quoted note here \ipa{z}''")]
    [Arguments(@"\ipa{a} \change\ \ipa{a} ``quoted note here \ipa{z}""")]
    [Arguments(@"\ipa{a} \change\ \ipa{a} / \ipa{a}_ ``when near \ipa{z}""", true)]
    [Arguments(@"\ipa{a} \change\ \ipa{a} / \ipa{a}_ ! ``near \ipa{z}""", true)]
    public async Task TryParseRule_RuleWithQuoteNote_NoExtractedCharsFromNote(string input, bool checkContext = false)
    {
        Debug.Assert(input.Contains('z'), "use char 'z' to indicate value that shouldn't be analyzed as ipa char.");
        var latexParserMock = MockableILatexParser.Mock();
        latexParserMock.ParseLatexSegment(Any(), Any()).Callback((str, sb) =>
        {
            if (str.Contains('z'))
                sb.Append('z');
            else if (str.Contains('_'))
                sb.Append("a_");
            else
                sb.Append('a');
        });

        PhonologicalRuleParser parser = new(latexParserMock.Object);

        var success = parser.TryParseRule(input, out PhonologicalRule resultRule);

        await Assert.That(success).IsTrue();
        await Assert.That(resultRule.InputCharacters.Length).IsEqualTo(1);
        await Assert.That(resultRule.OutputCharacters.Length).IsEqualTo(1);
        if (checkContext)
        {
            await Assert.That(resultRule.InputCharacters.Length).IsEqualTo(1);
        }
        await Assert.That(resultRule.InputCharacters).DoesNotContain(ipaChar => ipaChar.Character == "z");
        await Assert.That(resultRule.OutputCharacters).DoesNotContain(ipaChar => ipaChar.Character == "z");
        await Assert.That(resultRule.ContextCharacters).DoesNotContain(ipaChar => ipaChar.Character == "z");
    }

    public partial class DataSource
    {
        private const string IpaDoubleCharSource = "pɸ bβ pf bv ts dz tʃ ʈʂ ɖʐ tɕ dʑ cç ɟʝ kx ɡɣ qχ ɢʁ ʡʜ ʡʢ ʔh tɬ dɮ ";
        public IEnumerable<string> Affricates()
        {
            for (int i = 0; i < IpaDoubleCharSource.Length; i += 3)
            {
                yield return IpaDoubleCharSource.Substring(i, 2);
            }
        }
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.Affricates))]
    public async Task TryParseRule_RuleWithAffricates_ExtractsOnlyAffricates(string affricate)
    {
        const string inputRule = @"\ipa{z} \change \ipa{z}";
        var latexParserMock = MockableILatexParser.Mock();
        latexParserMock.ParseLatexSegment(Any(), Any()).Callback((_, sb) => sb.Append(affricate));
        PhonologicalRuleParser parser = new(latexParserMock.Object);
        IpaCharacter expectedIpaCharacter = new(affricate, []);

        var success = parser.TryParseRule(inputRule, out PhonologicalRule resultRule);

        await Assert.That(success).IsTrue();
        await Assert.That(resultRule.InputCharacters.Length).IsEqualTo(1);
        await Assert.That(resultRule.InputCharacters).Contains(expectedIpaCharacter);
        await Assert.That(resultRule.OutputCharacters.Length).IsEqualTo(1);
        await Assert.That(resultRule.OutputCharacters).Contains(expectedIpaCharacter);
        await Assert.That(resultRule.ContextCharacters).IsEmpty();
    }

    [Test]
    [Arguments(@"\ipa{a} \change \ipa{a} (zebra)")]
    [Arguments(@"\ipa{a} \change \ipa{a} / \ipa{a}_ (zebra)")]
    [Arguments(@"\ipa{a} \change \ipa{a} / \ipa{a}_ (z sentence here with)")]
    [Arguments(@"\ipa{a} \change \ipa{a} / \ipa{a}_ (? inconsistent z)")]
    [Arguments(@"\ipa{a} \change \ipa{a} / \ipa{a}_ (has z ``quotes'')")]
    public async Task TryParseRule_RuleWithParenthesisNote_NoExtractedCharsFromNote(string input)
    {
        Debug.Assert(input.Contains('z'), "use char 'z' to indicate value that shouldn't be analyzed as ipa char.");
        var latexParserMock = MockableILatexParser.Mock();
        latexParserMock.ParseLatexSegment(Any(), Any()).Callback((str, sb) =>
        {
            if (str.Contains('z'))
                sb.Append('z');
            else if (str.Contains('_'))
                sb.Append("a_");
            else
                sb.Append('a');
        });
        PhonologicalRuleParser parser = new(latexParserMock.Object);

        var success = parser.TryParseRule(input, out PhonologicalRule resultRule);

        await Assert.That(success).IsTrue();
        await Assert.That(resultRule.InputCharacters).DoesNotContain(ipaChar => ipaChar.Character.Contains('z'));
        await Assert.That(resultRule.OutputCharacters).DoesNotContain(ipaChar => ipaChar.Character.Contains('z'));
        await Assert.That(resultRule.ContextCharacters).DoesNotContain(ipaChar => ipaChar.Character.Contains('z'));
    }

    [Test]
    [Arguments(@"\ipa{a} \textrightarrow\ \ipa{a} / in some unstressed syllable", 'n')]
    [Arguments(@"\ipa{a} \textrightarrow\ \ipa{a} / in some unstressed syllable", 's')] // Just to check all words don't appear here
    [Arguments(@"\ipa{a} \change\ \ipa{a} / else", 's')]
    [Arguments(@"Loaned \ipa{a} \change\ \ipa{a}", 'd')]
    [Arguments(@"\ipa{a} \textrightarrow\ \ipa{a} / in the thing", 'n')]
    public async Task TryParseRule_RuleWithPlainNote_NoExtractedCharsFromNote(string input, char indicator)
    {
        Debug.Assert(input.Contains(indicator), "use 'indicator' to indicate what shouldn't be an ipa char");
        var latexParserMock = MockableILatexParser.Mock();
        latexParserMock.ParseLatexSegment(Any(), Any()).Callback((str, sb) =>
        {
            if (str.Contains(indicator))
                sb.Append(indicator);
            else if (str.Contains('_'))
                sb.Append("a_");
            else
                sb.Append('a');
        });
        PhonologicalRuleParser parser = new(latexParserMock.Object);

        var success = parser.TryParseRule(input, out PhonologicalRule resultRule);

        await Assert.That(success).IsTrue();
        await Assert.That(resultRule.InputCharacters).DoesNotContain(ipaChar => ipaChar.Character.Contains(indicator));
        await Assert.That(resultRule.OutputCharacters).DoesNotContain(ipaChar => ipaChar.Character.Contains(indicator));
        await Assert.That(resultRule.ContextCharacters).DoesNotContain(ipaChar => ipaChar.Character.Contains(indicator));
    }
}
