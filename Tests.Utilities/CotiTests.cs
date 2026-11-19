using Apps.Utilities.Actions;
using Apps.Utilities.Models.Coti;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class CotiTests : TestBase
{
    private Coti _coti = null!;
    private string _outputDirectory = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _outputDirectory = Path.Combine(GetTestFolderPath(), "Output");
        if (Directory.Exists(_outputDirectory))
            Directory.Delete(_outputDirectory, true);
        Directory.CreateDirectory(_outputDirectory);
        _coti = new Coti(InvocationContext, FileManager);
    }

    [TestMethod]
    public async Task ParseCotiFile_ReturnsLanguagesTranslationFilesAndPackagedReferences()
    {
        var package = CreatePackage(
            new ProjectFixture("project-en", "project-id-en", "en-GB", ["content.xml"], true),
            new ProjectFixture("project-es", "project-id-es", "es-ES", ["content.xml"], false));

        var response = await _coti.ParseCotiFile(new CotiFileRequest { File = package });

        CollectionAssert.AreEqual(new[] { "de-DE" }, response.SourceLanguages.ToArray());
        CollectionAssert.AreEqual(new[] { "en-GB", "es-ES" }, response.TargetLanguages.ToArray());
        Assert.AreEqual(2, response.TranslationFiles.Count());
        Assert.AreEqual(1, response.ReferenceFiles.Count());
        Assert.AreEqual(2, response.TranslationFileArchivePaths.Count());
        Assert.AreEqual(1, response.ReferenceFileArchivePaths.Count());

        var translationDetails = response.TranslationFileDetails.ToList();
        var englishIndex = translationDetails.FindIndex(file => file.TargetLanguage == "en-GB");
        var englishFile = translationDetails[englishIndex];
        Assert.AreEqual("project-en/translation files/content.xml", response.TranslationFileArchivePaths.ElementAt(englishIndex));
        Assert.AreEqual("project-id-en", englishFile.ProjectId);

        var reference = response.ReferenceFileDetails.Single();
        Assert.AreEqual("project-en/reference files/instructions.pdf", response.ReferenceFileArchivePaths.Single());
        Assert.AreEqual("reference files\\instructions.pdf", reference.ManifestFileReference);
        Assert.AreEqual("no", reference.External);
        Assert.AreEqual("application/pdf", reference.MimeType);
        Assert.AreEqual("other", reference.Type);
        Assert.AreEqual("Customer instructions", reference.Description);
    }

    [TestMethod]
    public async Task AssembleCotiFile_IncludesOnlySuppliedCompleteProjectsAndPreservesManifest()
    {
        var package = CreatePackage(
            new ProjectFixture("project-en", "project-id-en", "en-GB", ["content.xml"], true),
            new ProjectFixture("project-es", "project-id-es", "es-ES", ["content.xml"], true));
        var parsed = await _coti.ParseCotiFile(new CotiFileRequest { File = package });
        var spanishIndex = parsed.TranslationFileDetails.ToList().FindIndex(file => file.TargetLanguage == "es-ES");
        var spanishFile = parsed.TranslationFiles.ElementAt(spanishIndex);
        var spanishPath = parsed.TranslationFileArchivePaths.ElementAt(spanishIndex);
        await File.WriteAllTextAsync(
            Path.Combine(_outputDirectory, spanishFile.Name),
            "<translated>Spanish translation</translated>");

        var assembled = await _coti.AssembleCotiFile(new AssembleCotiRequest
        {
            OriginalCotiFile = package,
            TranslatedFiles = [spanishFile],
            OriginalArchivePaths = [spanishPath]
        });

        using var archive = ZipFile.OpenRead(Path.Combine(_outputDirectory, assembled.File.Name));
        Assert.IsTrue(archive.Entries.All(entry => entry.FullName.StartsWith("project-es/")));
        Assert.IsFalse(archive.Entries.Any(entry => entry.FullName.StartsWith("project-en/")));

        var translationEntry = archive.GetEntry("project-es/translation files/content.xml");
        Assert.IsNotNull(translationEntry);
        using (var reader = new StreamReader(translationEntry.Open()))
            Assert.AreEqual("<translated>Spanish translation</translated>", await reader.ReadToEndAsync());

        var manifestEntry = archive.GetEntry("project-es/COTI.xml");
        Assert.IsNotNull(manifestEntry);
        using var manifestStream = manifestEntry.Open();
        var manifest = XDocument.Load(manifestStream);
        Assert.AreEqual("none", manifest.Descendants().Single(element => element.Name.LocalName == "translation").Attribute("status")?.Value);
        Assert.IsNotNull(archive.GetEntry("project-es/reference files/instructions.pdf"));
    }

    [TestMethod]
    public async Task ParseOutputs_MapDirectlyToAssembleInputsForFullRoundTrip()
    {
        var package = CreatePackage(
            new ProjectFixture("project-en", "project-id-en", "en-GB", ["content.xml"], true),
            new ProjectFixture("project-es", "project-id-es", "es-ES", ["content.xml"], true));
        var parsed = await _coti.ParseCotiFile(new CotiFileRequest { File = package });

        var assembled = await _coti.AssembleCotiFile(new AssembleCotiRequest
        {
            OriginalCotiFile = package,
            TranslatedFiles = parsed.TranslationFiles,
            OriginalArchivePaths = parsed.TranslationFileArchivePaths
        });

        using var archive = ZipFile.OpenRead(Path.Combine(_outputDirectory, assembled.File.Name));
        Assert.IsNotNull(archive.GetEntry("project-en/COTI.xml"));
        Assert.IsNotNull(archive.GetEntry("project-es/COTI.xml"));
        Assert.AreEqual(6, archive.Entries.Count);
    }

    [TestMethod]
    public async Task AssembleCotiFile_RejectsIncompleteProject()
    {
        var package = CreatePackage(
            new ProjectFixture("project-en", "project-id-en", "en-GB", ["first.xml", "second.xml"], false));
        var parsed = await _coti.ParseCotiFile(new CotiFileRequest { File = package });

        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            _coti.AssembleCotiFile(new AssembleCotiRequest
            {
                OriginalCotiFile = package,
                TranslatedFiles = [parsed.TranslationFiles.First()],
                OriginalArchivePaths = [parsed.TranslationFileArchivePaths.First()]
            }));

        StringAssert.Contains(exception.Message, "is incomplete");
        StringAssert.Contains(exception.Message, "second.xml");
    }

    [TestMethod]
    public async Task AssembleCotiFile_RejectsUnknownArchivePath()
    {
        var package = CreatePackage(
            new ProjectFixture("project-en", "project-id-en", "en-GB", ["content.xml"], false));
        var parsed = await _coti.ParseCotiFile(new CotiFileRequest { File = package });
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            _coti.AssembleCotiFile(new AssembleCotiRequest
            {
                OriginalCotiFile = package,
                TranslatedFiles = [parsed.TranslationFiles.Single()],
                OriginalArchivePaths = ["another-project/translation files/content.xml"]
            }));

        StringAssert.Contains(exception.Message, "is not declared");
    }

    [TestMethod]
    public async Task AssembleCotiFile_RejectsMismatchedFileAndPathCounts()
    {
        var package = CreatePackage(
            new ProjectFixture("project-en", "project-id-en", "en-GB", ["content.xml"], false));
        var parsed = await _coti.ParseCotiFile(new CotiFileRequest { File = package });

        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            _coti.AssembleCotiFile(new AssembleCotiRequest
            {
                OriginalCotiFile = package,
                TranslatedFiles = [parsed.TranslationFiles.Single()],
                OriginalArchivePaths = []
            }));

        StringAssert.Contains(exception.Message, "one original archive path for every translated file");
    }

    private FileReference CreatePackage(params ProjectFixture[] projects)
    {
        const string fileName = "sample.coti";
        var path = Path.Combine(_outputDirectory, fileName);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var project in projects)
            AddProject(archive, project);
        return new FileReference { Name = fileName, ContentType = "application/zip" };
    }

    private static void AddProject(ZipArchive archive, ProjectFixture fixture)
    {
        XNamespace ns = "http://www.dercom.de/COTI/2013/1.0/namespace";
        var translation = new XElement(ns + "translation",
            new XAttribute("name", fixture.ProjectId),
            new XAttribute("source-language", "de-DE"),
            new XAttribute("target-language", fixture.TargetLanguage),
            new XAttribute("status", "none"));

        foreach (var fileName in fixture.TranslationFiles)
        {
            translation.Add(new XElement(ns + "translation-file",
                new XAttribute("file-ref", $"translation files\\{fileName}"),
                new XAttribute("mime-type", "application/xml"),
                new XAttribute("encoding", "UTF-8")));
            AddEntry(archive, $"{fixture.Root}/translation files/{fileName}", $"<source>{fixture.TargetLanguage}</source>");
        }

        if (fixture.IncludeReference)
        {
            translation.Add(new XElement(ns + "reference-file",
                new XAttribute("external", "no"),
                new XAttribute("file-ref", "reference files\\instructions.pdf"),
                new XAttribute("mime-type", "application/pdf"),
                new XAttribute("type", "other"),
                new XAttribute("description", "Customer instructions")));
            translation.Add(new XElement(ns + "reference-file",
                new XAttribute("external", "yes"),
                new XAttribute("file-ref", "https://example.com/external.pdf"),
                new XAttribute("mime-type", "application/pdf"),
                new XAttribute("type", "other")));
            AddEntry(archive, $"{fixture.Root}/reference files/instructions.pdf", "reference");
        }

        var manifest = new XDocument(
            new XElement(ns + "coti",
                new XAttribute("version", "1.0"),
                new XAttribute("level", "2"),
                new XAttribute("creator", "Utilities tests"),
                new XAttribute("creation-date", "2026-01-01T00:00:00Z"),
                new XElement(ns + "project",
                    new XAttribute("name", fixture.ProjectId),
                    new XAttribute("project-id", fixture.ProjectId),
                    new XAttribute("checkout-date", "2026-01-01T00:00:00Z"),
                    new XAttribute("proposal", "no"),
                    translation)));

        var manifestEntry = archive.CreateEntry($"{fixture.Root}/COTI.xml");
        using var manifestStream = manifestEntry.Open();
        manifest.Save(manifestStream);
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private sealed record ProjectFixture(
        string Root,
        string ProjectId,
        string TargetLanguage,
        string[] TranslationFiles,
        bool IncludeReference);
}
