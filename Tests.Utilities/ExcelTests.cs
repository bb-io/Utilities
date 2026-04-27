using Apps.Utilities.Actions;
using Apps.Utilities.Models.Excel;
using Apps.Utilities.Models.Files;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class ExcelTests : TestBase
{
    private Excel Actions => new (InvocationContext, FileManager);
    
    [TestMethod]
    public async Task RemoveXlsColumns_works()
    {
        var file = new FileReference { Name = "Sample.xlsx" };
        var csvFile = new ExcelFile { File = file };
        var response = await Actions.RemoveColumnsByIndexes(
            csvFile, 
            1, 
            new Apps.Utilities.Models.Excel.ColumnIndexesRequest { ColumnIndexes = new List<int> { 1, 4, 5,6 } });

        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task GroupRowsByColumn_CsvFileInput_ThrowsMisconfigException()
    {
        // Arrange
        var excelFile = new ExcelFile { File = new FileReference { Name = "testReg.csv" } };
        var groupInput = new ExcelGroupingRequest
        {
            ColumnIndex = 1,
            SkipHeader = false,
            WorksheetIndex = 1
        };

        // Act
        var ex = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            Actions.GroupRowsByColumn(excelFile, groupInput));
        
        // Assert
        Console.WriteLine(ex.Message);
    }

    [TestMethod]
    public async Task GroupRowsByColumn_XlsxFileInput_ReturnsGroupedRows()
    {
        // Arrange
        var excelFile = new ExcelFile { File = new FileReference { Name = "testReg.xlsx" } };
        var groupInput = new ExcelGroupingRequest
        {
            ColumnIndex = 1,
            SkipHeader = false,
            WorksheetIndex = 1
        };

        // Act
        var result = await Actions.GroupRowsByColumn(excelFile, groupInput);
        
        // Assert
        PrintJsonResult(result);
        Assert.IsNotNull(result);
    }
}