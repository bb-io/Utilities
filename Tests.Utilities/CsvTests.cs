using Apps.Utilities.Actions;
using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class CsvTests : TestBase
{
    private Csv actions;

    private const string CSV_FILE = "test.csv";

    [TestInitialize]
    public void Init()
    {
        var outputDirectory = Path.Combine(GetTestFolderPath(), "Output");
        if (Directory.Exists(outputDirectory))
            Directory.Delete(outputDirectory, true);
        Directory.CreateDirectory(outputDirectory);

        actions = new Csv(InvocationContext, FileManager);
    }

    [TestMethod]
    public async Task FilterRows_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.FilterRows(csvFile, new CsvOptions { HasHeader = false }, 0, "is_full");
    }

    [TestMethod]
    public async Task RemoveRows_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.RemoveRows(csvFile, new CsvOptions { HasHeader = false }, new List<int> { 0 });
    }

    [TestMethod]
    public async Task RemoveColumns_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.RemoveColumns(csvFile, new CsvOptions { HasHeader = false }, new List<int> { 3, 4 });
    }

    [TestMethod]
    public async Task RedefineColumns_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.SwapColumns(csvFile, new CsvOptions { HasHeader = false }, new List<int> { 1, 1, 2, 0, 4 });
    }

    [TestMethod]
    public async Task RegexColumn_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.ApplyRegexToColumn(csvFile, new CsvOptions { HasHeader = false }, 3, new RegexInput { Regex = "\\((\\d*?)\\)", Group = "1"});
    }

    [TestMethod]
    public async Task RegexRow_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.ApplyRegexToRow(csvFile, new CsvOptions { HasHeader = false }, 3, new RegexInput { Regex = "\\((\\d*?)\\)", Group = "1" });
    }
}
