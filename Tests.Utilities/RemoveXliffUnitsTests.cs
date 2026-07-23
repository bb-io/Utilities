using Apps.Utilities.Actions;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using System.Xml.Linq;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class RemoveXliffUnitsTests : TestBase
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
    public async Task Default_RemovesFinalUnitsAndSkeletonButPreservesMetadata()
    {
        const string fileName = "remove-units-default-2.0.xlf";

        var result = await Actions.RemoveXliffUnits(new RemoveXliffUnitsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffUnits", fileName),
                ContentType = "application/xliff+xml",
            },
        });

        Assert.AreEqual(5, result.TotalSegmentsBefore);
        Assert.AreEqual(3, result.TotalSegmentsAfter);
        Assert.AreEqual(2, result.UnitsLeft);
        Assert.AreEqual(2, result.RemovedSegmentsByState);
        Assert.AreEqual(0, result.RemovedSegmentsWithEmptyTarget);
        Assert.AreEqual(0, result.RemovedSegmentsUnderQualityThreshold);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        Assert.IsFalse(output.Descendants(ns + "skeleton").Any());
        Assert.AreEqual("source.html", output.Descendants(ns + "file").Single().Attribute("original")?.Value);
        Assert.IsTrue(output.Descendants(ns + "note").Any(x => x.Value == "Keep this note"));
        CollectionAssert.AreEquivalent(
            new[] { "u-mixed", "u-translated" },
            output.Descendants(ns + "unit").Select(x => x.Attribute("id")!.Value).ToArray());
    }

    [TestMethod]
    public async Task EnabledFilters_UseOrLogicAndCountEveryMatchingReason()
    {
        const string fileName = "remove-units-filters-2.0.xlf";

        var result = await Actions.RemoveXliffUnits(new RemoveXliffUnitsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffUnits", fileName),
                ContentType = "application/xliff+xml",
            },
            SegmentStates = ["final", "reviewed"],
            RemoveUnitsWithEmptyTargets = true,
            RemoveUnitsUnderQualityThreshold = true,
            StripSkeleton = false,
        });

        Assert.AreEqual(6, result.TotalSegmentsBefore);
        Assert.AreEqual(3, result.TotalSegmentsAfter);
        Assert.AreEqual(2, result.UnitsLeft);
        Assert.AreEqual(2, result.RemovedSegmentsByState);
        Assert.AreEqual(3, result.RemovedSegmentsWithEmptyTarget);
        Assert.AreEqual(2, result.RemovedSegmentsUnderQualityThreshold);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        Assert.IsTrue(output.Descendants(ns + "skeleton").Any());
        CollectionAssert.AreEquivalent(
            new[] { "u-partly-empty", "u-equal" },
            output.Descendants(ns + "unit").Select(x => x.Attribute("id")!.Value).ToArray());
    }

    [TestMethod]
    public async Task CallerQualityLimit_OverridesStoredThreshold()
    {
        const string fileName = "remove-units-quality-2.0.xlf";

        var result = await Actions.RemoveXliffUnits(new RemoveXliffUnitsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffUnits", fileName),
                ContentType = "application/xliff+xml",
            },
            QualityThresholdLimit = 60,
        });

        Assert.AreEqual(3, result.TotalSegmentsBefore);
        Assert.AreEqual(2, result.TotalSegmentsAfter);
        Assert.AreEqual(2, result.UnitsLeft);
        Assert.AreEqual(1, result.RemovedSegmentsUnderQualityThreshold);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.0";
        CollectionAssert.AreEquivalent(
            new[] { "u-equal", "u-no-score" },
            output.Descendants(ns + "unit").Select(x => x.Attribute("id")!.Value).ToArray());
    }

    [TestMethod]
    public async Task Xliff12_RemovesNestedUnitAndSkeletonButKeepsHeaderMetadata()
    {
        const string fileName = "remove-units-nested-1.2.xlf";

        var result = await Actions.RemoveXliffUnits(new RemoveXliffUnitsRequest
        {
            File = new FileReference
            {
                Name = Path.Combine("RemoveXliffUnits", fileName),
                ContentType = "application/xliff+xml",
            },
        });

        Assert.AreEqual(2, result.TotalSegmentsBefore);
        Assert.AreEqual(1, result.TotalSegmentsAfter);
        Assert.AreEqual(1, result.UnitsLeft);

        var output = await LoadOutput(result.File);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:1.2";
        Assert.AreEqual("1.2", output.Root?.Attribute("version")?.Value);
        Assert.IsFalse(output.Descendants(ns + "skl").Any());
        Assert.IsTrue(output.Descendants(ns + "phase-group").Any());
        Assert.IsTrue(output.Descendants(ns + "group").Any());
        Assert.AreEqual("u-translated", output.Descendants(ns + "trans-unit").Single().Attribute("id")?.Value);
    }

    [TestMethod]
    [DataRow(-0.01)]
    [DataRow(100.01)]
    public async Task InvalidQualityLimit_Throws(double limit)
    {
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            Actions.RemoveXliffUnits(new RemoveXliffUnitsRequest
            {
                File = new FileReference
                {
                    Name = Path.Combine("RemoveXliffUnits", "unused.xlf"),
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
