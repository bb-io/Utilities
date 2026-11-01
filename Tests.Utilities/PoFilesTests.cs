using Apps.Utilities.Actions;
using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Po;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using System.Text;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class PoFilesTests : TestBase
{
    private const string TestFilesFolder = "CopyPoKeysToTranslations";

    private PoFiles Actions => new(InvocationContext, FileManager);

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

    [TestMethod]
    public async Task UpdateHeader_ReadsContinuedValueAndPreservesFileContent()
    {
        var name = $"PoFilesTests/{Guid.NewGuid()}.po";
        var input = "# Header\r\nmsgid \"\"\r\nmsgstr \"\"\r\n\"Language: f\"\r\n\"r\\n\"\r\n\"X-Other: a:b\\n\"\r\n\r\nmsgid \"Hi\"\r\nmsgstr \"Salut\"";
        var file = CreateInput(name, input, new UTF8Encoding(true));
        try
        {
            var original = await Actions.GetPoHeader(file, new PoHeaderRequest { HeaderName = "language" });
            Assert.AreEqual("fr", original.HeaderValue);
            var result = await Actions.SetPoHeader(file, new SetPoHeaderRequest { HeaderName = "Language", HeaderValue = "a \"quote\" \\ path" });
            Assert.AreEqual(2, result.Headers.Count());
            Assert.AreEqual("a \"quote\" \\ path", result.Headers.First().Value);
            var bytes = File.ReadAllBytes(Path.Combine(GetTestFolderPath(), "Output", name));
            Assert.IsTrue(bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
            var output = Encoding.UTF8.GetString(bytes[3..]);
            StringAssert.Contains(output, "\"Language: a \\\"quote\\\" \\\\ path\\n\"\r\n\"X-Other: a:b\\n\"");
            StringAssert.EndsWith(output, "\r\n\r\nmsgid \"Hi\"\r\nmsgstr \"Salut\"");
            var readBack = await Actions.GetPoHeader(file, new PoHeaderRequest { HeaderName = "language" });
            Assert.AreEqual(result.Headers.First().Value, readBack.HeaderValue);
        }
        finally { Cleanup(name); }
    }

    [TestMethod]
    public async Task AppendHeader_ReturnsAllHeadersAndKeepsTranslation()
    {
        var name = $"PoFilesTests/{Guid.NewGuid()}.po";
        var file = CreateInput(name, "msgid \"\"\nmsgstr \"\"\n\"Language: fr\\n\"\n\nmsgid \"key\"\nmsgstr \"value\"\n", new UTF8Encoding(false));
        try
        {
            var result = await Actions.SetPoHeader(file, new SetPoHeaderRequest { HeaderName = "X-New", HeaderValue = "new" });
            CollectionAssert.AreEqual(new[] { "Language", "X-New" }, result.Headers.Select(x => x.Name).ToArray());
            var output = File.ReadAllText(Path.Combine(GetTestFolderPath(), "Output", name));
            StringAssert.Contains(output, "\"Language: fr\\n\"\n\"X-New: new\\n\"\n\nmsgid \"key\"");
        }
        finally { Cleanup(name); }
    }

    [TestMethod]
    public async Task MissingHeader_ReportsError()
    {
        var name = $"PoFilesTests/{Guid.NewGuid()}.po";
        var file = CreateInput(name, "msgid \"\"\nmsgstr \"\"\n\"Language: fr\\n\"\n", new UTF8Encoding(false));
        try
        {
            await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
                Actions.GetPoHeader(file, new PoHeaderRequest { HeaderName = "Missing" }));
        }
        finally { Cleanup(name); }
    }

    private static FileDto CreateInput(string name, string text, Encoding encoding)
    {
        var path = Path.Combine(GetTestFolderPath(), "Input", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, encoding);
        return new FileDto { File = new FileReference { Name = name, ContentType = "text/x-gettext-translation" } };
    }

    private static void Cleanup(string name)
    {
        File.Delete(Path.Combine(GetTestFolderPath(), "Input", name));
        var outputPath = Path.Combine(GetTestFolderPath(), "Output", name);
        if (File.Exists(outputPath)) File.Delete(outputPath);
    }
}
