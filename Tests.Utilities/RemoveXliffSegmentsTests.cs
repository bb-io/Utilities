using Apps.Utilities.Actions;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using System.Xml.Linq;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class RemoveXliffSegmentsTests : TestBase
{
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
    public async Task Default_RemovesAllSegmentsAndUnitsButPreservesMetadata()
    {
        const string fileName = "remove-segments-default-2.0.xlf";

        var result = await Actions.RemoveXliffSegments(new RemoveXliffSegmentsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffSegments", fileName),
                ContentType = "application/xliff+xml",
            },
        });

        Assert.AreEqual(5, result.TotalSegmentsBefore);
        Assert.AreEqual(0, result.TotalSegmentsAfter);
        Assert.AreEqual(0, result.KeptSegmentsByState);
        Assert.AreEqual(0, result.KeptSegmentsWithEmptyTarget);
        Assert.AreEqual(0, result.KeptSegmentsUnderQualityThreshold);
        Assert.AreEqual(5, result.RemovedSegmentsByState);
        Assert.AreEqual(0, result.RemovedSegmentsWithEmptyTarget);
        Assert.AreEqual(0, result.RemovedSegmentsUnderQualityThreshold);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        Assert.IsFalse(output.Descendants(ns + "skeleton").Any());
        Assert.AreEqual("source.html", output.Descendants(ns + "file").Single().Attribute("original")?.Value);
        Assert.IsTrue(output.Descendants(ns + "note").Any(x => x.Value == "Keep this note"));
        Assert.IsFalse(output.Descendants(ns + "unit").Any());
    }

    [TestMethod]
    public async Task EnabledKeepRules_UseOrLogicAndCountOverlappingResults()
    {
        const string fileName = "remove-segments-filters-2.0.xlf";

        var result = await Actions.RemoveXliffSegments(new RemoveXliffSegmentsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffSegments", fileName),
                ContentType = "application/xliff+xml",
            },
            SegmentStatesToKeep = ["final", "reviewed"],
            KeepSegmentsWithEmptyTargets = true,
            KeepSegmentsUnderQualityThreshold = true,
            StripSkeleton = false,
        });

        Assert.AreEqual(6, result.TotalSegmentsBefore);
        Assert.AreEqual(4, result.TotalSegmentsAfter);
        Assert.AreEqual(2, result.KeptSegmentsByState);
        Assert.AreEqual(4, result.KeptSegmentsWithEmptyTarget);
        Assert.AreEqual(2, result.KeptSegmentsUnderQualityThreshold);
        Assert.AreEqual(2, result.RemovedSegmentsByState);
        Assert.AreEqual(0, result.RemovedSegmentsWithEmptyTarget);
        Assert.AreEqual(0, result.RemovedSegmentsUnderQualityThreshold);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        Assert.IsTrue(output.Descendants(ns + "skeleton").Any());
        CollectionAssert.AreEquivalent(
            new[] { "u-overlap", "u-partly-empty", "u-empty" },
            output.Descendants(ns + "unit").Select(x => x.Attribute("id")!.Value).ToArray());
        CollectionAssert.AreEquivalent(
            new[] { "s1", "s2", "s3", "s5" },
            output.Descendants(ns + "segment").Select(x => x.Attribute("id")!.Value).ToArray());
    }

    [TestMethod]
    public async Task DisabledKeepRules_ReportRemovedSegmentAttributes()
    {
        const string fileName = "remove-segments-filters-2.0.xlf";

        var result = await Actions.RemoveXliffSegments(new RemoveXliffSegmentsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffSegments", fileName),
                ContentType = "application/xliff+xml",
            },
            SegmentStatesToKeep = ["translated"],
        });

        Assert.AreEqual(6, result.TotalSegmentsBefore);
        Assert.AreEqual(4, result.TotalSegmentsAfter);
        Assert.AreEqual(4, result.KeptSegmentsByState);
        Assert.AreEqual(2, result.KeptSegmentsWithEmptyTarget);
        Assert.AreEqual(0, result.KeptSegmentsUnderQualityThreshold);
        Assert.AreEqual(2, result.RemovedSegmentsByState);
        Assert.AreEqual(2, result.RemovedSegmentsWithEmptyTarget);
        Assert.AreEqual(2, result.RemovedSegmentsUnderQualityThreshold);
    }

    [TestMethod]
    public async Task StoredQualityThreshold_KeepsOnlySegmentsBelowIt()
    {
        const string fileName = "remove-segments-quality-2.0.xlf";

        var result = await Actions.RemoveXliffSegments(new RemoveXliffSegmentsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffSegments", fileName),
                ContentType = "application/xliff+xml",
            },
            SegmentStatesToKeep = ["final"],
            KeepSegmentsUnderQualityThreshold = true,
        });

        Assert.AreEqual(3, result.TotalSegmentsBefore);
        Assert.AreEqual(1, result.TotalSegmentsAfter);
        Assert.AreEqual(0, result.KeptSegmentsByState);
        Assert.AreEqual(1, result.KeptSegmentsUnderQualityThreshold);
        Assert.AreEqual(2, result.RemovedSegmentsByState);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        Assert.AreEqual(
            "u-equal",
            output.Descendants(ns + "unit").Single().Attribute("id")?.Value);
    }

    [TestMethod]
    public async Task CallerQualityLimit_EnablesKeepRuleAndOverridesStoredThreshold()
    {
        const string fileName = "remove-segments-quality-2.0.xlf";

        var result = await Actions.RemoveXliffSegments(new RemoveXliffSegmentsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffSegments", fileName),
                ContentType = "application/xliff+xml",
            },
            SegmentStatesToKeep = ["final"],
            QualityThresholdLimit = 60,
        });

        Assert.AreEqual(3, result.TotalSegmentsBefore);
        Assert.AreEqual(1, result.TotalSegmentsAfter);
        Assert.AreEqual(0, result.KeptSegmentsByState);
        Assert.AreEqual(1, result.KeptSegmentsUnderQualityThreshold);
        Assert.AreEqual(2, result.RemovedSegmentsByState);
        Assert.AreEqual(0, result.RemovedSegmentsUnderQualityThreshold);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        CollectionAssert.AreEquivalent(
            new[] { "u-below" },
            output.Descendants(ns + "unit").Select(x => x.Attribute("id")!.Value).ToArray());
    }

    [TestMethod]
    public async Task StateForRemainingSegments_SetsStateAfterFiltering()
    {
        const string fileName = "remove-segments-filters-2.0.xlf";

        var result = await Actions.RemoveXliffSegments(new RemoveXliffSegmentsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffSegments", fileName),
                ContentType = "application/xliff+xml",
            },
            SegmentStatesToKeep = ["final", "reviewed"],
            StateForRemainingSegments = "translated",
        });

        Assert.AreEqual(2, result.TotalSegmentsAfter);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        Assert.IsTrue(output.Descendants(ns + "segment").All(
            segment => segment.Attribute("state")?.Value == "translated"));
    }

    [TestMethod]
    public async Task StateForRemainingSegments_SupportsXliff12()
    {
        const string fileName = "remove-segments-nested-1.2.xlf";

        var result = await Actions.RemoveXliffSegments(new RemoveXliffSegmentsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffSegments", fileName),
                ContentType = "application/xliff+xml",
            },
            SegmentStatesToKeep = ["translated"],
            StateForRemainingSegments = "final",
        });

        Assert.AreEqual(1, result.TotalSegmentsAfter);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:1.2";
        Assert.AreEqual(
            "final",
            output.Descendants(ns + "target").Single().Attribute("state")?.Value);
    }

    [TestMethod]
    public async Task Xliff12_DefaultRemovesAllUnitsAndSkeletonButKeepsHeaderMetadata()
    {
        const string fileName = "remove-segments-nested-1.2.xlf";

        var result = await Actions.RemoveXliffSegments(new RemoveXliffSegmentsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffSegments", fileName),
                ContentType = "application/xliff+xml",
            },
        });

        Assert.AreEqual(2, result.TotalSegmentsBefore);
        Assert.AreEqual(0, result.TotalSegmentsAfter);
        Assert.AreEqual(0, result.KeptSegmentsByState);
        Assert.AreEqual(2, result.RemovedSegmentsByState);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:1.2";
        Assert.AreEqual("1.2", output.Root?.Attribute("version")?.Value);
        Assert.IsFalse(output.Descendants(ns + "skl").Any());
        Assert.IsTrue(output.Descendants(ns + "phase-group").Any());
        Assert.IsTrue(output.Descendants(ns + "group").Any());
        Assert.IsFalse(output.Descendants(ns + "trans-unit").Any());
    }

    [TestMethod]
    [DataRow(-0.01)]
    [DataRow(100.01)]
    public async Task InvalidQualityLimit_Throws(double limit)
    {
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            Actions.RemoveXliffSegments(new RemoveXliffSegmentsRequest
            {
                File = new FileReference
                {
                    Name = Path.Combine("RemoveXliffSegments", "unused.xlf"),
                    ContentType = "application/xliff+xml",
                },
                QualityThresholdLimit = limit,
            }));

        StringAssert.Contains(exception.Message, "between 0 and 100");
    }

    private async Task<XDocument> LoadOutput(FileReference file)
    {
        await using var stream = await FileManager.DownloadAsync(file);
        return await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
    }
}
