﻿using Blackbird.Applications.Sdk.Common.Actions;
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
using DocumentFormat.OpenXml.Wordprocessing;
using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.Actions;

[ActionList]
public class Csv(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseInvocable(invocationContext)
{
    [Action("Remove CSV rows", Description = "Remove the selected rows from a CSV file.")]
    public async Task<CsvFile> RemoveRows(
        [ActionParameter] CsvFile csvFile,
        [ActionParameter] CsvOptions csvOptions,
        [ActionParameter][Display("Row indexes", Description = "The first row starts with 0")] IEnumerable<int> rowIndexes        
        )
    {
        var records = await ReadCsv(csvFile, csvOptions);
        var filteredRecords = records.Where((_, index) => !rowIndexes.Contains(index)).ToList();
        return await WriteCsv(filteredRecords, csvOptions, csvFile.File.Name, csvFile.File.ContentType);
    }

    [Action("Filter CSV rows", Description = "Remove the selected rows from a CSV file based on a column condition.")]
    public async Task<CsvFile> FilterRows(
        [ActionParameter] CsvFile csvFile,
        [ActionParameter] CsvOptions csvOptions,
        [ActionParameter][Display("Column index")] int columnIndex,
        [ActionParameter][Display("Condition", Description = "The condition that is applied to the column")][StaticDataSource(typeof(CsvColumnCondition))] string condition
        )
    {
        if (columnIndex < 0) throw new PluginApplicationException("A column index must be 0 or a positive number.");
        var records = await ReadCsv(csvFile, csvOptions);
        var newRecords = new List<List<string>>();
        foreach (var record in records)
        {
            if (columnIndex >= record.Count) throw new PluginApplicationException("The column index is bigger than the amount of columns.");
            var value = record[columnIndex];

            if (condition == "is_empty" && string.IsNullOrEmpty(value))
            {
                newRecords.Add(record);
            } else if (condition == "is_full" && !string.IsNullOrEmpty(value))
            {
                newRecords.Add(record);
            }
        }
        return await WriteCsv(newRecords, csvOptions, csvFile.File.Name, csvFile.File.ContentType);
    }

    [Action("Remove CSV columns", Description = "Remove the selected columns from a CSV file.")]
    public async Task<CsvFile> RemoveColumns(
    [ActionParameter] CsvFile csvFile,
    [ActionParameter] CsvOptions csvOptions,
    [ActionParameter][Display("Column indexes", Description = "The first column starts with 0")] IEnumerable<int> columnIndexes
    )
    {
        var records = await ReadCsv(csvFile, csvOptions);
        var newRecords = new List<List<string>>();
        foreach (var record in records)
        {
            var newColumns = record.Where((_, index) => !columnIndexes.Contains(index)).ToList();
            newRecords.Add(newColumns);
        }
        return await WriteCsv(newRecords, csvOptions, csvFile.File.Name, csvFile.File.ContentType);
    }

    [Action("Redefine CSV columns", Description = "Rearrange the columns of a CSV file according to the specified order.")]
    public async Task<CsvFile> SwapColumns(
        [ActionParameter] CsvFile csvFile,
        [ActionParameter] CsvOptions csvOptions,
        [ActionParameter][Display("New columns", Description = "0 being the first column. A value of [1, 1, 2] would indicate that there are 3 columns in the new CSV file. The first two columns would have the value of the original column 1, the third column would have original column 2.")] 
            IEnumerable<int> columnOrder)
    {
        if (columnOrder.Any(x => x < 0)) throw new PluginApplicationException("A column identifier must be 0 or a positive number.");
        var records = await ReadCsv(csvFile, csvOptions);
        var newRecords = new List<List<string>>();
        foreach (var record in records)
        {
            var newColumns = columnOrder.Select(index => index < record.Count ? record[index] : "").ToList();
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
        foreach (var record in records)
        {
            if (columnIndex < record.Count)
            {
                if (String.IsNullOrEmpty(regex.Group))
                {
                    record[columnIndex] = Regex.Match(record[columnIndex], regex.Regex).Value;
                }
                else
                {
                    record[columnIndex] = Regex.Match(record[columnIndex], regex.Regex).Groups[regex.Group].Value;
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
        var records = await ReadCsv(csvFile, csvOptions);
        if (rowIndex >= records.Count) return csvFile;

        for (int i = 0; i < records[rowIndex].Count; i++)
        {
            if (String.IsNullOrEmpty(regex.Group))
            {
                records[rowIndex][i] = Regex.Match(records[rowIndex][i], regex.Regex).Value;
            }
            else
            {
                records[rowIndex][i] = Regex.Match(records[rowIndex][i], regex.Regex).Groups[regex.Group].Value;
            }
        }

        return await WriteCsv(records, csvOptions, csvFile.File.Name, csvFile.File.ContentType);
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
