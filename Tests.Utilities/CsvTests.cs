using Apps.Utilities.Actions;
using Apps.Utilities.Models.Files;
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

    private const string CSV_FILE = "customers.csv";

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
    public async Task CsvRemoveRows_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.RemoveRows(csvFile, new List<int> { -1, 0, 1 });
    }

    [TestMethod]
    public async Task CsvRedefineColumns_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.SwapColumns(csvFile, new List<int> { 1, 1, 2, 0, 4 });
    }

    [TestMethod]
    public async Task CsvRegex_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.ApplyRegexToColumn(csvFile, 2, new Apps.Utilities.Models.Texts.RegexInput { Regex = "\\((\\d*?)\\)", Group = "1"});
    }
}
