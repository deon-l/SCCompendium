using System.Text;
using SCCompendium.Domain.Exceptions;
using SCCompendium.Infrastructure.Parser.LatexParser;

namespace SCCompendium.Tests.Infrastructure.Parser;

public class LatexParserTests
{
    [Test]
    public async Task ParseLatexSegment_EscapedChars_GetCharsWithoutBackslash()
    {
        const string input = @"\#\$\&\%\{\}\ ";
        const string expectedOutput = "#$&%{} ";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatexSegment_WhitespaceChars_GetOnlySpacesTabs()
    {
        string input = new String(' ', 2) + "\n\n\t\r" + ' ';
        const string expectedOutput = "  \t ";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatexSegment_BasicNoParamCommands_GetParsedOutput()
    {
        const string input = @"\change\textrightarrow";
        const string expectedOutput = "→→";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatexSegment_GroupCommandBF_ResetsOutsideGroup()
    {
        const string input = @"{\bf abc}def";
        const string expectedOutput = "𝐚𝐛𝐜def";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_CommandTextBf_ExpectedOutput()
    {
        const string input = @"\textbf{abc}def";
        const string expectedOutput = "𝐚𝐛𝐜def";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\textit{aBcz}def", "𝑎𝐵𝑐𝑧def")]
    [Arguments(@"{\it aBcz}def", "𝑎𝐵𝑐𝑧def")]
    public async Task ParseLatex_ItAndVariants_ExpectedOutput(string input, string expectedOutput)
    {
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\textbf{\textit{ABCxyz}}", "𝑨𝑩𝑪𝒙𝒚𝒛")]
    [Arguments(@"\textit{\textbf{ABCxyz}}", "𝑨𝑩𝑪𝒙𝒚𝒛")]
    [Arguments(@"{\it{\bf ABCxyz}}", "𝑨𝑩𝑪𝒙𝒚𝒛")]
    [Arguments(@"{\bf{\it ABCxyz}}", "𝑨𝑩𝑪𝒙𝒚𝒛")]
    public async Task parseLatex_CombiningItBf_BoldItalicOutput(string input, string expectedOutput)
    {
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatexSegment_SourceLigatures_GetLigatures()
    {
        const string input = @"``''-- ---";
        const string expectedOutput = "“”– —";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    public class DataSource
    {
        public const string SimpleTipaInput  = ":;\"0123456789@ABCDEFGHIJKLMNOPQRSTUVWXYZ|";
        public const string SimpleTipaOutput = "ːˑˈʉɨʌɜɥɐɒɤɵɘəɑβɕðɛɸɣɦɪʝʁʎɱŋɔʔʕɾʃθʊʋɯχʏʒ|";
        public static IEnumerable<(char, char)> TipaSourceToOutput()
        {
            Debug.Assert(SimpleTipaInput.Length == SimpleTipaOutput.Length, $"{SimpleTipaInput.Length} != {SimpleTipaOutput.Length}");
            for (int i = 0; i < SimpleTipaOutput.Length; i++)
            {
                yield return (SimpleTipaInput[i], SimpleTipaOutput[i]);
            }
        }
    }

    [Test]
    public async Task ParseLatexSegment_AllSimpleTipaInput_ExpectedSimpleTipaOutput()
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexSegment(@$"\ipa{{{DataSource.SimpleTipaInput}}}", sb);

        await Assert.That(sb.ToString()).IsEqualTo(DataSource.SimpleTipaOutput);
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.TipaSourceToOutput))]
    public async Task ParseLatex_Tipa1ReplacedChar_GetReplacementChar(char inputSegment, char outputSegment)
    {
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(@$"\ipa{{{inputSegment}}}");

        await Assert.That(result).IsEqualTo(outputSegment.ToString());
    }

    [Test]
    public async Task ParseLatex_TipaLigaturesSource_GetLigatures()
    {
        const string input = "\"\" ||";
        const string expectedOutput = "ˌ ‖";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(@$"\ipa{{{input}}}");

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\*f \*k \*r \*t \*w", "ⅎ ʞ ɹ ʇ ʍ")]
    // Note: "\*f" translation doesn't follow TIPA specs,
    //  but couldn't find similar character (that isn't also result of "\*j")
    [Arguments(@"\*j \*n \*h \*l \*z", "ɟ ɲ ħ ɬ ɮ")]
    [Arguments(@"\*A \*B \*C \*1 \*2 \*3", "A B C 1 2 3")]
    [Arguments(@"\*; \*: \*@ \*\# \*\$ \*\& \*\% \*\{ \*\}", "; : @ # $ & % { }")]
    [Arguments(@"\*{123}", "123")]
    public async Task ParseLatex_TipaAsteriskMacros_ExpectedOutput(string input, string expectedOutput)
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexSegment(@$"\ipa{{{input}}}", sb);

        await Assert.That(sb.ToString()).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_TipaCommandSuper_ReturnsExpectedOutput()
    {
        const string input = @"\ipa{\super{hlmnjwxyHMNPQW}}";
        const string expectedOutput = "ʰˡᵐⁿʲʷˣʸʱᶬᵑˀˤᵚ";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_EmptyMathInput_NoModification()
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexSegment("$$", sb);

        await Assert.That(sb).IsEmpty();
    }

    [Test]
    [Arguments("$_0$", "₀")]
    [Arguments("$^1$", "¹")]
    [Arguments(@"$\Omega$", "Ω")]
    [Arguments(@"$\langle$", "⟨")]
    [Arguments(@"$\rangle$", "⟩")]
    public async Task ParseLatex_MathSingleCommands_ExpectedOutputs(string inputSegment, string expectedOutput)
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexSegment(inputSegment, sb);
        string actualOutput = sb.ToString();

        await Assert.That(actualOutput).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\bbbbb")]
    [Arguments(@"\")]
    public async Task ParseLatex_InvalidInput_ThrowsException(string invalidInput)
    {
        LatexParser parser = new();

        void ErrorAction() => parser.ParseLatexSegment(invalidInput, new());

        await Assert.That(ErrorAction).ThrowsExactly<LatexParsingException>().WithMessageContaining(invalidInput);
    }

    [Test]
    [Arguments(@"\tab")]
    [Arguments(@"\ipa{\tab}")]
    public async Task ParseLatex_TabInput_ProducesSingleTab(string input)
    {
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo("\t");
    }

    [Test]
    [Arguments(@"\hspace{10pt}")]
    [Arguments(@"\hspace  { 5.5   pt }")]
    [Arguments(@"\hspace 10pt")]
    [Arguments(@"\hspace  10  pt")]
    [Arguments(@"\hspace5.5pt")]
    [Arguments(@"\hspace10 em")]
    public async Task ParseLatex_HSpaceInputs_ProducesSomeWhitespace(string input)
    {
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsNotEmpty();
        await Assert.That(result).IsNullOrWhiteSpace();
    }

    [Test]
    public async Task ParseLatex_DifferentHSpaceSizes_ProducesDifferentWhitespaceCount()
    {
        const string smallerInput = @"\hspace{10pt}";
        const string largerInput = @"\hspace 1000pt";
        LatexParser parser = new();

        int count1 = parser.ParseLatexSegment(smallerInput).Count(' '.Equals);
        int count2 = parser.ParseLatexSegment(largerInput).Count(' '.Equals);

        await Assert.That(count1).IsLessThan(count2);
    }

    [Test]
    public async Task ParseLatex_DifferentUnits_ProduceDifferentWhitespaceCounts()
    {
        const string smallerInput = @"\hspace100mm";
        const string largerInput = @"\hspace{100cm}";
        LatexParser parser = new();

        int count1 = parser.ParseLatexSegment(smallerInput).Count(' '.Equals);
        int count2 = parser.ParseLatexSegment(largerInput).Count(' '.Equals);

        await Assert.That(count1).IsLessThan(count2);
    }

    [Test]
    [Arguments(@"\hspace{pt}")]
    [Arguments(@"\hspace 10")]
    [Arguments(@"\hspace{{10 pt}}")]
    [Arguments(@"\hspace{10 pt")]
    [Arguments(@"\hspace 10 pt}")]
    [Arguments(@"\hspace{ 10 pt}}")]
    [Arguments(@"\hspace10 ps")]
    public async Task ParseLatex_BadHSpaceCommands_ProducesError(string invalidInput)
    {
        LatexParser parser = new();

        void ErrorAction() => parser.ParseLatexSegment(invalidInput, new());

        await Assert.That(ErrorAction).ThrowsExactly<LatexParsingException>().WithMessageContaining(invalidInput);
    }

    [Test]
    public async Task ParseLatex_RaiseboxCommand_ExpectedResult()
    {
        const string input = @"\raisebox{-0.6ex}{atE}";
        string expectedOutput = @"atE";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_TtCommand_ExpectedOutput()
    {
        const string input = @"{\tt aBc123}AbC";
        const string expectedOutput = @"𝚊𝙱𝚌𝟷𝟸𝟹AbC";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_TextPolHookCommand_ExpectedOutput()
    {
        const string input = @"\textpolhook{e}";
        string expectedOutput = @"ę".Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_NormalTildeCommand_ExpectedOutput()
    {
        const string input = @"\~{a} \~ea";
        string expectedOutput = @"ã ẽa".Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\ipa{\~aa}", "ãa")]
    [Arguments(@"\ipa{\~.ee}", "ė̃e")]
    [Arguments(@"\ipa{\~*ee}", "ḛe")]
    [Arguments(@"\ipa{\~* de}", "d̰e")] // subscript tilde seems to be put on the next char in some fonts, rather than prior char.
    [Arguments(@"\ipa{\~.{cc}d}","ċ̃ċ̃d")]
    [Arguments(@"\ipa{\~{ff}g}", "f̃f̃g")]
    public async Task ParseLatex_AllIpaTildeVariations_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\'aa", "áa")]
    [Arguments(@"\ipa{\'{bb}c}", "b́b́c")]
    public async Task ParseLatex_ApostropheCommand_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\c aa", "a̧a")]
    [Arguments(@"\ipa{\c{bb}c}", "b̧b̧c")]
    public async Task ParseLatex_cCommand_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\i{}i", "ıi")]
    [Arguments(@"\~{\i}", "ı̃")]
    // [Arguments(@"\~{\i}", "ĩ")] // These have result as normal i with tilde. They are not equivalent.
    // [Arguments(@"\~{\i}", "ĩ")]
    public async Task ParseLatex_iCommand_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\d{S}", "Ṣ")]
    [Arguments(@"\d{A}", "Ạ")]
    public async Task ParseLatex_dCommand_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\O{}i", "Øi")]
    [Arguments(@"\ipa{\O}", "Ø")]
    public async Task ParseLatex_OCommand_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\^ a", "â")]
    [Arguments(@"\textsubcircum{a}", "a̭")]
    [Arguments(@"\textcircumdot z","ż̂")]
    public async Task ParseLatex_CaretCommandAndVariations_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\ipa{\^ ab}", "âb")]
    [Arguments(@"\ipa{\^*{a}b}", "a̭b")]
    [Arguments(@"\ipa{\^. zz}","ż̂z")]
    public async Task ParseLatex_IpaCaretCommandAndVariations_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_textellipsisCommand_ExpectedOutput()
    {
        const string input = @"a\textellipsis{}b";
        const string expectedOutput = "a…b";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_textbardotlessjCommand_ExpectedOutput()
    {
        const string input = @"a\textbardotlessj{}b";
        const string expectedOutput = "aɟb";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"a\textcrh{}b", "aħb")]
    [Arguments(@"a\textbeltl{}b", "aɬb")]
    [Arguments(@"a\textltailn{}b", "aɲb")]
    [Arguments(@"a\textless{}b", "a<b")]
    [Arguments(@"a\textgreater{}b", "a>b")]
    [Arguments(@"a\ae{}D", "aæD")]
    [Arguments(@"A\o{}z", "Aøz")]
    [Arguments(@"A\textturnmrleg{}z", "Aɰz")]
    [Arguments(@"A\textturnw{}z", "Aʍz")]
    [Arguments(@"A\j{}z", "Aȷz")]
    [Arguments(@"A\textasciitilde{}z", "A~z")]
    public async Task ParseLatex_ReplacementCommands_ExpectedOutput(string input, string expectedOutput)
    {
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"a\textsubarch xe", "ax̯e")]
    [Arguments(@"a\. xe", "aẋe")]
    public async Task ParseLatex_SimpleDiacriticApplierCommand_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_tIpaCommand_ExpectedOutput()
    {
        const string input = @"\ipa{\t{gb}a}";
        const string expectedOutput = "g͡ba";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_QuoteCommand_ExpectedOutput()
    {
        const string input = "\\\"{adt}" ;
        string expectedOutput = "äd̈ẗ".Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\ipa{\; B}", "ʙ")]
    [Arguments(@"\ipa{\;{AZ}Z}", "ᴀᴢʒ")]
    public async Task ParseLatex_SymSemicolonCommand_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\ipa{\: d\:l\:nt}", "ɖɭɳt")]
    [Arguments(@"\ipa{\:{tszr}d}", "ʈʂʐɽd")]
    public async Task ParseLatex_SymColonCommand_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\ipa{\! b\!dd}", "ɓɗd")]
    [Arguments(@"\ipa{\!{ogGj}d}", "ʘɠʛʄd")]
    public async Task ParseLatex_SymExclamationPointCommand_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\textltilde", "ɫ")]
    [Arguments(@"\textroundcap{a}", "ȃ")]
    [Arguments(@"\textsubbridge{a}", "a̪")]
    [Arguments(@"\textinvsubbridge{a}", "a̺")]
    [Arguments(@"\textsubrhalfring{a}", "a̹")]
    [Arguments(@"\textsublhalfring{a}", "a̜")]
    [Arguments(@"\textsubw{a}", "a̫")]
    [Arguments(@"\textseagull{a}", "a̼")]
    [Arguments(@"\textovercross{a}", "a̽")]
    [Arguments(@"\textsubplus{a}", "a̟")]
    [Arguments(@"\textraising{a}", "a̝")]
    [Arguments(@"\textlowering{a}", "a̞")]
    [Arguments(@"\textadvancing{a}", "a̘")]
    [Arguments(@"\textretracting{a}", "a̘")]
    [Arguments(@"\textsuperimposedtilde{a}", "a̴")]
    public async Task ParseLatex_VertCommandFamily_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\ipa{\|ca}", "ȃ")]
    [Arguments(@"\ipa{\|[a}", "a̪")]
    [Arguments(@"\ipa{\|]a}", "a̺")]
    [Arguments(@"\ipa{\|(a}", "a̹")]
    [Arguments(@"\ipa{\|)a}", "a̜")]
    [Arguments(@"\ipa{\|wa}", "a̫")]
    [Arguments(@"\ipa{\|ma}", "a̼")]
    [Arguments(@"\ipa{\|xa}", "a̽")]
    [Arguments(@"\ipa{\|+a}", "a̟")]
    [Arguments(@"\ipa{\|'a}", "a̝")]
    [Arguments(@"\ipa{\|`a}", "a̞")]
    [Arguments(@"\ipa{\|<a}", "a̘")]
    [Arguments(@"\ipa{\|>a}", "a̘")]
    [Arguments(@"\ipa{\|~a}", "a̴")]
    public async Task ParseLatex_TipaVertCommand_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\=e \=*e", "ē *̄e")]
    [Arguments(@"\textsubbar{ez}", "e̠z̠")]
    [Arguments(@"\ipa{\=e \=*e}", "ē e̠")]
    public async Task ParseLatex_SymEqualsCommandFamily_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\v a \v*z", "ǎ *̌z")]
    [Arguments(@"\textacutewedge{sa}d", "š́ǎ́d")]
    [Arguments(@"\textsubwedge vb", "v̬b")]
    [Arguments(@"\textipa{\v a\v*{b}\v' c}", "ǎb̬č́")]
    public async Task ParseLatex_VCommandFamily_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\r{a}\r*z", "å*̊z")]
    [Arguments(@"\textsubring{d}", "d̥")]
    [Arguments(@"\textringmacron g", "ḡ̊")]
    [Arguments(@"\textipa{\r* a \r= b \r{c}}", "ḁ b̄̊ c̊")]
    public async Task ParseLatex_rCommandFamily_ExpectedOutput(string input, string expectedOutput)
    {
        expectedOutput = expectedOutput.Normalize();
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input).Normalize();

        await Assert.That(result).IsEqualTo(expectedOutput);
    }
}
