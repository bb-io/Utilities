using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using Apps.Utilities.Models.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper;
using System.Text;
using Blackbird.Applications.Sdk.Common.Exceptions;
using System.Text.RegularExpressions;
using Apps.Utilities.Models.Texts;

namespace Apps.Utilities.Actions;

[ActionList]
public class Csv(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseInvocable(invocationContext)
{
    [Action("Remove CSV rows", Description = "Remove the selected rows from a CSV file.")]
    public async Task<CsvFile> RemoveRows([ActionParameter] CsvFile csvFile, [ActionParameter][Display("Row indexes", Description = "The first row starts with 0")] IEnumerable<int> rowIndexes)
    {
        await using var streamIn = await fileManagementClient.DownloadAsync(csvFile.File);
        streamIn.Position = 0;

        using var reader = new StreamReader(streamIn);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            lines.Add(reader.ReadLine());
        }

        var filteredLines = lines.Where((_, index) => !rowIndexes.Contains(index)).ToList();

        using var streamOut = new MemoryStream();
        using (var writer = new StreamWriter(streamOut, leaveOpen: true))
        {
            foreach (var line in filteredLines)
            {
                writer.WriteLine(line);
            }
        }

        streamOut.Position = 0;
        var resultFile = await fileManagementClient.UploadAsync(streamOut, csvFile.File.ContentType, csvFile.File.Name);
        return new CsvFile { File = resultFile };
    }

    [Action("Redefine CSV columns", Description = "Rearrange the columns of a CSV file according to the specified order.")]
    public async Task<CsvFile> SwapColumns(
        [ActionParameter] CsvFile csvFile, 
        [ActionParameter][Display("New columns", Description = "0 being the first column. A value of [1, 1, 2] would indicate that there are 3 columns in the new CSV file. The first two columns would have the value of the original column 1, the third column would have original column 2.")] 
            IEnumerable<int> columnOrder)
    {
        await using var streamIn = await fileManagementClient.DownloadAsync(csvFile.File);
        streamIn.Position = 0;

        using var reader = new StreamReader(streamIn);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            lines.Add(reader.ReadLine());
        }

        var reorderedLines = new List<string>();
        foreach (var line in lines)
        {
            var columns = ParseCsvLine(line);
            try
            {
                var newColumns = columnOrder.Select(index => index < columns.Length ? columns[index] : "").ToArray();
                reorderedLines.Add(string.Join(",", newColumns.Select(EscapeCsvField)));
            } catch (IndexOutOfRangeException e)
            {
                throw new PluginMisconfigurationException("One of your column orders was smaller than 0 or larger than the amount of columns.");
            }
        }

        using var streamOut = new MemoryStream();
        using (var writer = new StreamWriter(streamOut, leaveOpen: true))
        {
            foreach (var line in reorderedLines)
            {
                writer.WriteLine(line);
            }
        }

        streamOut.Position = 0;
        var resultFile = await fileManagementClient.UploadAsync(streamOut, csvFile.File.ContentType, csvFile.File.Name);
        return new CsvFile { File = resultFile };
    }

    [Action("Apply regex to CSV column", Description = "Apply a regex pattern to a specified column in the CSV file.")]
    public async Task<CsvFile> ApplyRegexToColumn(
        [ActionParameter] CsvFile csvFile, 
        [ActionParameter][Display("Column index")] int columnIndex,
        [ActionParameter] RegexInput regex)
    {
        await using var streamIn = await fileManagementClient.DownloadAsync(csvFile.File);
        streamIn.Position = 0;

        using var reader = new StreamReader(streamIn);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            lines.Add(reader.ReadLine());
        }

        var modifiedLines = new List<string>();

        foreach (var line in lines)
        {
            var columns = ParseCsvLine(line);
            try
            {
                if (columnIndex < columns.Length)
                {
                    if (String.IsNullOrEmpty(regex.Group))
                    {
                        columns[columnIndex] = Regex.Match(columns[columnIndex], regex.Regex).Value;
                    }
                    else
                    {
                        columns[columnIndex] = Regex.Match(columns[columnIndex], regex.Regex).Groups[regex.Group].Value;
                    }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                throw new PluginMisconfigurationException("Your column index was smaller than 0 or larger than the amount of columns.");
            }
            modifiedLines.Add(string.Join(",", columns.Select(EscapeCsvField)));
        }

        using var streamOut = new MemoryStream();
        using (var writer = new StreamWriter(streamOut, leaveOpen: true))
        {
            foreach (var line in modifiedLines)
            {
                writer.WriteLine(line);
            }
        }

        streamOut.Position = 0;
        var resultFile = await fileManagementClient.UploadAsync(streamOut, csvFile.File.ContentType, csvFile.File.Name);
        return new CsvFile { File = resultFile };
    }

    private string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }
        fields.Add(currentField.ToString());
        return fields.ToArray();
    }

    private string EscapeCsvField(string field)
    {
        if (field.Contains("\"") || field.Contains(","))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
        return field;
    }
}
