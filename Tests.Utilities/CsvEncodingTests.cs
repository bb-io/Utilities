using Apps.Utilities.Actions;
using Apps.Utilities.Models.Files;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Text;

namespace Tests.Utilities;

[TestClass]
public class CsvEncodingTests
{
    private const string Csv = "Linguist,Hours\r\nRenée Lefèvre,12\r\nZoë Müller,3\r\n";

    private static readonly string[] ExpectedLinguists = ["Linguist", "Renée Lefèvre", "Zoë Müller"];

    [ClassInitialize]
    public static void RegisterCodePages(TestContext _) => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [TestMethod]
    public async Task GetCsvRows_Windows1252WithoutBom_ReadsAccentedCharacters()
    {
        var linguists = await ReadLinguists(Encoding.GetEncoding(1252).GetBytes(Csv));

        CollectionAssert.AreEqual(ExpectedLinguists, linguists);
    }

    [TestMethod]
    public async Task GetCsvRows_Utf8WithoutBom_ReadsAccentedCharacters()
    {
        var linguists = await ReadLinguists(new UTF8Encoding(false).GetBytes(Csv));

        CollectionAssert.AreEqual(ExpectedLinguists, linguists);
    }

    [TestMethod]
    public async Task GetCsvRows_Utf8WithBom_DoesNotKeepBomInFirstCell()
    {
        var utf8WithBom = new UTF8Encoding(true);
        var linguists = await ReadLinguists(utf8WithBom.GetPreamble().Concat(utf8WithBom.GetBytes(Csv)).ToArray());

        CollectionAssert.AreEqual(ExpectedLinguists, linguists);
    }

    [TestMethod]
    public async Task GetCsvRows_Utf16WithBom_ReadsAccentedCharacters()
    {
        var linguists = await ReadLinguists(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(Csv)).ToArray());

        CollectionAssert.AreEqual(ExpectedLinguists, linguists);
    }

    private static async Task<List<string>> ReadLinguists(byte[] csvBytes)
    {
        var actions = new Csv(new InvocationContext(), new InMemoryFileClient(csvBytes));
        var csvFile = new CsvFile { File = new FileReference { Name = "linguists.csv", ContentType = "text/csv" } };

        var response = await actions.GetCsvRows(csvFile, new CsvOptions { HasHeader = false });

        return response.Rows.Select(row => row.Values[0]).ToList();
    }

    private sealed class InMemoryFileClient(byte[] bytes) : IFileManagementClient
    {
        public Task<Stream> DownloadAsync(FileReference reference) => Task.FromResult<Stream>(new MemoryStream(bytes));

        public Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName) =>
            Task.FromResult(new FileReference { Name = fileName, ContentType = contentType });
    }
}
