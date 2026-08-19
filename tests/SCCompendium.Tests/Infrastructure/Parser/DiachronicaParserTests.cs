using SCCompendium.Application.Parser;
using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Infrastructure.Parser;
using SCCompendium.Infrastructure.Parser.LatexParser;

namespace SCCompendium.Tests.Infrastructure.Parser;

public partial class DiachronicaParserTests
{
    private static readonly IpaCharacter _charA = new("a", []);
    private static readonly PhonologicalRule _defaultRule = new("a -> a", [_charA], [_charA], [_charA]);

    [Test]
    public async Task Parse_EmptyInput_ReturnsEmptyList()
    {
        var latexParser = MockableILatexParser.Mock();
        var ruleParser = IPhonologicalRuleParser.Mock();
        DiachronicaParser parser = new(latexParser.Object, ruleParser.Object);

        var listings = parser.Parse(new StringReader(String.Empty));

        await Assert.That(listings).IsEmpty();
        latexParser.ParseLatexSegment(Any(), Any()).WasNeverCalled();
        ruleParser.TryParseRule(Any()).WasNeverCalled();
    }

    [Test]
    public async Task Parse_JustSections_ReturnsEmptyList()
    {
        var latexParser = MockableILatexParser.Mock();
        var ruleParser = IPhonologicalRuleParser.Mock();
        DiachronicaParser parser = new(latexParser.Object, ruleParser.Object);
        const string input =
"""
\documentclass[11pt]{article}
\usepackage{asdfadsfa;j}
\usepackage[2]{:2222a:df]
%\usepackage{comment}
\title{title}
\begin{document}
\section{Prefix}

\ipa{a} \change\ \ipa{b} 
V\ipa{b} \textrightarrow\ V / \ipa{s}_
\ipa{a} \change\ \ipa{b} ``should not be parsed! zz"

\section{Proto-Asagaf to Fals Elang}\textit{me}, from here and now (2002), \textit{aaaaaaaaaaaaaaa}

\ipa{a} \change\ \ipa{b}  \\
V\ipa{b} \textrightarrow\ V / \ipa{s}_ \\
\ipa{a} \change\ \ipa{b} ``still should not be parsed! zz" 

""";
        var result = parser.Parse(new StringReader(input));

        await Assert.That(result).IsEmpty();
        latexParser.ParseLatexSegment(Any(), Any()).WasNeverCalled();
        ruleParser.TryParseRule(Any()).WasNeverCalled();
    }

    [Test]
    public async Task Parse_SkipsPreamble_ReturnsListOfOnly1Rule()
    {
        const string input =
"""
\documentclass[11pt]{article}
\usepackage{asdfadsfa;j}
\usepackage[2]{:2222a:df]
%\usepackage{comment}
\title{title}
\begin{document}
\section{Prefix}

\ipa{a} \change\ \ipa{b} 
V\ipa{b} \textrightarrow\ V / \ipa{s}_
\ipa{a} \change\ \ipa{b} ``should not be parsed!"

\section{Proto-Asagaf to Fals Elang}\textit{me}, from here and now (2002), \textit{aaaaaaaaaaaaaaa}

\ipa{a} \change\ \ipa{b}  \\
V\ipa{b} \textrightarrow\ V / \ipa{s}_ \\
\ipa{a} \change\ \ipa{b} ``still should not be parsed!" 

\subsection{1 to 2}credits

\ipa{a} \change\ \ipa{x}  \\
V\ipa{b} \textrightarrow\ V / \ipa{y}_ \\
""";
        var latexParser = MockableILatexParser.Mock();
        latexParser.ParseLatexSegment(Any(), Any()).Callback((str, sb) => sb.Append(">"+str));
        var ruleParser = IPhonologicalRuleParser.Mock();
        var arg1 = Is<string>(s => s?.Contains('x') ?? false);
        var arg2 = Is<string>(s => s?.Contains('y') ?? false);
        ruleParser.TryParseRule(arg1).Returns(true).SetsOutRule(_defaultRule);
        ruleParser.TryParseRule(arg2).Returns(true).SetsOutRule(_defaultRule);
        DiachronicaParser parser = new(latexParser.Object, ruleParser.Object);

        var result = parser.Parse(new StringReader(input));

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Rules).Count().IsEqualTo(2);
        await Assert.That(result[0].Title).IsEqualTo(">1 to 2");
        await Assert.That(result[0].Credit).IsEqualTo(">credits");
        await Assert.That(arg1.Values).Count().IsEqualTo(1);
        await Assert.That(arg2.Values).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Parse_MultipleSections_ParsesAllSections()
    {
        const string input =
"""
\section{s1}
\ipa{a} \change\ \ipa{a}
\subsection{s2}cr2
\ipa{a} \change\ \ipa{a}
\subsubsection{s3}cr3
\ipa{a} \change\ \ipa{a}
\section{s4}
\ipa{a} \change\ \ipa{a}
\subsection{s5}cr5
\ipa{a} \change\ \ipa{a}
\paragraph{s6}cr6
\ipa{a} \change\ \ipa{a}
\subparagraph{s7}cr7
\ipa{a} \change\ \ipa{a}

""";
        var latexParser = MockableILatexParser.Mock();
        latexParser.ParseLatexSegment(Any(), Any()).Callback((str, sb) => sb.Append(">"+str));
        var ruleParser = IPhonologicalRuleParser.Mock();
        var arg1 = Is<string>(s => s?.Contains("ipa{a}") ?? false);
        ruleParser.TryParseRule(arg1).Returns(true).SetsOutRule(_defaultRule);
        DiachronicaParser parser = new(latexParser.Object, ruleParser.Object);

        var result = parser.Parse(new StringReader(input));

        await Assert.That(result).Count().IsEqualTo(5);
        foreach (PhonologicalRuleGroup group in result)
        {
            await Assert.That(group.Rules).Count().IsEqualTo(1);
        }
        await Assert.That(result[0].Title).IsEqualTo(">s2");
        await Assert.That(result[1].Title).IsEqualTo(">s3");
        await Assert.That(result[2].Title).IsEqualTo(">s5");
        await Assert.That(result[3].Title).IsEqualTo(">s6");
        await Assert.That(result[4].Title).IsEqualTo(">s7");
        await Assert.That(result[0].Credit).StartsWith(">cr2");
        await Assert.That(result[1].Credit).StartsWith(">cr3");
        await Assert.That(result[2].Credit).StartsWith(">cr5");
        await Assert.That(result[3].Credit).StartsWith(">cr6");
        await Assert.That(result[4].Credit).StartsWith(">cr7");
        await Assert.That(arg1.Values).Count().IsEqualTo(5);
    }

    [Test]
    public async Task Parse_SectionWithNotaBene_TakesAsNote()
    {
        const string input =
"""
\section{s1}
\subsection{s2}
NB: note 1 (Don't think its used this way.)
\ipa{z} \change\ \ipa{z}
\subsection{s3}
\tab {\it NB: expected format}
\ipa{z} \change\ \ipa{z}
\subsection{s4}
\tab \textit{NB: this is used once}
\ipa{z} \change\ \ipa{z}
""";
        var latexParser = MockableILatexParser.Mock();
        latexParser.ParseLatexSegment(Any(), Any()).Callback((str, sb) => sb.Append(str));
        var ruleParser = IPhonologicalRuleParser.Mock();
        ruleParser.TryParseRule(str => str.Contains('z')).Returns(true).SetsOutRule(_defaultRule);
        DiachronicaParser parser = new(latexParser.Object, ruleParser.Object);

        var result = parser.Parse(new StringReader(input));

        await Assert.That(result).Count().IsEqualTo(3);
        foreach (PhonologicalRuleGroup group in result)
        {
            Console.WriteLine($"Checking [{group}]");
            await Assert.That(group.Rules).Count().IsEqualTo(1);
            await Assert.That(group.Note).Contains("NB:");
        }
    }

    [Test]
    public async Task Parse_HasNonNote_NotUsedAsNote()
    {
        const string input =
            """
            \section{s1}
            \subsection{s2}
            \ipa{z} \change\ \ipa{z}\\
            Some miscellaneous thing that isn't a note involving \ipa{a} and stuff.\\
            \ipa{z} \change\ \ipa{z}\\
            \ipa{z} \change\ \ipa{z}
            """;
        var latexParser = MockableILatexParser.Mock();
        latexParser.ParseLatexSegment(Any(), Any()).Callback((str, sb) => sb.Append(str));
        var ruleParser = IPhonologicalRuleParser.Mock();
        ruleParser.TryParseRule(str => str.Contains('z')).Returns(true).SetsOutRule(_defaultRule);
        DiachronicaParser parser = new(latexParser.Object, ruleParser.Object);

        var result = parser.Parse(new StringReader(input));

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Rules).Count().IsEqualTo(3);
        await Assert.That(result[0].Rules).DoesNotContain(rule => !String.IsNullOrWhiteSpace(rule.Note));
    }

    [Test]
    public async Task Parse_HasNoteWithExplicitApplication_DoesNotOverApply()
    {
        const string input =
            """
            \section{s1}
            \subsection{s2}
            \ipa{z} \change\ \ipa{z}\\
            Initials:\\
            ---\ipa{z} \change\ \ipa{z}\\
            ---\ipa{z} \change\ \ipa{z}\\
            \ipa{z} \change\ \ipa{z}
            """;
        var latexParser = MockableILatexParser.Mock();
        latexParser.ParseLatexSegment(Any(), Any()).Callback((str, sb) => sb.Append(str));
        var ruleParser = IPhonologicalRuleParser.Mock();
        // mock arg matching seem to work in reverse order.
        ruleParser.TryParseRule(str => str.Contains('z')).Returns(true).SetsOutRule(_defaultRule);
        ruleParser.TryParseRule(str => str.StartsWith("---")).Returns(true).SetsOutRule(_defaultRule);
        DiachronicaParser parser = new(latexParser.Object, ruleParser.Object);

        var result = parser.Parse(new StringReader(input));

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Rules).Count().IsEqualTo(4);
        await Assert.That(result[0].Rules[0].Note).IsEmpty();
        await Assert.That(result[0].Rules[1].Note).IsNotEmpty();
        await Assert.That(result[0].Rules[2].Note).IsNotEmpty();
        await Assert.That(result[0].Rules[3].Note).IsEmpty();
    }

    [Test]
    public async Task Parse_HasNoteWithTrickyExplicitApplication_DoesNotOverApplyOrUseWrongNote()
    {
        const string input =
            """
            \section{s1}
            \subsection{s2}
            \ipa{z} \change\ \ipa{z}\\
            Initials:\\
            ---not note but does end in w/ a colon (:
            ---\ipa{z} \change\ \ipa{z} ``ends in a colon (:\\
            ---\ipa{z} \change\ \ipa{z}\\
            \ipa{z} \change\ \ipa{z}
            """;
        var latexParser = MockableILatexParser.Mock();
        latexParser.ParseLatexSegment(Any(), Any()).Callback((str, sb) => sb.Append(str));
        var ruleParser = IPhonologicalRuleParser.Mock();
        // mock arg matching seem to work in reverse order.
        ruleParser.TryParseRule(str => str.Contains('z')).Returns(true).SetsOutRule(_defaultRule);
        ruleParser.TryParseRule(str => str.StartsWith("---\\")).Returns(true).SetsOutRule(_defaultRule);
        DiachronicaParser parser = new(latexParser.Object, ruleParser.Object);

        var result = parser.Parse(new StringReader(input));

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Rules).Count().IsEqualTo(4);
        await Assert.That(result[0].Rules[0].Note).IsEmpty();
        await Assert.That(result[0].Rules[1].Note).StartsWith("Initials:");
        await Assert.That(result[0].Rules[2].Note).StartsWith("Initials:");
        await Assert.That(result[0].Rules[3].Note).IsEmpty();
    }

    [Test]
    public async Task Parse_HasGreedyNote_TakesAll()
    {
        const string input =
            """
            \section{s1}
            \subsection{s2}
            Initials:\\
            \ipa{z} \change\ \ipa{z}\\
            \ipa{z} \change\ \ipa{z}\\
            \ipa{z} \change\ \ipa{z}
            """;
        var latexParser = MockableILatexParser.Mock();
        latexParser.ParseLatexSegment(Any(), Any()).Callback((str, sb) => sb.Append(str));
        var ruleParser = IPhonologicalRuleParser.Mock();
        ruleParser.TryParseRule(str => str.Contains('z')).Returns(true).SetsOutRule(_defaultRule);
        DiachronicaParser parser = new(latexParser.Object, ruleParser.Object);

        var result = parser.Parse(new StringReader(input));

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Rules).Count().IsEqualTo(3);
        await Assert.That(result[0].Rules[0].Note).StartsWith("Initials:");
        await Assert.That(result[0].Rules[1].Note).StartsWith("Initials:");
        await Assert.That(result[0].Rules[2].Note).StartsWith("Initials:");
    }

    [Test]
    [Skip("Unreasonable to use at this point, when other dependant classes are incomplete.")]
    public async Task Parse_EntireDiachronica_NonEmptyListings()
    {
        DiachronicaParser parser = new(new LatexParser(), new PhonologicalRuleParser(new LatexParser()));
        StringReader reader = new(_sampleDiachronica);

        var result = parser.Parse(reader);

        await Assert.That(result).IsNotEmpty();
    }
}
