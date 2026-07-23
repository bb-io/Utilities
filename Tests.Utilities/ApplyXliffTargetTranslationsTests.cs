using Apps.Utilities.Actions;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using System.Xml.Linq;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class ApplyXliffTargetTranslationsTests : TestBase
{
    private const string TestFilesFolder = "ApplyXliffTargetTranslations";
    private Xliff Actions => new(FileManager);

    [TestInitialize]
    public void Init()
    {
        var outputDirectory = Path.Combine(GetTestFolderPath(), "Output");
        if (Directory.Exists(outputDirectory))
            Directory.Delete(outputDirectory, true);
        Directory.CreateDirectory(outputDirectory);
    }

    [TestMethod]
    public async Task AppliesTargetsAndPreservesTargetFileSemantics()
    {
        const string targetFileName = "target-main-2.0.xlf";
        var result = await Actions.ApplyXliffTargetTranslations(new ApplyXliffTargetTranslationsRequest
        {
            TargetFile = CreateFileReference(targetFileName, "application/custom-xliff"),
            TranslationsFile = CreateFileReference("translations-main-2.0.xlf"),
        });

        Assert.AreEqual(Path.Combine(TestFilesFolder, targetFileName), result.File.Name);
        Assert.AreEqual("application/custom-xliff", result.File.ContentType);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace its = "http://www.w3.org/2005/11/its";
        var file = output.Descendants(ns + "file").Single();
        var unit = output.Descendants(ns + "unit").Single();
        var segments = unit.Elements(ns + "segment").ToList();

        Assert.AreEqual("2.0", output.Root?.Attribute("version")?.Value);
        Assert.AreEqual("en", output.Root?.Attribute("srcLang")?.Value);
        Assert.AreEqual("fr", output.Root?.Attribute("trgLang")?.Value);
        Assert.AreEqual("target-file", file.Attribute("id")?.Value);
        Assert.AreEqual("target.html", file.Attribute("original")?.Value);
        Assert.AreEqual("target skeleton", file.Element(ns + "skeleton")?.Value);
        Assert.IsTrue(output.Descendants(ns + "note").Any(note => note.Value == "Target file note"));
        Assert.IsTrue(output.Descendants(ns + "note").Any(note => note.Value == "Target unit note"));
        Assert.AreEqual("Target translator", unit.Attribute(its + "person")?.Value);
        Assert.AreEqual("25", unit.Attribute(its + "locQualityRatingScore")?.Value);

        var idMatchedSegment = segments.Single(segment => segment.Attribute("id")?.Value == "s-id");
        Assert.AreEqual("New ID target", idMatchedSegment.Element(ns + "target")?.Value);
        Assert.AreEqual("Hello", idMatchedSegment.Element(ns + "source")?.Value);
        Assert.AreEqual("reviewed", idMatchedSegment.Attribute("state")?.Value);

        var sourceMatchedSegment = segments.Single(
            segment => segment.Element(ns + "source")?.Value == "Exact source");
        Assert.AreEqual("New source target", sourceMatchedSegment.Element(ns + "target")?.Value);
        Assert.AreEqual("translated", sourceMatchedSegment.Attribute("state")?.Value);

        Assert.AreEqual(
            "Keep empty target",
            segments.Single(segment => segment.Attribute("id")?.Value == "s-empty")
                .Element(ns + "target")?.Value);
        Assert.AreEqual(
            "Keep unmatched target",
            segments.Single(segment => segment.Attribute("id")?.Value == "s-unchanged")
                .Element(ns + "target")?.Value);
        Assert.AreEqual(
            "Keep case-sensitive target",
            segments.Single(segment => segment.Element(ns + "source")?.Value == "Case-sensitive source")
                .Element(ns + "target")?.Value);

        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("empty target", StringComparison.Ordinal)));
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("case-sensitive source", StringComparison.Ordinal)));
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("s-missing", StringComparison.Ordinal)));
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("u-missing", StringComparison.Ordinal)));
    }

    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public async Task UnitMetadataOptionsOperateIndependently(
        bool copyProvenance,
        bool copyQuality)
    {
        var result = await Actions.ApplyXliffTargetTranslations(new ApplyXliffTargetTranslationsRequest
        {
            TargetFile = CreateFileReference("target-metadata-2.0.xlf"),
            TranslationsFile = CreateFileReference("translations-metadata-2.0.xlf"),
            CopyProvenanceMetadata = copyProvenance,
            CopyQualityData = copyQuality,
        });

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace its = "http://www.w3.org/2005/11/its";
        var unit = output.Descendants(ns + "unit").Single();

        var provenancePrefix = copyProvenance ? "Donor" : "Target";
        Assert.AreEqual($"{provenancePrefix} translator", unit.Attribute(its + "person")?.Value);
        Assert.AreEqual($"{provenancePrefix} organization", unit.Attribute(its + "org")?.Value);
        Assert.AreEqual($"{provenancePrefix} tool", unit.Attribute(its + "tool")?.Value);
        Assert.AreEqual($"{provenancePrefix} reviewer", unit.Attribute(its + "revPerson")?.Value);
        Assert.AreEqual(
            $"{provenancePrefix} review organization",
            unit.Attribute(its + "revOrg")?.Value);
        Assert.AreEqual($"{provenancePrefix} review tool", unit.Attribute(its + "revTool")?.Value);

        Assert.AreEqual(
            copyQuality ? "95" : "25",
            unit.Attribute(its + "locQualityRatingScore")?.Value);
        Assert.AreEqual(
            copyQuality ? "80" : "50",
            unit.Attribute(its + "locQualityRatingScoreThreshold")?.Value);
        Assert.AreEqual(
            copyQuality ? "1" : "-1",
            unit.Attribute(its + "locQualityRatingVote")?.Value);
        Assert.AreEqual(
            copyQuality ? "2" : "1",
            unit.Attribute(its + "locQualityRatingVoteThreshold")?.Value);
        Assert.AreEqual(
            copyQuality ? "urn:quality:donor" : "urn:quality:target",
            unit.Attribute(its + "locQualityRatingProfileRef")?.Value);
        Assert.AreEqual("New target", unit.Descendants(ns + "target").Single().Value);
    }

    [TestMethod]
    public async Task AmbiguousAndCollidingMatchesAreSkippedWithWarnings()
    {
        var result = await Actions.ApplyXliffTargetTranslations(new ApplyXliffTargetTranslationsRequest
        {
            TargetFile = CreateFileReference("target-ambiguity-2.0.xlf"),
            TranslationsFile = CreateFileReference("translations-ambiguity-2.0.xlf"),
        });

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        var targets = output.Descendants(ns + "target").Select(target => target.Value).ToList();

        CollectionAssert.Contains(targets, "Keep shared");
        CollectionAssert.Contains(targets, "Keep duplicate");
        CollectionAssert.Contains(targets, "Keep target duplicate one");
        CollectionAssert.Contains(targets, "Keep target duplicate two");
        CollectionAssert.Contains(targets, "Keep ambiguous one");
        CollectionAssert.Contains(targets, "Keep ambiguous two");
        CollectionAssert.Contains(targets, "Keep repeated");
        CollectionAssert.Contains(targets, "Keep one");
        CollectionAssert.Contains(targets, "Keep two");
        CollectionAssert.Contains(targets, "Keep three");

        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("duplicate segment ID", StringComparison.Ordinal)));
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("multiple segments without IDs", StringComparison.Ordinal)));
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("matches multiple target segments", StringComparison.Ordinal)));
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("resolving to the same target segment", StringComparison.Ordinal)));
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("no matching target segment", StringComparison.Ordinal)));
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("duplicated in the target file", StringComparison.Ordinal)));
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("duplicated in the translations file", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task AppliesCrossVersionTranslationsAndKeepsXliff2TargetVersion()
    {
        var result = await Actions.ApplyXliffTargetTranslations(new ApplyXliffTargetTranslationsRequest
        {
            TargetFile = CreateFileReference("target-cross-2.0.xlf"),
            TranslationsFile = CreateFileReference("translations-cross-1.2.xlf"),
        });

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        var segment = output.Descendants(ns + "segment").Single();

        Assert.AreEqual(0, result.Warnings.Count);
        Assert.AreEqual("2.0", output.Root?.Attribute("version")?.Value);
        Assert.AreEqual("Cross-version target", segment.Element(ns + "target")?.Value);
        Assert.AreEqual("reviewed", segment.Attribute("state")?.Value);
    }

    [TestMethod]
    public async Task AppliesCrossVersionTranslationsAndKeepsXliff12TargetSemantics()
    {
        var result = await Actions.ApplyXliffTargetTranslations(new ApplyXliffTargetTranslationsRequest
        {
            TargetFile = CreateFileReference("target-cross-1.2.xlf"),
            TranslationsFile = CreateFileReference("translations-cross-2.0.xlf"),
        });

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:1.2";
        var file = output.Descendants(ns + "file").Single();
        var target = output.Descendants(ns + "target").Single();

        Assert.AreEqual(0, result.Warnings.Count);
        Assert.AreEqual("1.2", output.Root?.Attribute("version")?.Value);
        Assert.AreEqual("target.txt", file.Attribute("original")?.Value);
        Assert.AreEqual("fr", file.Attribute("target-language")?.Value);
        Assert.AreEqual("New target", target.Value);
        Assert.AreEqual("translated", target.Attribute("state")?.Value);
        Assert.AreEqual(
            "target.txt.skl",
            output.Descendants(ns + "external-file").Single().Attribute("href")?.Value);
        Assert.AreEqual("Target note", output.Descendants(ns + "note").Single().Value);
    }

    [TestMethod]
    public async Task InvalidTranslationsXliffUsesExistingParserError()
    {
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            Actions.ApplyXliffTargetTranslations(new ApplyXliffTargetTranslationsRequest
            {
                TargetFile = CreateFileReference("target-main-2.0.xlf"),
                TranslationsFile = CreateFileReference("invalid.xlf"),
            }));

        StringAssert.Contains(exception.Message, "not a valid XLIFF file");
    }

    private static FileReference CreateFileReference(
        string fileName,
        string contentType = "application/xliff+xml")
    {
        return new FileReference
        {
            Name = Path.Combine(TestFilesFolder, fileName),
            ContentType = contentType,
        };
    }

    private async Task<XDocument> LoadOutput(FileReference file)
    {
        await using var stream = await FileManager.DownloadAsync(file);
        return await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
    }
}
