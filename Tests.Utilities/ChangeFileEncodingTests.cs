using Apps.Utilities.Actions;
using Apps.Utilities.Models.Files;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Text;

namespace Tests.Utilities;

[TestClass]
public class ChangeFileEncodingTests
{
    private const string Text = "Hello, Привіт 😀\r\nSecond line\nLast\r";

    [TestMethod]
    public async Task Utf8BomRoundTrip_PreservesExactBytesAndMetadata()
    {
        var original = Encoding.UTF8.GetBytes(Text);
        var client = new MemoryFileClient(original);
        var actions = new Files(new InvocationContext(), client);
        var result = await actions.ChangeFileEncoding(Request("utf8bom"));
        CollectionAssert.AreEqual(new UTF8Encoding(true).GetPreamble().Concat(original).ToArray(), client.Bytes);
        Assert.AreEqual("sample.txt", result.File.Name);
        Assert.AreEqual("text/plain", result.File.ContentType);

        await actions.ChangeFileEncoding(Request("utf8"));
        CollectionAssert.AreEqual(original, client.Bytes);
    }

    [TestMethod]
    public async Task Utf16WithBom_IsDetectedAndConvertedBack()
    {
        var client = new MemoryFileClient(Encoding.UTF8.GetBytes(Text));
        var actions = new Files(new InvocationContext(), client);
        await actions.ChangeFileEncoding(Request("utf16le"));
        CollectionAssert.AreEqual(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(Text)).ToArray(), client.Bytes);
        await actions.ChangeFileEncoding(Request("utf8"));
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(Text), client.Bytes);
    }

    [TestMethod]
    public async Task ExplicitSource_ReadsUtf16WithoutBom()
    {
        var client = new MemoryFileClient(Encoding.Unicode.GetBytes(Text));
        await new Files(new InvocationContext(), client).ChangeFileEncoding(Request("utf8", "utf16le"));
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(Text), client.Bytes);
    }

    [TestMethod]
    public async Task EmptyFile_AddsOnlyRequestedBom()
    {
        var client = new MemoryFileClient([]);
        var actions = new Files(new InvocationContext(), client);
        await actions.ChangeFileEncoding(Request("utf8bom"));
        CollectionAssert.AreEqual(new UTF8Encoding(true).GetPreamble(), client.Bytes);
        await actions.ChangeFileEncoding(Request("utf8"));
        Assert.AreEqual(0, client.Bytes.Length);
    }

    [TestMethod]
    public async Task InvalidUtf8_ThrowsWithoutUploading()
    {
        var client = new MemoryFileClient([0xC3, 0x28]);
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            new Files(new InvocationContext(), client).ChangeFileEncoding(Request("utf8bom")));
        Assert.AreEqual(0, client.Uploads);
    }

    [TestMethod]
    public async Task ConflictingSourceBom_ThrowsWithoutUploading()
    {
        var client = new MemoryFileClient(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(Text)).ToArray());
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            new Files(new InvocationContext(), client).ChangeFileEncoding(Request("utf8", "utf8")));
        Assert.AreEqual(0, client.Uploads);
    }

    [DataTestMethod]
    [DataRow("unknown", null)]
    [DataRow("", null)]
    [DataRow("utf8", "unknown")]
    public async Task InvalidEncoding_ThrowsBeforeDownloading(string target, string? source)
    {
        var client = new MemoryFileClient([]);
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            new Files(new InvocationContext(), client).ChangeFileEncoding(Request(target, source)));
        Assert.AreEqual(0, client.Downloads);
    }

    private static ChangeFileEncodingRequest Request(string target, string? source = null) => new()
    {
        File = new FileReference { Name = "sample.txt", ContentType = "text/plain" },
        TargetEncoding = target,
        SourceEncoding = source
    };

    private sealed class MemoryFileClient(byte[] bytes) : IFileManagementClient
    {
        public byte[] Bytes { get; private set; } = bytes;
        public int Uploads { get; private set; }
        public int Downloads { get; private set; }

        public Task<Stream> DownloadAsync(FileReference reference)
        {
            Downloads++;
            return Task.FromResult<Stream>(new MemoryStream(Bytes));
        }

        public async Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName)
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            Bytes = buffer.ToArray();
            Uploads++;
            return new FileReference { Name = fileName, ContentType = contentType };
        }
    }
}
