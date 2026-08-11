using Apps.Utilities.Actions;
using Apps.Utilities.DataSourceHandlers;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Filters.Coders;
using System.Text;
using System.Xml.Linq;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class XliffLocaleAndRegexTests : TestBase
{
    private const string TestFilesFolder = "XliffLocaleAndRegex";
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
    public async Task SetLocales_PreservesXliff12AndUpdatesEveryFile()
    {
        var result = await Actions.SetXliffLocales(new SetXliffLocalesRequest
        {
            File = CreateFileReference("locales-multifile-1.2.xlf"),
            NewSourceLocale = "es",
            NewTargetLocale = "pt-BR",
        });

        Assert.AreEqual("en", result.SourceLocaleChangedFrom);
        Assert.AreEqual("es", result.SourceLocaleChangedTo);
        Assert.AreEqual("fr", result.TargetLocaleChangedFrom);
        Assert.AreEqual("pt-BR", result.TargetLocaleChangedTo);

        var output = await LoadOutputXml(result.File);
        Assert.AreEqual("1.2", output.Root?.Attribute("version")?.Value);
        Assert.IsTrue(output.Descendants()
            .Where(element => element.Name.LocalName == "file")
            .All(element => element.Attribute("source-language")?.Value == "es"));
        Assert.IsTrue(output.Descendants()
            .Where(element => element.Name.LocalName == "file")
            .All(element => element.Attribute("target-language")?.Value == "pt-BR"));
    }

    [TestMethod]
    public async Task SetLocales_PreservesXliff21()
    {
        var result = await Actions.SetXliffLocales(new SetXliffLocalesRequest
        {
            File = CreateFileReference("locales-2.1.xlf"),
            NewTargetLocale = "de",
        });

        Assert.AreEqual("en", result.SourceLocaleChangedFrom);
        Assert.AreEqual("en", result.SourceLocaleChangedTo);
        Assert.AreEqual("fr", result.TargetLocaleChangedFrom);
        Assert.AreEqual("de", result.TargetLocaleChangedTo);

        var output = await LoadOutputXml(result.File);
        Assert.AreEqual("2.1", output.Root?.Attribute("version")?.Value);
        Assert.AreEqual("en", output.Root?.Attribute("srcLang")?.Value);
        Assert.AreEqual("de", output.Root?.Attribute("trgLang")?.Value);
    }

    [TestMethod]
    public async Task SetLocales_ConvertsNativeHtmlToXliff22AndDefaultsSourceLocale()
    {
        var result = await Actions.SetXliffLocales(new SetXliffLocalesRequest
        {
            File = CreateFileReference("source.html", "text/html"),
            NewTargetLocale = "fr",
        });

        Assert.IsNull(result.SourceLocaleChangedFrom);
        Assert.AreEqual("en", result.SourceLocaleChangedTo);
        Assert.IsNull(result.TargetLocaleChangedFrom);
        Assert.AreEqual("fr", result.TargetLocaleChangedTo);
        StringAssert.EndsWith(result.File.Name, "source.html.xlf");

        var output = await LoadOutputXml(result.File);
        Assert.AreEqual("2.2", output.Root?.Attribute("version")?.Value);
        Assert.AreEqual("en", output.Root?.Attribute("srcLang")?.Value);
        Assert.AreEqual("fr", output.Root?.Attribute("trgLang")?.Value);
    }

    [TestMethod]
    public async Task ReplaceTargets_CombinesStateAndTargetFiltersAndPreservesInlineCodes()
    {
        var result = await Actions.ReplaceInTargetsViaRegex(new ReplaceInTargetsViaRegexRequest
        {
            File = CreateFileReference("regex-2.0.xlf"),
            RegexPattern = @"(\d+)",
            Replacement = "[$1]",
            TargetMatchPattern = @"^Bonjour\s+monde 123$",
            SegmentStates = ["translated"],
        });

        var output = await LoadOutputXml(result.File);
        Assert.AreEqual("2.0", output.Root?.Attribute("version")?.Value);

        var segments = output.Descendants()
            .Where(element => element.Name.LocalName == "segment")
            .ToDictionary(element => element.Attribute("id")!.Value);
        var firstTarget = segments["s1"].Elements().Single(element => element.Name.LocalName == "target");

        Assert.AreEqual("Bonjour  monde [123]", firstTarget.Value);
        Assert.IsTrue(firstTarget.Elements().Any(element => element.Name.LocalName == "ph"));
        Assert.AreEqual(
            "Bonjour autre 456",
            segments["s2"].Elements().Single(element => element.Name.LocalName == "target").Value);
        Assert.AreEqual(
            "Ignore this 789",
            segments["s3"].Elements().Single(element => element.Name.LocalName == "target").Value);
    }

    [TestMethod]
    public async Task ReplaceTargets_EmptyReplacementRemovesMatchingText()
    {
        var result = await Actions.ReplaceInTargetsViaRegex(new ReplaceInTargetsViaRegexRequest
        {
            File = CreateFileReference("regex-2.0.xlf"),
            RegexPattern = @"monde\s*",
        });

        var output = await LoadOutputXml(result.File);
        var firstTarget = output.Descendants()
            .First(element => element.Name.LocalName == "target");

        Assert.AreEqual("Bonjour  123", firstTarget.Value);
        Assert.IsTrue(firstTarget.Elements().Any(element => element.Name.LocalName == "ph"));
    }

    [TestMethod]
    public async Task ReplaceTargets_SourceOnlyTextReturnsOriginalFormat()
    {
        var result = await Actions.ReplaceInTargetsViaRegex(new ReplaceInTargetsViaRegexRequest
        {
            File = CreateFileReference("source.txt", "text/plain"),
            RegexPattern = @"\d+",
            Replacement = "NUM",
            TargetMatchPattern = "^Alpha",
        });

        Assert.AreEqual(Path.Combine(TestFilesFolder, "source.txt"), result.File.Name);
        Assert.AreEqual("text/plain", result.File.ContentType);

        await using var stream = await FileManager.DownloadAsync(result.File);
        using var reader = new StreamReader(stream);
        var output = await reader.ReadToEndAsync();

        StringAssert.Contains(output, "Alpha NUM");
        StringAssert.Contains(output, "Beta 456");
    }

    [TestMethod]
    public async Task ReplaceTargets_SourceOnlyTextCanReturnXliff22()
    {
        var result = await Actions.ReplaceInTargetsViaRegex(new ReplaceInTargetsViaRegexRequest
        {
            File = CreateFileReference("source.txt", "text/plain"),
            RegexPattern = @"\d+",
            Replacement = "NUM",
            TargetMatchPattern = "^Alpha",
            OutputFileFormat = XliffOutputFormatDataSourceHandler.Xliff22,
        });

        var output = await LoadOutputXml(result.File);
        Assert.AreEqual("2.2", output.Root?.Attribute("version")?.Value);
        Assert.IsTrue(output.Descendants()
            .Where(element => element.Name.LocalName == "target")
            .Any(element => element.Value == "Alpha NUM"));
        Assert.IsTrue(output.Descendants()
            .Where(element => element.Name.LocalName == "target")
            .Any(element => element.Value == "Beta 456"));
    }

    [TestMethod]
    public async Task ReplaceTargets_PoDefaultsToValidOriginalPo()
    {
        var result = await Actions.ReplaceInTargetsViaRegex(new ReplaceInTargetsViaRegexRequest
        {
            File = CreateFileReference("regex.po", "text/x-gettext-translation"),
            RegexPattern = @"\d+",
            Replacement = "[number]",
            TargetMatchPattern = "^Hallo",
        });

        Assert.AreEqual(Path.Combine(TestFilesFolder, "regex.po"), result.File.Name);
        Assert.AreEqual("text/x-gettext-translation", result.File.ContentType);

        await using var stream = await FileManager.DownloadAsync(result.File);
        using var reader = new StreamReader(stream);
        var output = await reader.ReadToEndAsync();
        using var validationStream = new MemoryStream(Encoding.UTF8.GetBytes(output));
        Assert.IsNotNull(new PoCoder().TryLoad(validationStream, "text/x-gettext-translation"));
        StringAssert.Contains(output, "msgid \"Hello world 123\"");
        StringAssert.Contains(output, "msgstr \"Hallo wereld [number]\"");
        StringAssert.Contains(output, "msgstr \"Opslaan 456\"");
    }

    [DataTestMethod]
    [DataRow(XliffOutputFormatDataSourceHandler.Xliff12, "1.2")]
    [DataRow(XliffOutputFormatDataSourceHandler.Xliff22, "2.2")]
    public async Task ReplaceTargets_PoCanReturnSelectedXliffVersion(
        string outputFormat,
        string expectedVersion)
    {
        var result = await Actions.ReplaceInTargetsViaRegex(new ReplaceInTargetsViaRegexRequest
        {
            File = CreateFileReference("regex.po", "text/x-gettext-translation"),
            RegexPattern = @"\d+",
            Replacement = "NUM",
            OutputFileFormat = outputFormat,
        });

        StringAssert.EndsWith(result.File.Name, "regex.po.xlf");
        Assert.AreEqual("application/xliff+xml", result.File.ContentType);

        var output = await LoadOutputXml(result.File);
        Assert.AreEqual(expectedVersion, output.Root?.Attribute("version")?.Value);
        Assert.IsTrue(output.Descendants()
            .Where(element => element.Name.LocalName == "target")
            .Any(element => element.Value.Contains("NUM", StringComparison.Ordinal)));
    }

    [DataTestMethod]
    [DataRow("[", null, "regular expression pattern")]
    [DataRow(@"\d+", "[", "regular expression pattern")]
    public async Task ReplaceTargets_InvalidRegexThrows(
        string regexPattern,
        string? targetMatchPattern,
        string expectedMessage)
    {
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            Actions.ReplaceInTargetsViaRegex(new ReplaceInTargetsViaRegexRequest
            {
                File = CreateFileReference("regex-2.0.xlf"),
                RegexPattern = regexPattern,
                TargetMatchPattern = targetMatchPattern,
            }));

        StringAssert.Contains(exception.Message, expectedMessage);
    }

    [TestMethod]
    public async Task ReplaceTargets_InvalidReplacementThrows()
    {
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            Actions.ReplaceInTargetsViaRegex(new ReplaceInTargetsViaRegexRequest
            {
                File = CreateFileReference("regex-2.0.xlf"),
                RegexPattern = @"\d+",
                Replacement = "$999999999999999999999999999999999999999999999999999",
            }));

        StringAssert.Contains(exception.Message, "Replacement is invalid");
    }

    [TestMethod]
    public async Task ReplaceTargets_InvalidStateThrows()
    {
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            Actions.ReplaceInTargetsViaRegex(new ReplaceInTargetsViaRegexRequest
            {
                File = CreateFileReference("regex-2.0.xlf"),
                RegexPattern = @"\d+",
                SegmentStates = ["unsupported"],
            }));

        StringAssert.Contains(exception.Message, "segment states are invalid");
    }

    [TestMethod]
    public async Task ReplaceTargets_InvalidOutputFormatThrows()
    {
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            Actions.ReplaceInTargetsViaRegex(new ReplaceInTargetsViaRegexRequest
            {
                File = CreateFileReference("regex-2.0.xlf"),
                RegexPattern = @"\d+",
                OutputFileFormat = "unsupported",
            }));

        StringAssert.Contains(exception.Message, "Output file format is invalid");
    }

    [TestMethod]
    public async Task ReplaceTargets_UnsupportedFileThrows()
    {
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            Actions.ReplaceInTargetsViaRegex(new ReplaceInTargetsViaRegexRequest
            {
                File = CreateFileReference("unsupported.bin", "application/octet-stream"),
                RegexPattern = "test",
            }));

        StringAssert.Contains(exception.Message, "supported by Blackbird Filters");
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

    private async Task<XDocument> LoadOutputXml(FileReference file)
    {
        await using var stream = await FileManager.DownloadAsync(file);
        return await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
    }
}
