using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using Apps.Utilities.Models.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Common.Exceptions;
using ClosedXML.Excel;
using Blackbird.Applications.Sdk.Common.Files;
using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using System.Text.RegularExpressions;
using Apps.Utilities.Models.Excel;

namespace Apps.Utilities.Actions;

[ActionList]
public class Excel(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseInvocable(invocationContext)
{
    [Action("Redefine spreadsheet columns", Description = "Rearrange the columns of a spreadsheet file according to the specified order.")]
    public async Task<FileReference> ReorderColumns(
    [ActionParameter] ExcelFile File,
    [ActionParameter][Display("Sheet number")] int worksheetIndex,
    [ActionParameter] ExcelColumnOrderRequest columnOrder)
    {
        if (columnOrder.ColumnOrder.Any(x => x < 1))
            throw new PluginApplicationException("A column identifier must be a positive number.");

        var (workbook, worksheet) = await ReadExcel(File.File, worksheetIndex);
        var originalSheetName = worksheet.Name;

        var usedRange = worksheet.RangeUsed();
        var rowCount = usedRange.RowCount();
        var tempSheet = workbook.AddWorksheet("Temp");

        var newOrder = columnOrder.ColumnOrder.ToList();

        for (int i = 0; i < newOrder.Count; i++)
        {
            int sourceCol = newOrder[i];
            for (int row = 1; row <= rowCount; row++)
            {
                var sourceCell = worksheet.Cell(row, sourceCol);
                var destCell = tempSheet.Cell(row, i + 1);
                destCell.CopyFrom(sourceCell);
            }

            tempSheet.Column(i + 1).Width = worksheet.Column(sourceCol).Width;
        }

        workbook.Worksheets.Delete(worksheet.Name);
        tempSheet.Name = originalSheetName;

        return await WriteExcel(workbook, File.File.Name);
    }


    [Action("Remove spreadsheet rows by indexes", Description = "Remove the selected rows from a spreadsheet file.")]
    public async Task<FileReference> RemoveRowsByIndexes(
        [ActionParameter] ExcelFile File,
        [ActionParameter][Display("Sheet number")] int worksheetIndex,
        [ActionParameter] Models.Excel.RowIndexesRequest rowIndexesRequest)
    {
        var (workbook, worksheet) = await ReadExcel(File.File, worksheetIndex);
        foreach (var rowIndex in rowIndexesRequest.RowIndexes.OrderByDescending(i => i))
        {
            worksheet.Row(rowIndex).Delete();
        }
        return await WriteExcel(workbook, File.File.Name);
    }

    [Action("Remove spreadsheet columns by indexes", Description = "Remove the selected columns from a spreadsheet file.")]
    public async Task<FileReference> RemoveColumnsByIndexes(
        [ActionParameter] ExcelFile File,
        [ActionParameter][Display("Sheet number")] int worksheetIndex,
        [ActionParameter] Models.Excel.ColumnIndexesRequest columnIndexesRequest)
    {
        var (workbook, worksheet) = await ReadExcel(File.File, worksheetIndex);
        foreach (var colIndex in columnIndexesRequest.ColumnIndexes.OrderByDescending(i => i))
        {
            worksheet.Column(colIndex).Delete();
        }

        return await WriteExcel(workbook, File.File.Name);
    }

    [Action("Remove spreadsheet rows by condition", Description = "Remove the rows that meet the condition in the specfied column index.")]
    public async Task<FileReference> RemoveRowsByCondition(
        [ActionParameter] ExcelFile File,
        [ActionParameter][Display("Sheet number")] int worksheetIndex, 
        [ActionParameter][Display("Column letter")] string columnIndex,
        [ActionParameter][Display("Condition", Description = "The condition that is applied to the column")][StaticDataSource(typeof(CsvColumnCondition))] string condition
        )
    {
        var (workbook, worksheet) = await ReadExcel(File.File, worksheetIndex);
        var usedRange = worksheet.RangeUsed();
        var rows = usedRange.RowsUsed().ToList();

        foreach (var row in rows)
        {
            string value = "";
            try {
                value = row.Cell(columnIndex)?.Value.GetText();
            } catch { }
            if (condition == "is_empty" && string.IsNullOrEmpty(value))
            {
                row.Delete();
            }
            else if (condition == "is_full" && !string.IsNullOrEmpty(value))
            {
                row.Delete();
            }
        }

        return await WriteExcel(workbook, File.File.Name);
    }

    [Action("Replace using Regex in a spreadsheet row", Description = "Apply a regular expression and replace pattern to a row in a spreadsheet")]
    public async Task<FileReference> ApplyRegexToRow(
    [ActionParameter]ExcelFile File,
    [ActionParameter][Display("Sheet number")] int worksheetIndex,
    [ActionParameter][Display("Row index", Description = "The first row starts with 1")] int rowIndex,
    [ActionParameter][Display("Regular expression")] string pattern,
    [ActionParameter][Display("Replacement pattern")] string replacement)
    {
        var (workbook, worksheet) = await ReadExcel(File.File, worksheetIndex);

        var regex = new Regex(pattern);
        var row = worksheet.Row(rowIndex);

        foreach (var cell in row.CellsUsed())
        {
            if (cell.DataType == XLDataType.Text || cell.DataType == XLDataType.Number)
            {
                var originalValue = cell.GetString();
                var newValue = regex.Replace(originalValue, replacement);
                cell.Value = newValue;
            }
        }

        return await WriteExcel(workbook, File.File.Name);
    }

    [Action("Replace using Regex in a spreadsheet column", Description = "Apply a regular expression and replace patternt to a spreadsheet column")]
    public async Task<FileReference> ApplyRegexToColumn(
    [ActionParameter]ExcelFile File,
    [ActionParameter][Display("Sheet number")] int worksheetIndex,
    [ActionParameter][Display("Column index", Description = "The first column starts with 1")] int columnIndex,
    [ActionParameter][Display("Regular expression")] string pattern,
    [ActionParameter][Display("Replacement pattern")] string replacement)
    {
        var (workbook, worksheet) = await ReadExcel(File.File, worksheetIndex);

        var regex = new Regex(pattern);
        var column = worksheet.Column(columnIndex);

        foreach (var cell in column.CellsUsed())
        {
            if (cell.DataType == XLDataType.Text || cell.DataType == XLDataType.Number)
            {
                var originalValue = cell.GetString();
                var newValue = regex.Replace(originalValue, replacement);
                cell.Value = newValue;
            }
        }

        return await WriteExcel(workbook, File.File.Name);
    }

    [Action("Insert row to a spreadsheet", Description = "Inserts a new row at the given index in a spreadsheet")]
    public async Task<FileReference> InsertRowAtIndex(
    [ActionParameter] ExcelFile File,
    [ActionParameter][Display("Sheet number")] int worksheetIndex,
    [ActionParameter][Display("Row index", Description = "The first row starts with 1")] int rowIndex,
    [ActionParameter] CellValuesRequest cellValuesRequest)
    {
        var (workbook, worksheet) = await ReadExcel(File.File, worksheetIndex);
        worksheet.Row(rowIndex).InsertRowsAbove(1);

        int columnIndex = 1;
        foreach (var value in cellValuesRequest.CellValues)
        {
            worksheet.Cell(rowIndex, columnIndex).SetValue(value);
            columnIndex++;
        }

        return await WriteExcel(workbook, File.File.Name);
    }

    [Action("Insert empty rows to a spreadsheet", Description = "Inserts new empty rows at the given indexes in a worksheet")]
    public async Task<FileReference> InsertEmptyRowAtIndex(
    [ActionParameter] ExcelFile File,
    [ActionParameter][Display("Sheet number")] int worksheetIndex,
    [ActionParameter] Models.Excel.RowIndexesRequest rowIndexesRequest)
    {
        var (workbook, worksheet) = await ReadExcel(File.File, worksheetIndex);
        
        foreach (var rowIndex in rowIndexesRequest.RowIndexes)
        {
            worksheet.Row(rowIndex).InsertRowsAbove(1);
        }

        return await WriteExcel(workbook, File.File.Name);
    }

    private async Task<(XLWorkbook Workbook, IXLWorksheet Worksheet)> ReadExcel(FileReference file, int worksheetIndex)
    {
        var stream = new MemoryStream();
        var downloaded = await fileManagementClient.DownloadAsync(file);
        await downloaded.CopyToAsync(stream);
        stream.Position = 0;

        var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(worksheetIndex);
        return (workbook, worksheet);
    }

    private async Task<FileReference> WriteExcel(XLWorkbook workbook, string originalFileName)
    {
        var streamOut = new MemoryStream();
        workbook.SaveAs(streamOut);
        streamOut.Position = 0;

        var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var file = await fileManagementClient.UploadAsync(streamOut, mimeType, originalFileName);
        return file;
    }
}