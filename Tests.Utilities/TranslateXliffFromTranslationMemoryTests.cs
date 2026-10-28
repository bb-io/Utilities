using Apps.Utilities.Actions;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Files;
using System.Xml.Linq;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class TranslateXliffFromTranslationMemoryTests : TestBase
{
    private const string TestFilesFolder = "TranslateXliffFromTranslationMemory";
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
    public async Task AppliesPlainAndInlineTranslationsAndPreservesXliffVersion()
    {
        var result = await Actions.TranslateXliffFromTranslationMemory(
            new TranslateXliffFromTranslationMemoryRequest
            {
                XliffFile = CreateFileReference("target-2.0.xlf", "application/custom-xliff"),
                TranslationMemory = CreateFileReference("memory.tmx", "application/x-tmx+xml"),
            });

        Assert.AreEqual(3, result.SegmentsTranslated);
        Assert.AreEqual(1, result.SegmentsWithoutMatch);
        Assert.AreEqual(0, result.SegmentsSkipped);
        Assert.AreEqual(0, result.Warnings.Count);
        Assert.AreEqual("application/custom-xliff", result.File.ContentType);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        Assert.AreEqual("2.0", output.Root?.Attribute("version")?.Value);

        var plainTarget = output.Descendants(ns + "segment")
            .Single(segment => segment.Attribute("id")?.Value == "s1")
            .Element(ns + "target");
        Assert.AreEqual("Hallo Welt", plainTarget?.Value);

        var inlineTarget = output.Descendants(ns + "segment")
            .Single(segment => segment.Attribute("id")?.Value == "s2")
            .Element(ns + "target");
        Assert.IsNotNull(inlineTarget);
        Assert.AreEqual("Hier klicken.", inlineTarget.Value);
        Assert.AreEqual(1, inlineTarget.Descendants(ns + "pc").Count());
        Assert.AreEqual("Hier klicken", inlineTarget.Descendants(ns + "pc").Single().Value);

        var placeholderTarget = output.Descendants(ns + "segment")
            .Single(segment => segment.Attribute("id")?.Value == "s4")
            .Element(ns + "target");
        Assert.IsNotNull(placeholderTarget);
        Assert.AreEqual("Zeilenumbruch", placeholderTarget.Value);
        Assert.AreEqual(1, placeholderTarget.Descendants(ns + "ph").Count());

        var unmatchedTarget = output.Descendants(ns + "segment")
            .Single(segment => segment.Attribute("id")?.Value == "s3")
            .Element(ns + "target");
        Assert.AreEqual("Keep unmatched target", unmatchedTarget?.Value);
    }

    [TestMethod]
    public async Task SkipsConflictingTranslationsAndTargetsWithIncompatibleInlineTags()
    {
        var result = await Actions.TranslateXliffFromTranslationMemory(
            new TranslateXliffFromTranslationMemoryRequest
            {
                XliffFile = CreateFileReference("target-2.0.xlf"),
                TranslationMemory = CreateFileReference("memory-conflicts.tmx", "application/x-tmx+xml"),
            });

        Assert.AreEqual(0, result.SegmentsTranslated);
        Assert.AreEqual(3, result.SegmentsWithoutMatch);
        Assert.AreEqual(1, result.SegmentsSkipped);
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("multiple different targets", StringComparison.Ordinal)));
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("does not contain all inline codes", StringComparison.Ordinal)));

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        var targets = output.Descendants(ns + "target").Select(target => target.Value).ToList();
        CollectionAssert.Contains(targets, "Old plain target");
        CollectionAssert.Contains(targets, "Old inline target");
        CollectionAssert.Contains(targets, "Keep unmatched target");
        CollectionAssert.Contains(targets, "Old placeholder target");
    }

    [TestMethod]
    public async Task AppliesInlineTranslationAndPreservesXliff12()
    {
        var result = await Actions.TranslateXliffFromTranslationMemory(
            new TranslateXliffFromTranslationMemoryRequest
            {
                XliffFile = CreateFileReference("target-1.2.xlf"),
                TranslationMemory = CreateFileReference("memory.tmx", "application/x-tmx+xml"),
            });

        Assert.AreEqual(1, result.SegmentsTranslated, string.Join(Environment.NewLine, result.Warnings));
        Assert.AreEqual(0, result.SegmentsWithoutMatch);
        Assert.AreEqual(0, result.SegmentsSkipped);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:1.2";
        Assert.AreEqual("1.2", output.Root?.Attribute("version")?.Value);
        var target = output.Descendants(ns + "target").Single();
        Assert.AreEqual("Hier klicken.", string.Concat(target.Nodes().OfType<XText>().Select(node => node.Value)));
        Assert.AreEqual(1, target.Descendants(ns + "bpt").Count());
        Assert.AreEqual(1, target.Descendants(ns + "ept").Count());
    }

    [DataTestMethod]
    [DataRow(false, 0, 1, "")]
    [DataRow(true, 1, 0, "測試")]
    public async Task ClientSmartlingFilesRespectSourceCaseOption(
        bool ignoreSourceCase,
        int expectedTranslated,
        int expectedWithoutMatch,
        string expectedTarget)
    {
        var result = await Actions.TranslateXliffFromTranslationMemory(
            new TranslateXliffFromTranslationMemoryRequest
            {
                XliffFile = CreateFileReference("client-7dc1b741.xliff"),
                TranslationMemory = CreateFileReference("client-7dc1b741.tmx", "application/x-tmx+xml"),
                IgnoreSourceCase = ignoreSourceCase,
            });

        Assert.AreEqual(expectedTranslated, result.SegmentsTranslated);
        Assert.AreEqual(expectedWithoutMatch, result.SegmentsWithoutMatch);
        Assert.AreEqual(0, result.SegmentsSkipped);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:1.2";
        Assert.AreEqual(expectedTarget, output.Descendants(ns + "target").SingleOrDefault()?.Value ?? string.Empty);
    }

    private static FileReference CreateFileReference(string fileName, string contentType = "application/xliff+xml") =>
        new()
        {
            Name = Path.Combine(TestFilesFolder, fileName),
            ContentType = contentType,
        };

    private async Task<XDocument> LoadOutput(FileReference file)
    {
        await using var stream = await FileManager.DownloadAsync(file);
        return await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
    }
}
