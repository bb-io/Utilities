using Apps.Utilities.Actions;
using Apps.Utilities.Models.Csv;
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

        var response = await actions.FilterRows(csvFile, new CsvOptions { HasHeader = false }, 0, "is_full", null);
    }

    [TestMethod]
    public async Task RemoveRows_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.RemoveRows(csvFile, new CsvOptions { HasHeader = false }, new RowIndexesRequest { RowIndexes = new List<int> { 0 } });
    }

    [TestMethod]
    public async Task RemoveColumns_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.RemoveColumns(csvFile, new CsvOptions { HasHeader = false }, new ColumnIndexesRequest{ ColumnIndexes = new List<int> { 3, 4 } });
    }

    [TestMethod]
    public async Task RedefineColumns_works()
    {
        var file = new FileReference { Name = CSV_FILE };
        var csvFile = new CsvFile { File = file };

        var response = await actions.SwapColumns(csvFile, new CsvOptions { HasHeader = false }, new ColumnOrderRequest { ColumnOrder = new List<int> { 3, 3, 3, 3 } });
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

    [TestMethod]
    public async Task RegexChangeValues_works()
    {
        var file = new FileReference { Name = "testReg.csv" };
        var csvFile = new CsvFile { File = file };
        var regexInput = new RegexInput
        {
            Regex = @"\S+",
            From = new[] { "US", "GB", "UA" },
            To = new[] { "United States", "Great Britain", "Ukraine" }
        };


        var response = await actions.ApplyRegexToRow(csvFile, new CsvOptions { HasHeader = false }, 0, regexInput);
    }

    [TestMethod]
    public async Task AddRows_works()
    {
        var file = new FileReference { Name = "test.csv" };
        var csvFile = new CsvFile { File = file };

        var response = await actions.AddRow(csvFile, new CsvOptions { HasHeader = true }, 
            new RowPositionOption { RowPosition=6, InputValues = ["TRW,Nicole Ponkey,Global Purchasing Manager at TRW Automotive,44.197.866.7800,,производитель автозапчастей,29,09,не зацікавлені,Великобритания,Стальная шерсть,,,Интернет,https://www.trwaftermarket.com/ru/,Стальная шерсть,Наташа,"] });

    }



    [TestMethod]
    public async Task RemoveXlsColumns_works()
    {
        var file = new FileReference { Name = "Sample.xlsx" };
        var csvFile = new ExcelFile { File = file };
        var action = new Excel(InvocationContext, FileManager);
        var response = await action.RemoveColumnsByIndexes(
            csvFile, 
            1, 
            new Apps.Utilities.Models.Excel.ColumnIndexesRequest { ColumnIndexes = new List<int> { 1, 4, 5,6 } });

        Assert.IsNotNull(response);
    }
}
