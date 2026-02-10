using Apps.Utilities.Actions;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Files;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class XliffTests: TestBase
{
    public Xliff Actions => new(FileManager);

    [TestMethod]
    [DataRow("test.xliff")]
    [DataRow("contentful_2.xlf")]
    [DataRow("contentful.html.xlf")]
    [DataRow("estimated-contentful.html.xlf")]
    [DataRow("estimated-v22-sample.xlf")]
    [DataRow("estimated-file.xliff")]
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
}
