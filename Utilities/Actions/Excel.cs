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
using Apps.Utilities.ErrorWrapper;

namespace Apps.Utilities.Actions;

[ActionList("Excel spreadsheets")]
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

    [Action("Remove spreadsheet rows by condition",
    Description = "Remove the rows that meet the condition in the specified column."
)]
    public async Task<FileReference> RemoveRowsByCondition(
    [ActionParameter] ExcelFile File,
    [ActionParameter][Display("Sheet number")] int worksheetIndex,
    [ActionParameter][Display("Column letter")] string columnLetter,
    [ActionParameter]
    [Display("Condition", Description = "The condition that is applied to the column")]
    [StaticDataSource(typeof(CsvColumnCondition))] string condition,
    [ActionParameter]
    [Display("Value to compare")]
    string? targetValue)
    {
        var (workbook, worksheet) = await ReadExcel(File.File, worksheetIndex);
        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
            return await WriteExcel(workbook, File.File.Name);

        var rows = usedRange.RowsUsed().ToList();

        foreach (var row in rows)
        {
            string value = "";
            try
            {
                value = row.Cell(columnLetter)?.Value.GetText();
            }
            catch
            {}

            switch (condition)
            {
                case "is_empty":
                    if (string.IsNullOrEmpty(value))
                        row.Delete();
                    break;

                case "is_full":
                    if (!string.IsNullOrEmpty(value))
                        row.Delete();
                    break;

                case "value_equals":
                    if (string.IsNullOrEmpty(targetValue))
                        throw new PluginMisconfigurationException(
                            "Value to compare needs to be filled in when using Value equals or contains conditions");

                    if (value == targetValue)
                        row.Delete();
                    break;

                case "value_contains":
                    if (string.IsNullOrEmpty(targetValue))
                        throw new PluginMisconfigurationException(
                            "Value to compare needs to be filled in when using Value equals or contains conditions");

                    if (value.Contains(targetValue))
                        row.Delete();
                    break;

                case "value_does_not_equal":
                    if (string.IsNullOrEmpty(targetValue))
                        throw new PluginMisconfigurationException(
                            "Value to compare needs to be filled in when using Value equals or contains conditions");

                    if (value != targetValue)
                        row.Delete();
                    break;

                default:
                    throw new PluginApplicationException($"Unsupported condition: {condition}");
            }
        }

        return await WriteExcel(workbook, File.File.Name);
    }

    [Action("Get spreadsheet row indexes by condition", Description = "Returns the indexes of rows meeting the specified condition")]
    public async Task<List<int>> GetRowIndexesByCondition(
       [ActionParameter] ExcelFile File,
       [ActionParameter][Display("Sheet number")] int worksheetIndex,
       [ActionParameter][Display("Column letter")] string columnIndex,
       [ActionParameter][Display("Condition", Description = "The condition that is applied to the column")][StaticDataSource(typeof(CsvColumnCondition))] string condition
       )
    {
        var (workbook, worksheet) = await ReadExcel(File.File, worksheetIndex);
        var usedRange = worksheet.RangeUsed();
        var rows = usedRange.RowsUsed().ToList();
        var rowIndexes = new List<int>();

        foreach (var row in rows)
        {
            string value = "";
            try
            {
                value = row.Cell(columnIndex)?.Value.GetText();
            }
            catch { }
            if (condition == "is_empty" && string.IsNullOrEmpty(value))
            {
                rowIndexes.Add(row.RowNumber());
            }
            else if (condition == "is_full" && !string.IsNullOrEmpty(value))
            {
                rowIndexes.Add(row.RowNumber());
            }
        }

        return rowIndexes;
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

        Regex regex;
        try
        {
            regex = new Regex(pattern);
        }
        catch (RegexParseException ex)
        {
            throw new PluginMisconfigurationException($"Invalid regular expression pattern: {ex.Message}");
        }

        var row = worksheet.Row(rowIndex);

        foreach (var cell in row.CellsUsed())
        {
            if (cell.DataType == XLDataType.Text || cell.DataType == XLDataType.Number)
            {
                var originalValue = cell.GetString();
                try
                {
                    var newValue = regex.Replace(originalValue, replacement);
                    cell.Value = newValue;
                }
                catch (ArgumentException ex)
                {
                    throw new PluginMisconfigurationException($"Invalid replacement pattern: {ex.Message}");
                }
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

        if (columnIndex < 1 || columnIndex > worksheet.LastColumnUsed().ColumnNumber())
        {
            throw new PluginMisconfigurationException($"Invalid column index: {columnIndex}. Please check your input and try again");
        }

        var regex = ErrorWrapperExecute.ExecuteSafely(() =>
        {
            return new Regex(pattern);
        }, ex => throw new PluginMisconfigurationException($"Invalid regular expression pattern: {ex.Message}"));
        var column = worksheet.Column(columnIndex);

        foreach (var cell in column.CellsUsed())
        {
            if (cell.DataType == XLDataType.Text || cell.DataType == XLDataType.Number)
            {
                var originalValue = cell.GetString();
                try
                {
                    var newValue = regex.Replace(originalValue, replacement);
                    cell.Value = newValue;
                }
                catch (ArgumentException ex)
                {
                    throw new PluginMisconfigurationException($"Invalid replacement pattern: {ex.Message}");
                }
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

    [Action("Group rows by column value in spreadsheet",
    Description = "Reads a spreadsheet and groups its rows based on a specific column's value")]
    public async Task<List<GroupedRows>> GroupRowsByColumn(
    [ActionParameter] ExcelFile file,
    [ActionParameter] ExcelGroupingRequest request)
    {
        var (workbook, worksheet) = await ReadExcel(file.File, request.WorksheetIndex);

        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
            throw new PluginMisconfigurationException("The worksheet is empty.");

        var allRows = usedRange
            .RowsUsed()
            .Select(r => r.Cells().Select(c => c.GetString()).ToList())
            .ToList();

        if (!allRows.Any())
            throw new PluginMisconfigurationException("No rows found in the worksheet.");

        var rows = request.SkipHeader ? allRows.Skip(1).ToList() : allRows;

        if (!rows.Any())
            throw new PluginMisconfigurationException("No data rows available after skipping header.");

        var columnCount = rows.First().Count;
        if (request.ColumnIndex < 1 || request.ColumnIndex > columnCount)
            throw new PluginMisconfigurationException(
                $"Invalid column index {request.ColumnIndex}. The sheet contains {columnCount} columns.");

        int colIndex = request.ColumnIndex - 1;

        var grouped = rows
     .GroupBy(row => row[colIndex] ?? string.Empty)
     .Select(g => new GroupedRows
     {
         Key = g.Key,
         Rows = g.Select(r => new Row
         {
             Cells = r
         }).ToList()
     })
     .ToList();

        return grouped;
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

    [Action("Get column values from spreadsheet", Description = "Returns all values from a specific column in a spreadsheet.")]
    public async Task<IEnumerable<string>> GetColumnValues(
   [ActionParameter] ExcelFile File,
   [ActionParameter][Display("Sheet number")] int worksheetIndex,
   [ActionParameter][Display("Column letter")] string columnLetter,
   [ActionParameter][Display("Skip first row")] bool? skipFirstRow = false,
   [ActionParameter][Display("Distinct values only")] bool? distinctOnly = false)
    {
        var (_, worksheet) = await ReadExcel(File.File, worksheetIndex);

        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
            return Enumerable.Empty<string>();

        var rows = usedRange.RowsUsed().ToList();
        var values = new List<string>();

        foreach (var row in rows)
        {
            if (skipFirstRow.HasValue && skipFirstRow.Value && row.RowNumber() == usedRange.FirstRow().RowNumber())
                continue;

            string value = "";
            try
            {
                value = row.Cell(columnLetter)?.Value.GetText();
            }
            catch
            { }

            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return distinctOnly.HasValue && distinctOnly.Value
            ? values.Distinct()
            : values;
    }
    private async Task<(XLWorkbook Workbook, IXLWorksheet Worksheet)> ReadExcel(FileReference file, int worksheetIndex)
    {
        var stream = new MemoryStream();
        var downloaded = await fileManagementClient.DownloadAsync(file);
        await downloaded.CopyToAsync(stream);
        stream.Position = 0;
        var workbook = new XLWorkbook(stream);

        if (worksheetIndex < 1 || worksheetIndex > workbook.Worksheets.Count)
        {
            throw new PluginMisconfigurationException($"Invalid worksheet index: {worksheetIndex}. Please check your input and try again");
        }

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