using Apps.Utilities.Actions;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Files;
using System.Xml.Linq;
using Apps.Utilities.Models.Files;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class XliffTests: TestBase
{
    private Xliff Actions => new(FileManager);

    [TestMethod]
    [DataRow("example.mxliff")]
    [DataRow("test.xliff")]
    public async Task AddNoteToXliff_Works(string testFileName)
    {
        var request = new AddNoteToXliffRequest
        {
            File = new FileReference() { Name = testFileName },
        };

        var result = await Actions.AddNoteToXliff(request);

        Console.WriteLine(result.File.Name);
    }

    [TestMethod]
    [DataRow("test.xliff")]
    [DataRow("contentful_2.xlf")]
    [DataRow("contentful.html.xlf")]
    [DataRow("estimated-contentful.html.xlf")]
    [DataRow("estimated-v22-sample.xlf")]
    [DataRow("estimated-file.xliff")]
    public async Task CopySourceToTarget_Works(string testFileName)
    {
        var request = new CopySourceToTargetRequest
        {
            File = new FileReference() { Name = testFileName },
        };

        var result = await Actions.CopySourceToTarget(request);

        Console.WriteLine(result.File.Name);
    }

    [TestMethod]
    [DataRow("cyrillic.xliff")]
    public async Task ConvertXliffToCsv_IsSuccess(string testFileName)
    {
        // Arrange
        var request = new ConvertXliffToCsvRequest
        {
            File = new FileReference { Name = testFileName },
            BatchSize = 10
        };

        // Act
        var result = await Actions.ConvertXliffToCsv(request);

        // Assert
        foreach (var file in result.Files)
            Console.WriteLine(file.Name);
    }

    [TestMethod]
    public async Task MoveXliffContentToNotes_MovesAttributeToNote_InXliff12()
    {
        const string fileName = "move-note-1.2.xliff";
        DeleteOutputFile(fileName);

        var request = new MoveXliffContentToNotesRequest
        {
            File = new FileReference { Name = fileName },
            XPath = "//ns:target",
            Attribute = "custom:attribute"
        };

        var result = await Actions.MoveXliffContentToNotes(request);

        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task MoveXliffContentToNotes_MovesAttributeToUnitNotes_InXliff22()
    {
        const string fileName = "move-note-2.2.xlf";
        DeleteOutputFile(fileName);

        var request = new MoveXliffContentToNotesRequest
        {
            File = new FileReference { Name = fileName },
            XPath = "//ns:target",
            Attribute = "custom:attribute"
        };

        var result = await Actions.MoveXliffContentToNotes(request);

        await using var stream = await FileManager.DownloadAsync(result.File);
        var doc = XDocument.Load(stream);
        XNamespace ns = doc.Root!.GetDefaultNamespace();

        var unit = doc.Descendants(ns + "unit").First();
        var notes = unit.Element(ns + "notes");
        var note = notes?.Element(ns + "note");
        var target = unit.Descendants(ns + "target").First();

        Assert.IsNotNull(notes);
        Assert.IsNotNull(note);
        Assert.AreEqual("custom:attribute=\"surf hint\"", note.Value);
        Assert.IsNull(target.Attributes().FirstOrDefault(a => a.Name.LocalName == "attribute"));
    }

    [TestMethod]
    public async Task ConfirmAndLockFinalTargets_WithDocxFile_ThrowsMisconfigException()
    {
        // Arrange
        var input = new ConvertTextToDocumentResponse
        {
            File = new FileReference { Name = "test.docx" }
        };

        // Act
        var ex = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(
            () => Actions.ConfirmAndLockFinalTargets(input));

        // Assert
        StringAssert.Contains(ex.Message, "Failed to parse the file as XLIFF");
    }

    [TestMethod]
    [DataRow("notes.mxliff")]
    [DataRow("example.mxliff")]
    public async Task ExtractXliffNotes_ReturnsXliffNotes(string fileName)
    {
        // Arrange
        var actions = new Xliff(FileManager);
        var input = new FileDto { File = new FileReference { Name = fileName } };

        // Act
        var result = await actions.ExtractXliffNotes(input);

        // Assert
        for (int i = 0; i < result.SegmentIds.Count; i++)
        {
            Console.WriteLine(result.SegmentIds[i]);
            Console.WriteLine(result.SegmentNotes[i]);
        }
        
        Assert.IsNotNull(result);
    }
    
    private static void DeleteOutputFile(string fileName)
    {
        var path = Path.Combine(GetTestFolderPath(), "Output", fileName);
        if (File.Exists(path))
            File.Delete(path);
    }
}
