using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using Apps.Utilities.Models.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper;
using Blackbird.Applications.Sdk.Common.Exceptions;
using System.Text.RegularExpressions;
using Apps.Utilities.Models.Texts;
using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Apps.Utilities.Models.Csv;

namespace Apps.Utilities.Actions;

[ActionList("CSV")]
public class Csv(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseInvocable(invocationContext)
{
    [Action("Remove CSV rows", Description = "Remove the selected rows from a CSV file.")]
    public async Task<CsvFile> RemoveRows(
        [ActionParameter] CsvFile csvFile,
        [ActionParameter] CsvOptions csvOptions,
        [ActionParameter] RowIndexesRequest rowIndexes        
        )
    {
        var records = await ReadCsv(csvFile, csvOptions);
        var filteredRecords = records.Where((_, index) => !rowIndexes.RowIndexes.Contains(index)).ToList();
        return await WriteCsv(filteredRecords, csvOptions, csvFile.File.Name, csvFile.File.ContentType);
    }

    [Action("Filter CSV rows", Description = "Remove the selected rows from a CSV file based on a column condition.")]
    public async Task<CsvFile> FilterRows(
        [ActionParameter] CsvFile csvFile,
        [ActionParameter] CsvOptions csvOptions,
        [ActionParameter][Display("Column index")] int columnIndex,
        [ActionParameter][Display("Condition", Description = "The condition that is applied to the column")][StaticDataSource(typeof(CsvColumnCondition))] string condition,
        [ActionParameter][Display("Value to compare")] string? targetValue)
    {
        if (columnIndex < 0) throw new PluginApplicationException("A column index must be 0 or a positive number.");
        var records = await ReadCsv(csvFile, csvOptions);
        var newRecords = new List<List<string>>();
        foreach (var record in records)
        {
            if (columnIndex >= record.Count) throw new PluginApplicationException("The column index is bigger than the amount of columns.");
            var value = record[columnIndex];

            switch (condition)
            {
                case "is_empty":
                    if (string.IsNullOrEmpty(value))
                        newRecords.Add(record);
                    break;

                case "is_full":
                    if (!string.IsNullOrEmpty(value))
                        newRecords.Add(record);
                    break;

                case "value_equals":
                    if (String.IsNullOrEmpty(targetValue))
                        throw new PluginMisconfigurationException("Optional value to compare needs to be filled in when using Value equals or contains conditions");
                    if (value == targetValue) 
                        newRecords.Add(record);
                    break;

                case "value_contains":
                    if (String.IsNullOrEmpty(targetValue))
                        throw new PluginMisconfigurationException("Optional value to compare needs to be filled in when using Value equals or contains conditions");
                    if (value.Contains(targetValue))
                        newRecords.Add(record);
                    break;

                default:
                    throw new PluginApplicationException($"Unsupported condition: {condition}");
            }
        }
        return await WriteCsv(newRecords, csvOptions, csvFile.File.Name, csvFile.File.ContentType);
    }

    [Action("Remove CSV columns", Description = "Remove the selected columns from a CSV file.")]
    public async Task<CsvFile> RemoveColumns(
    [ActionParameter] CsvFile csvFile,
    [ActionParameter] CsvOptions csvOptions,
    [ActionParameter] ColumnIndexesRequest columnIndexes
    )
    {
        var records = await ReadCsv(csvFile, csvOptions);
        var newRecords = new List<List<string>>();
        foreach (var record in records)
        {
            var newColumns = record.Where((_, index) => !columnIndexes.ColumnIndexes.Contains(index)).ToList();
            newRecords.Add(newColumns);
        }
        return await WriteCsv(newRecords, csvOptions, csvFile.File.Name, csvFile.File.ContentType);
    }

    [Action("Get CSV rows", Description = "Read a CSV file and return its rows, where each row is a list of cell values.")]
    public async Task<CsvRowsResponse> GetCsvRows(
    [ActionParameter] CsvFile csvFile,
    [ActionParameter] CsvOptions csvOptions
)
    {
        var records = await ReadCsv(csvFile, csvOptions) ?? new List<List<string>>();

        var rows = records.Select((record, index) => new Row
        {
            Id = (index + 1).ToString(),
            Values = record
        }).ToList();

        return new CsvRowsResponse
        {
            Rows = rows,
            TotalRows = rows.Count
        };
    }

    [Action("Redefine CSV columns", Description = "Rearrange the columns of a CSV file according to the specified order.")]
    public async Task<CsvFile> SwapColumns(
        [ActionParameter] CsvFile csvFile,
        [ActionParameter] CsvOptions csvOptions,
        [ActionParameter] ColumnOrderRequest columnOrder)
    {
        if (columnOrder.ColumnOrder.Any(x => x < 0)) throw new PluginApplicationException("A column identifier must be 0 or a positive number.");
        var records = await ReadCsv(csvFile, csvOptions);
        var newRecords = new List<List<string>>();
        foreach (var record in records)
        {
            var newColumns = columnOrder.ColumnOrder.Select(index => index < record.Count ? record[index] : "").ToList();
            newRecords.Add(newColumns);
        }
        return await WriteCsv(newRecords, csvOptions, csvFile.File.Name, csvFile.File.ContentType);
    }

    [Action("Apply regex to CSV column", Description = "Apply a regex pattern to a specified column in the CSV file.")]
    public async Task<CsvFile> ApplyRegexToColumn(
        [ActionParameter] CsvFile csvFile,
        [ActionParameter] CsvOptions csvOptions,
        [ActionParameter][Display("Column index")] int columnIndex,
        [ActionParameter] RegexInput regex)
    {
        if (columnIndex < 0) throw new PluginApplicationException("A column index must be 0 or a positive number.");
        var records = await ReadCsv(csvFile, csvOptions);
        var replaceValues = new Dictionary<string, string>();
        bool useDictionary = false;
        if (regex.From != null && regex.To != null)
        {
            useDictionary = true;
            replaceValues = regex.From.ToList().Zip(regex.To.ToList(), (key, value) => new { key, value })
                       .ToDictionary(x => x.key, x => x.value);
        }
        foreach (var record in records)
        {
            if (columnIndex < record.Count)
            {
                try
                {
                    if (useDictionary)
                    {
                        var match = Regex.Match(record[columnIndex], regex.Regex);
                        if (match.Success && replaceValues.ContainsKey(match.Value))
                        {
                            record[columnIndex] = Regex.Replace(record[columnIndex], match.Value, replaceValues[match.Value]);
                        }
                    }
                    else if (!string.IsNullOrEmpty(regex.Replace))
                    {
                        if (Regex.IsMatch(record[columnIndex], regex.Regex))
                        {
                            record[columnIndex] = Regex.Replace(record[columnIndex], regex.Regex, regex.Replace);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(regex.Group))
                        {
                            record[columnIndex] = Regex.Match(record[columnIndex], regex.Regex).Value;
                        }
                        else
                        {
                            record[columnIndex] = Regex.Match(record[columnIndex], regex.Regex).Groups[regex.Group].Value;
                        }
                    }
                }
                catch (RegexParseException ex)
                {
                    throw new PluginMisconfigurationException($"Error in regular expression: {ex.Message}");
                }
            }
        }
        return await WriteCsv(records, csvOptions, csvFile.File.Name, csvFile.File.ContentType);
    }

    [Action("Apply regex to CSV row", Description = "Apply a regex pattern to a specified row in the CSV file.")]
    public async Task<CsvFile> ApplyRegexToRow(
    [ActionParameter] CsvFile csvFile,
    [ActionParameter] CsvOptions csvOptions,
    [ActionParameter][Display("Row index")] int rowIndex,
    [ActionParameter] RegexInput regex)
    {
        if (rowIndex < 0) throw new PluginApplicationException("A row index must be 0 or a positive number.");

        bool hasFrom = regex.From != null && regex.From.Any();
        bool hasTo = regex.To != null && regex.To.Any();
        if (hasFrom || hasTo)
        {
            if (!hasFrom || !hasTo || regex.From.Count() != regex.To.Count())
                throw new PluginMisconfigurationException("'From' and 'To' lists must both be provided and have the same number of " +
                    "elements when one is specified. Please check the input and try again");
        }

        var records = await ReadCsv(csvFile, csvOptions);
        if (rowIndex >= records.Count) return csvFile;
        var replaceValues = new Dictionary<string,string>();
        bool useDictionary = false;
        if (regex.From != null && regex.To != null)
        {
            useDictionary = true;
            replaceValues = regex.From.ToList().Zip(regex.To.ToList(), (key, value) => new { key, value })
                       .ToDictionary(x => x.key, x => x.value);
        }

        for (int i = 0; i < records[rowIndex].Count; i++)
        {
            try
            {
                if (useDictionary)
                {
                    var match = Regex.Match(records[rowIndex][i], regex.Regex);
                    if (match != null && match.Success)
                    {
                        if (!replaceValues.ContainsKey(match.Value))
                        {
                            throw new PluginMisconfigurationException($"The matched value '{match.Value}' was not found. Please ensure all possible matched values are included in the 'From' list.");
                        }
                        records[rowIndex][i] = Regex.Replace(records[rowIndex][i], match.Value, replaceValues[match.Value]);
                    }
                }
                else if (!string.IsNullOrEmpty(regex.Replace))
                {
                    if (Regex.IsMatch(records[rowIndex][i], regex.Regex))
                    {
                        records[rowIndex][i] = Regex.Replace(records[rowIndex][i], regex.Regex, regex.Replace);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(regex.Group))
                    {
                        records[rowIndex][i] = Regex.Match(records[rowIndex][i], regex.Regex).Value;
                    }
                    else
                    {
                        records[rowIndex][i] = Regex.Match(records[rowIndex][i], regex.Regex).Groups[regex.Group].Value;
                    }
                }
            }
            catch (RegexParseException ex)
            {
                throw new PluginMisconfigurationException($"Error in regular expression: {ex.Message}");
            }
        }

        return await WriteCsv(records, csvOptions, csvFile.File.Name, csvFile.File.ContentType);
    }

    [Action("Add CSV row", Description = "Add a new row at the specified row index to the CSV file")]
    public async Task<CsvFile> AddRow(
    [ActionParameter] CsvFile csvFile,
    [ActionParameter] CsvOptions csvOptions,
    [ActionParameter] RowPositionOption rawOptions)
    {
        var records = await ReadCsv(csvFile, csvOptions);

        if (rawOptions.RowPosition == null)
        {
            rawOptions.RowPosition = records.Count;
        }

        if (rawOptions.RowPosition < 0 || rawOptions.RowPosition > records.Count)
        {
            throw new PluginMisconfigurationException("Invalid row position specified. Please check the input and try again");
        }

        records.Insert((int)rawOptions.RowPosition, rawOptions.InputValues.ToList());

        return await WriteCsv(records, csvOptions, csvFile.File.Name, csvFile.File.ContentType);
    }

    [Action("Get CSV column values", Description = "Retrieve all values from a specified column in the CSV file")]
    public async Task<List<string>> GetColumnValues(
    [ActionParameter] CsvFile csvFile,
    [ActionParameter] CsvOptions csvOptions,
    [ActionParameter][Display("Column index")] int columnIndex,
    [ActionParameter][Display("Deduplicate values")] bool? deduplicate = null)
    {
        var records = await ReadCsv(csvFile, csvOptions);

        if (!records.Any())
        {
            return new List<string>();
        }

        if (columnIndex < 0 || columnIndex >= records.Count)
        {
            throw new PluginMisconfigurationException($"Invalid column index '{columnIndex}'. " +
                $"The file has {records.Count} columns. Index starts at 0.");
        }

        var columnValues = records
            .Select(row => row[columnIndex])
            .ToList();

        if (deduplicate == true)
        {
            columnValues = columnValues
                .Distinct()
                .ToList();
        }

        return columnValues;
    }

    [Action("Sum numbers in column", Description = "Sums integer values from a specified CSV column in the given row range. Empty/non-numeric values are treated as zero.")]
    public async Task<SumNumbersInColumnResponse> SumNumbersInColumn(
        [ActionParameter] CsvFile csvFile,
        [ActionParameter] CsvOptions csvOptions,
        [ActionParameter] SumNumbersInColumnRequest input)
    {
        if (input.ColumnIndex < 0)
            throw new PluginMisconfigurationException("Column index must be 0 or a positive number.");

        if (input.FromRow is < 0)
            throw new PluginMisconfigurationException("From row must be 0 or a positive number.");

        if (input.ToRow is < 0)
            throw new PluginMisconfigurationException("To row must be 0 or a positive number.");

        var records = await ReadCsv(csvFile, csvOptions);

        if (records.Count == 0)
        {
            return new SumNumbersInColumnResponse
            {
                Sum = 0,
                FromRowUsed = 0,
                ToRowUsed = 0,
                RowsProcessed = 0
            };
        }

        var from = input.FromRow ?? FindFirstNumericRowIndex(records, input.ColumnIndex);
        var to = input.ToRow ?? (records.Count - 1);

        if (from >= records.Count)
        {
            var clampedTo = Math.Min(to, records.Count - 1);
            return new SumNumbersInColumnResponse
            {
                Sum = 0,
                FromRowUsed = from,
                ToRowUsed = clampedTo,
                RowsProcessed = 0
            };
        }

        if (to >= records.Count)
            to = records.Count - 1;

        if (from > to)
            throw new PluginMisconfigurationException("'From row' cannot be greater than 'To row'.");

        long sum = 0;
        for (int i = from; i <= to; i++)
        {
            var row = records[i];
            var raw = input.ColumnIndex < row.Count ? row[input.ColumnIndex] : string.Empty;

            if (TryParseIntInvariant(raw, out var value))
                sum += value;
        }

        return new SumNumbersInColumnResponse
        {
            Sum = sum,
            FromRowUsed = from,
            ToRowUsed = to,
            RowsProcessed = to - from + 1
        };
    }

    private static int FindFirstNumericRowIndex(List<List<string>> records, int columnIndex)
    {
        for (int i = 0; i < records.Count; i++)
        {
            var row = records[i];
            if (columnIndex >= row.Count) continue;

            if (TryParseIntInvariant(row[columnIndex], out _))
                return i;
        }

        return 0;
    }

    private static bool TryParseIntInvariant(string? raw, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private CsvConfiguration CreateConfiguration(CsvOptions csvOptions)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        if (csvOptions.HasHeader.HasValue) config.HasHeaderRecord = csvOptions.HasHeader.Value;
        if (csvOptions.IgnoreBlankLines.HasValue) config.IgnoreBlankLines = csvOptions.IgnoreBlankLines.Value;
        if (csvOptions.NewLine is not null) config.NewLine = csvOptions.NewLine;
        if (csvOptions.Delimiter is not null) config.Delimiter = csvOptions.Delimiter;
        if (csvOptions.Comment is not null && csvOptions.Comment.Length > 1) config.Comment = csvOptions.Comment[0];
        if (csvOptions.Escape is not null && csvOptions.Escape.Length > 1) config.Escape = csvOptions.Escape[0];
        if (csvOptions.Quote is not null && csvOptions.Quote.Length > 1) config.Quote = csvOptions.Quote[0];

        return config;
    }

    private async Task<List<List<string>>> ReadCsv(CsvFile csvFile, CsvOptions csvOptions)
    {
        await using var streamIn = await fileManagementClient.DownloadAsync(csvFile.File);
        using var reader = new StreamReader(streamIn);
        using var csv = new CsvReader(reader, CreateConfiguration(csvOptions));

        var records = new List<List<string>>();

        while (csv.Read())
        {
            var row = new List<string>();
            for (int i = 0; csv.TryGetField(i, out string? field); i++)
            {
                row.Add(field ?? "");
            }
            records.Add(row);
        }

        return records;
    }

    private async Task<CsvFile> WriteCsv(List<List<string>> records, CsvOptions csvOptions, string fileName, string mimeType = "text/csv")
    {
        using var streamOut = new MemoryStream();
        using (var writer = new StreamWriter(streamOut, leaveOpen: true))
        using (var csv = new CsvWriter(writer, CreateConfiguration(csvOptions)))
        {
            for (int i = 0; i < records.Count; i++)
            {
                foreach (var field in records[i])
                {
                    csv.WriteField(field);
                }

                if (i < records.Count - 1)
                {
                    csv.NextRecord();
                }
            }
        }

        streamOut.Position = 0;
        var file = await fileManagementClient.UploadAsync(streamOut, mimeType, fileName);
        return new CsvFile { File = file };
    }
}
