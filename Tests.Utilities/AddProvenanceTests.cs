using Apps.Utilities.Actions;
using Apps.Utilities.DataSourceHandlers;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Filters.Transformations;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class AddProvenanceTests
{
    private readonly FileManager _fileManager;
    private readonly Xliff _actions;

    public AddProvenanceTests()
    {
        var testFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
        _fileManager = new FileManager(testFolder);
        _actions = new Xliff(_fileManager);
    }

    [TestMethod]
    public async Task AddProvenance_WithSupportedNonXliff_AddsTranslationProvenance()
    {
        var result = await _actions.AddProvenance(new AddProvenanceRequest
        {
            File = new FileReference { Name = "test.txt", ContentType = "text/plain" },
            ProvenanceType = ProvenanceTypeDataSourceHandler.Translation,
            Person = "Jane Translator",
            OrganizationReference = "https://example.com/organizations/localization",
            Tool = "Blackbird",
        });

        Assert.AreEqual("test.txt.xlf", result.File.Name);
        Assert.AreEqual("application/xliff+xml", result.File.ContentType);

        await using var output = await _fileManager.DownloadAsync(result.File);
        var loadResult = Transformation.Load(output, result.File.Name);
        Assert.IsTrue(loadResult.Success, loadResult.Error);
        var transformation = loadResult.Value!;

        Assert.AreEqual("Jane Translator", transformation.Provenance.Translation.Person);
        Assert.IsNull(transformation.Provenance.Translation.PersonReference);
        Assert.AreEqual(
            "https://example.com/organizations/localization",
            transformation.Provenance.Translation.OrganizationReference);
        Assert.AreEqual("Blackbird", transformation.Provenance.Translation.Tool);
        Assert.IsNull(transformation.Provenance.Review.Person);
    }

    [TestMethod]
    public async Task AddProvenance_WithReviewType_PreservesExistingTranslationProvenance()
    {
        var result = await _actions.AddProvenance(new AddProvenanceRequest
        {
            File = new FileReference
            {
                Name = "ApplyXliffTargetTranslations/target-metadata-2.0.xlf"
            },
            ProvenanceType = ProvenanceTypeDataSourceHandler.Review,
            PersonReference = "https://example.com/reviewers/42",
            ToolReference = "https://example.com/review-tool",
        });

        await using var output = await _fileManager.DownloadAsync(result.File);
        var loadResult = Transformation.Load(output, result.File.Name);
        Assert.IsTrue(loadResult.Success, loadResult.Error);
        var transformation = loadResult.Value!;
        var unit = transformation.GetUnits().Single();

        Assert.AreEqual("Target translator", unit.Provenance.Translation.Person);
        Assert.AreEqual(
            "https://example.com/reviewers/42",
            transformation.Provenance.Review.PersonReference);
        Assert.AreEqual(
            "https://example.com/review-tool",
            transformation.Provenance.Review.ToolReference);
    }

    [TestMethod]
    public async Task AddProvenance_WithoutType_ThrowsMisconfiguration()
    {
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            _actions.AddProvenance(new AddProvenanceRequest
            {
                File = new FileReference { Name = "test.txt" },
            }));

        StringAssert.Contains(exception.Message, "Provenance type is required");
    }

    [TestMethod]
    public async Task AddProvenance_WithNameAndReference_AcceptsBothValues()
    {
        var result = await _actions.AddProvenance(new AddProvenanceRequest
        {
            File = new FileReference { Name = "test.txt" },
            ProvenanceType = ProvenanceTypeDataSourceHandler.Translation,
            Tool = "Tool name",
            ToolReference = "https://example.com/tool",
        });

        Assert.AreEqual("test.txt.xlf", result.File.Name);
    }
}
