using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Shared;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using System.Text;
using UglyToad.PdfPig;

namespace Apps.Utilities.Actions;

[ActionList]
public class Files : BaseInvocable
{
    private readonly IFileManagementClient _fileManagementClient;
    public Files(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : base(
       invocationContext)
    {
        _fileManagementClient = fileManagementClient;
    }

    [Action("Get file name", Description = "Returns the name of a file (without extension).")]
    public NameResponse GetFileName([ActionParameter] FileDto file)
    {
        return new NameResponse { Name = Path.GetFileNameWithoutExtension(file.File.Name) };
    }

    [Action("Change file name", Description = "Rename a file (without extension).")]
    public FileDto ChangeFileName([ActionParameter] FileDto file, [ActionParameter] RenameRequest input)
    {
        var extension = Path.GetExtension(file.File.Name);
        file.File.Name = input.Name + extension;
        return new FileDto { File = file.File };
    }

    [Action("Sanitize file name", Description = "Remove any defined characters from a file name (without extension).")]
    public FileDto SanitizeFileName([ActionParameter] FileDto file, [ActionParameter] SanitizeRequest input)
    {
        var extension = Path.GetExtension(file.File.Name);
        var newName = file.File.Name;
        foreach (string filteredCharacter in input.FilterCharacters)
        {
            newName = newName.Replace(filteredCharacter, string.Empty);
        }
        file.File.Name = newName + extension;
        return new FileDto { File = file.File };
    }

    [Action("Get file character count", Description = "Returns number of characters in the file")]

    public async Task<int> GetCharCountInFile([ActionParameter] FileDto file)
    {
        
        var _file = await _fileManagementClient.DownloadAsync(file.File);

        var extension = Path.GetExtension(file.File.Name).ToLower();

        var filecontent = await ReadDocument(_file, extension);

        return filecontent.Length;
    }

    [Action("Get file word count", Description = "Returns number of words in the file")]

    public async Task<int> GetWordCountInFile([ActionParameter] FileDto file)
    {

        var _file = await _fileManagementClient.DownloadAsync(file.File);

        var extension = Path.GetExtension(file.File.Name).ToLower();
        
        var filecontent = await ReadDocument(_file, extension);

        return CountWords(filecontent);
    }

    public static async Task<string> ReadDocument(Stream file, string fileExtension)
    {
        string text;
        if (fileExtension == ".txt")
            text = await ReadTxtFile(file);
        else if (fileExtension == ".pdf")
            text = await ReadPdfFile(file);
        else if (fileExtension == ".docx" || fileExtension == ".doc")
            text = await ReadDocxFile(file);
        else
            throw new ArgumentException("Unsupported document format. Please provide docx, pdf or txt file.");

        return text;
    }

    private static async Task<string> ReadTxtFile(Stream file)
    {
        var stringBuilder = new StringBuilder();
        using (var reader = new StreamReader(file))
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                stringBuilder.Append(line);
            }
        }

        var document = stringBuilder.ToString();
        return document;
    }

    private static async Task<string> ReadPdfFile(Stream file)
    {
       
        var document = PdfDocument.Open(file);
        var text = string.Join(" ", document.GetPages().Select(p => p.Text));
        return text;
        
    }

    private static async Task<string> ReadDocxFile(Stream file)
    {
      
        var document = WordprocessingDocument.Open(file, false);
        var text = document.MainDocumentPart.Document.Body.InnerText;
        return text;
       
    }

    private static int CountWords( string text)
    {
        char[] punctuationCharacters = text.Where(char.IsPunctuation).Distinct().ToArray();
        var words = text.Split().Select(x => x.Trim(punctuationCharacters));
        return words.Where(x => !string.IsNullOrWhiteSpace(x)).Count();
    }
}


