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
    public async Task AddNoteToXliff_Works(string testFileName)
    {
        var request = new AddNoteToXliffRequest
        {
            File = new FileReference() { Name = testFileName },
        };

        var result = await Actions.AddNoteToXliff(request);

        Console.WriteLine(result.File.Name);
    }
}
