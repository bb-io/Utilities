using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Shared;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Net.Mime;
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

    [Action("Get file name information", Description = "Returns the name of a file, with or without extension, and the extension.")]
    public NameResponse GetFileName([ActionParameter] FileDto file)
    {
        return new NameResponse 
        { 
            NameWithoutExtension = Path.GetFileNameWithoutExtension(file.File.Name),
            NameWithExtension = Path.GetFileName(file.File.Name),
            Extension = Path.GetExtension(file.File.Name)

        };
    }

    [Action("Convert document to text", Description = "Load document's text. Document must be in docx/doc, pdf or txt format.")]
    public async Task<LoadDocumentResponse> LoadDocument([ActionParameter] LoadDocumentRequest request)
    {
        var file = await _fileManagementClient.DownloadAsync(request.File);
        var extension = Path.GetExtension(request.File.Name).ToLower();
        var filecontent = await ReadDocument(file, extension);
        return new() { Text = filecontent };
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

    [Action("Convert text to document", Description = "Convert text to txt, doc or docx document.")]
    public async Task<ConvertTextToDocumentResponse> ConvertTextToDocument(
         [ActionParameter] ConvertTextToDocumentRequest request)
    {        
        var filename = request.Filename + request.FileExtension;

        switch (request.FileExtension)
        {
            case ".txt":
                var bytes = Encoding.UTF8.GetBytes(request.Text);
                var file = await _fileManagementClient.UploadAsync(new MemoryStream(bytes), MediaTypeNames.Application.Octet,
                filename);
                return new ConvertTextToDocumentResponse
                {
                    File = file
                };
            case ".doc":
            case ".docx":
                var stream = new MemoryStream();
                using (WordprocessingDocument doc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
                {
                    MainDocumentPart mainPart = doc.AddMainDocumentPart();
                    new Document(new Body()).Save(mainPart);
                    Body body = mainPart.Document.Body!;

                    var paragraphs = request.Text.Split(new[] { "\n\n" }, StringSplitOptions.None);

                    foreach (var para in paragraphs)
                    {
                        var paragraph = new Paragraph(new Run(new Text(para)));
                        body.Append(paragraph);
                    }

                    mainPart.Document.Save();
                }
                stream.Seek(0, SeekOrigin.Begin);
                var _file = await _fileManagementClient.UploadAsync(stream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    filename);
                return new ConvertTextToDocumentResponse
                {
                    File = _file
                };
            default:
                throw new Exception("Can convert to txt, doc or docx file only.");
        }
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


