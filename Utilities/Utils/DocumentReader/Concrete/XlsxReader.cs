using Apps.Utilities.ErrorWrapper;
using Blackbird.Applications.Sdk.Common.Exceptions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text;

namespace Apps.Utilities.Utils.DocumentReader.Concrete;

public class XlsxReader : IDocumentReader
{
    public async Task<string> Read(Stream file)
    {
        return await ErrorWrapperExecute.ExecuteSafelyAsync(async () =>
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var document = SpreadsheetDocument.Open(memoryStream, false);
            var workbookPart = document.WorkbookPart
                ?? throw new PluginApplicationException("Invalid XLSX file: workbook part is missing.");

            var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
            var stringBuilder = new StringBuilder();

            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var cells = worksheetPart.Worksheet.Descendants<Cell>();

                foreach (var cell in cells)
                {
                    var cellText = GetCellText(cell, sharedStringTable);

                    if (!string.IsNullOrWhiteSpace(cellText))
                    {
                        stringBuilder.Append(cellText);
                        stringBuilder.Append(' ');
                    }
                }
            }

            return stringBuilder.ToString();
        });
    }

    private static string GetCellText(Cell cell, SharedStringTable? sharedStringTable)
    {
        if (cell == null)
            return string.Empty;

        if (cell.DataType == null)
            return cell.CellValue?.InnerText ?? cell.InnerText ?? string.Empty;

        var dataType = cell.DataType.Value;

        if (dataType == CellValues.SharedString)
            return GetSharedStringValue(cell, sharedStringTable);

        if (dataType == CellValues.InlineString)
            return cell.InlineString?.InnerText ?? string.Empty;

        if (dataType == CellValues.String)
            return cell.CellValue?.InnerText ?? cell.InnerText ?? string.Empty;

        if (dataType == CellValues.Boolean)
            return cell.CellValue?.InnerText == "1" ? "TRUE" : "FALSE";

        return cell.CellValue?.InnerText ?? cell.InnerText ?? string.Empty;
    }

    private static string GetSharedStringValue(Cell cell, SharedStringTable? sharedStringTable)
    {
        if (sharedStringTable == null)
            return string.Empty;

        if (!int.TryParse(cell.CellValue?.InnerText, out var index))
            return string.Empty;

        var item = sharedStringTable.Elements<SharedStringItem>()
            .ElementAtOrDefault(index);

        return item?.InnerText ?? string.Empty;
    }
}
