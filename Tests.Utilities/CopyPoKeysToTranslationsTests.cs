using Apps.Utilities.Actions;
using Apps.Utilities.Models.Files;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using System.Text;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class CopyPoKeysToTranslationsTests : TestBase
{
    private const string TestFilesFolder = "CopyPoKeysToTranslations";

    private Files Actions => new(InvocationContext, FileManager);

    [TestInitialize]
    public void Init()
    {
        var outputDirectory = Path.Combine(GetTestFolderPath(), "Output");
        if (Directory.Exists(outputDirectory))
            Directory.Delete(outputDirectory, true);
        Directory.CreateDirectory(outputDirectory);
    }

    [TestMethod]
    public async Task SingularEntries_CopyKeysAndPreserveAllOtherText()
    {
        const string fileName = "complex.po";

        var result = await Actions.CopyPoKeysToTranslations(new FileDto
        {
            File = new FileReference
            {
                Name = Path.Combine(TestFilesFolder, fileName),
                ContentType = "application/custom-po",
            },
        });

        Assert.AreEqual(Path.Combine(TestFilesFolder, fileName), result.File.Name);
        Assert.AreEqual("application/custom-po", result.File.ContentType);
        CollectionAssert.AreEqual(
            File.ReadAllBytes(Path.Combine(
                GetTestFolderPath(),
                "Input",
                TestFilesFolder,
                "complex-expected.po")),
            await LoadOutputBytes(result.File));
    }

    [TestMethod]
    public async Task PluralEntries_MapSingularAndPluralSourceForms()
    {
        const string fileName = "plural.po";

        var result = await Actions.CopyPoKeysToTranslations(new FileDto
        {
            File = new FileReference
            {
                Name = Path.Combine(TestFilesFolder, fileName),
                ContentType = "text/x-gettext-translation",
            },
        });

        CollectionAssert.AreEqual(
            File.ReadAllBytes(Path.Combine(
                GetTestFolderPath(),
                "Input",
                TestFilesFolder,
                "plural-expected.po")),
            await LoadOutputBytes(result.File));
    }

    [TestMethod]
    public async Task HeaderOnlyFile_RemainsUnchangedAndUsesDefaultContentType()
    {
        const string fileName = "header-only.po";
        var inputPath = Path.Combine(GetTestFolderPath(), "Input", TestFilesFolder, fileName);

        var result = await Actions.CopyPoKeysToTranslations(new FileDto
        {
            File = new FileReference
            {
                Name = Path.Combine(TestFilesFolder, fileName),
            },
        });

        Assert.AreEqual("text/x-gettext-translation", result.File.ContentType);
        CollectionAssert.AreEqual(
            File.ReadAllBytes(inputPath),
            await LoadOutputBytes(result.File));
    }

    [TestMethod]
    public async Task CrLfAndMissingTrailingNewline_ArePreserved()
    {
        const string fileName = "bom-crlf.po";
        var relativePath = Path.Combine(TestFilesFolder, fileName);
        var inputPath = Path.Combine(GetTestFolderPath(), "Input", relativePath);
        var inputText =
            "msgid \"\"\r\n" +
            "msgstr \"\"\r\n" +
            "\"Project-Id-Version: encoding-test\\n\"\r\n" +
            "\r\n" +
            "msgid \"Café\"\r\n" +
            "msgstr \"Ancien\"";
        var expectedText =
            "msgid \"\"\r\n" +
            "msgstr \"\"\r\n" +
            "\"Project-Id-Version: encoding-test\\n\"\r\n" +
            "\r\n" +
            "msgid \"Café\"\r\n" +
            "msgstr \"Café\"";
        var inputBytes = Encoding.UTF8.GetBytes(inputText);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedText);

        await File.WriteAllBytesAsync(inputPath, inputBytes);
        try
        {
            var result = await Actions.CopyPoKeysToTranslations(new FileDto
            {
                File = new FileReference
                {
                    Name = relativePath,
                    ContentType = "text/x-gettext-translation",
                },
            });

            CollectionAssert.AreEqual(expectedBytes, await LoadOutputBytes(result.File));
        }
        finally
        {
            File.Delete(inputPath);
        }
    }

    [TestMethod]
    public async Task NonPoExtension_ThrowsBeforeDownloading()
    {
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            Actions.CopyPoKeysToTranslations(new FileDto
            {
                File = new FileReference
                {
                    Name = Path.Combine(TestFilesFolder, "unused.txt"),
                },
            }));

        StringAssert.Contains(exception.Message, "must be a PO file");
    }

    [TestMethod]
    public async Task MissingTranslation_ThrowsReadableError()
    {
        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            Actions.CopyPoKeysToTranslations(new FileDto
            {
                File = new FileReference
                {
                    Name = Path.Combine(TestFilesFolder, "malformed.po"),
                    ContentType = "text/x-gettext-translation",
                },
            }));

        StringAssert.Contains(exception.Message, "must contain one msgstr");
    }

    private async Task<byte[]> LoadOutputBytes(FileReference file)
    {
        await using var stream = await FileManager.DownloadAsync(file);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
