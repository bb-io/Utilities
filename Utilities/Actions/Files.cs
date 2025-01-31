using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Shared;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace Apps.Utilities.Actions;

[ActionList]
public class Files : BaseInvocable
{
    private readonly IFileManagementClient _fileManagementClient;

    private readonly ILogger<Files> _logger;

    public Files(InvocationContext invocationContext, IFileManagementClient fileManagementClient, ILogger<Files> logger) : base(
        invocationContext)
    {
        _fileManagementClient = fileManagementClient;
        _logger = logger;
        _logger.LogInformation("Files is called.");
    }

    [Action("Get file name information",
        Description = "Returns the name of a file, with or without extension, and the extension.")]
    public NameResponse GetFileName([ActionParameter] FileDto file)
    {
        return new NameResponse
        {
            NameWithoutExtension = Path.GetFileNameWithoutExtension(file.File.Name),
            NameWithExtension = Path.GetFileName(file.File.Name),
            Extension = Path.GetExtension(file.File.Name)
        };
    }

    [Action("Get file size", Description = "Returns the size of a file in bytes.")]
    public async Task<double> GetFileSize([ActionParameter] FileDto file)
    {
        var fileStream = await _fileManagementClient.DownloadAsync(file.File);
        var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream.Length;
    }

    [Action("Convert document to text",
        Description = "Load document's text. Document must be in docx/doc, pdf or any plaintext format.")]
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
    public async Task<double> GetWordCountInFile([ActionParameter] FileDto file)
    {
        var _file = await _fileManagementClient.DownloadAsync(file.File);

        var extension = Path.GetExtension(file.File.Name).ToLower();
        var filecontent = await ReadDocument(_file, extension);
        return (double)CountWords(filecontent);
    }
    
    [Action("Get files word count", Description = "Returns number of words in the files")]
    public async Task<FilesWordCountResponse> GetWordCountInFiles([ActionParameter] FilesWordCountRequest request)
    {
        double totalWordCount = 0;
        var files = new List<WordCountItem>();
        foreach (var file in request.Files)
        {
            var wordCount = await GetWordCountInFile(new FileDto { File = file });
            totalWordCount += wordCount;
            files.Add(new WordCountItem
            {
                FileName = file.Name,
                WordCount = wordCount
            });
        }

        return new FilesWordCountResponse
        {
            WordCount = totalWordCount,
            FilesWithWordCount = files
        };
    }
    
    [Action("Replace using Regex in document", Description = "Replace text in a document using Regex. Works only with text based files (txt, html, etc.). Action is pretty similar to 'Replace using Regex' but works with files")]
    public async Task<ReplaceTextInDocumentResponse> ReplaceTextInDocument(
        [ActionParameter] ReplaceTextInDocumentRequest request)
    {
        var file = await _fileManagementClient.DownloadAsync(request.File);
        var fileMemoryStream = new MemoryStream();
        await file.CopyToAsync(fileMemoryStream);
        fileMemoryStream.Position = 0;
        
        var reader = new StreamReader(fileMemoryStream);
        var text = await reader.ReadToEndAsync();
        var replacedText = Regex.Replace(text, request.Regex, request.Replace);
        return new()
        {
            File = await _fileManagementClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(replacedText)),
                request.File.ContentType, request.File.Name)
        };
    }
    
    [Action("Extract using Regex from document", Description = "Extract text from a document using Regex. Works only with text based files (txt, html, etc.). Action is pretty similar to 'Extract using Regex' but works with files")]
    public async Task<ExtractTextFromDocumentResponse> ExtractTextFromDocument(
        [ActionParameter] ExtractTextFromDocumentRequest request)
    {
        var file = await _fileManagementClient.DownloadAsync(request.File);
        var fileMemoryStream = new MemoryStream();
        await file.CopyToAsync(fileMemoryStream);
        fileMemoryStream.Position = 0;
        
        var reader = new StreamReader(fileMemoryStream);
        var text = await reader.ReadToEndAsync();
        
        text = String.IsNullOrEmpty(request.Group) 
            ? Regex.Match(text, request.Regex).Value 
            : Regex.Match(text, request.Regex).Groups[request.Group].Value;

        return new()
        {
            ExtractedText = text
        };
    }

    [Action("Convert text to document", Description = "Convert text to txt, doc or docx document.")]
    public async Task<ConvertTextToDocumentResponse> ConvertTextToDocument(
        [ActionParameter] ConvertTextToDocumentRequest request)
    {
        var filename = $"{request.Filename}{request.FileExtension}";
        ConvertTextToDocumentResponse response;

        switch (request.FileExtension.ToLower())
        {
            case ".txt":
                response = await ConvertToTextFile(request.Text, filename, MediaTypeNames.Text.Plain);
                break;
            case ".html":
                response = await ConvertToTextFile(request.Text, filename, MediaTypeNames.Text.Html);
                break;
            case ".doc":
            case ".docx":
                var font = request.Font ?? "Arial";
                var fontSize = request.FontSize ?? 12;
                response = await ConvertToWordDocument(request.Text, filename, font, fontSize);
                break;
            default:
                throw new ArgumentException("Can convert to txt, doc, or docx file only.");
        }

        return response;
    }

    [Action("Unzip files", Description = "Take a .zip file and unzips it into multiple files")]
    public async Task<MultipleFilesResponse> UnzipFiles([ActionParameter] FileDto request)
    {
        try
        {
            _logger.LogInformation("Unzipfiles is called.");

            var file = await _fileManagementClient.DownloadAsync(request.File);
            _logger.LogInformation("file is received to unzip");


            var files = new List<FileDto>();
            using (var filestream = new MemoryStream())
            {
                await file.CopyToAsync(filestream);
                filestream.Position = 0;
                using (var zip = new ZipArchive(filestream, ZipArchiveMode.Read, false))
                {
                    _logger.LogInformation("zip is opened");
                    foreach (var entry in zip.Entries)
                    {
                        _logger.LogInformation("zip entry is opened.");
                        using (var stream = entry.Open())
                        {
                            _logger.LogInformation("zip entry stream is opened.");
                            var uploadedFile = await _fileManagementClient.UploadAsync(stream, MimeTypes.GetMimeType(entry.Name), entry.Name);
                            files.Add(new FileDto { File = uploadedFile });
                            _logger.LogInformation("zip entry is uploaded.");
                        }
                    }
                }
            }
            _logger.LogInformation("Every zip is unzipped.");

            return new MultipleFilesResponse
            {
                Files = files
            };
        }
        catch (Exception e )
        {

            _logger.LogError(e.Message + e.StackTrace);
            throw e;
        }
        
    }

    private async Task<ConvertTextToDocumentResponse> ConvertToTextFile(string text, string filename, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var file = await _fileManagementClient.UploadAsync(new MemoryStream(bytes), contentType,
            filename);

        return new ConvertTextToDocumentResponse
        {
            File = file
        };
    }

    private async Task<ConvertTextToDocumentResponse> ConvertToWordDocument(string text, string filename, string font, int fontSize)
    {
        var stream = new MemoryStream();

        using (var doc = WordprocessingDocument.Create(stream,
                   DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            new Document(new Body()).Save(mainPart);
            var body = mainPart.Document.Body!;

            var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.None);
            
            var runProperties = new RunProperties();
            var runFonts = new RunFonts { Ascii = font };
            var size = new FontSize { Val = (fontSize * 2).ToString() }; // Font size in half-points (24 = 12pt)

            runProperties.Append(runFonts);
            runProperties.Append(size);
            
            foreach (var para in paragraphs)
            {
                var run = new Run();
                run.Append(runProperties.CloneNode(true));
                run.Append(new Text(para));
                
                var paragraph = new Paragraph(run);
                body.Append(paragraph);
            }

            mainPart.Document.Save();
        }

        stream.Seek(0, SeekOrigin.Begin);
        var file = await _fileManagementClient.UploadAsync(stream,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document", filename);

        return new ConvertTextToDocumentResponse
        {
            File = file
        };
    }

    private static async Task<string> ReadDocument(Stream file, string fileExtension)
    {
        string text;
        if (fileExtension == ".pdf")
            text = await ReadPdfFile(file);
        else if (fileExtension == ".docx" || fileExtension == ".doc")
            text = await ReadDocxFile(file);
        else if (fileExtension == ".html")
            text = await ReadHtmlFile(file);
        else
            text = await ReadPlaintextFile(file);

        return text;
    }

    private static async Task<string> ReadHtmlFile(Stream file)
    {
        var doc = new HtmlDocument();
        using (var reader = new StreamReader(file))
        {
            var htmlContent = await reader.ReadToEndAsync();
            doc.LoadHtml(htmlContent);
        }
        var text = doc.DocumentNode.InnerText;
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    private static async Task<string> ReadPlaintextFile(Stream file)
    {
        var stringBuilder = new StringBuilder();
        using (var reader = new StreamReader(file))
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                stringBuilder.AppendLine(line);
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

    private static int CountWords(string text)
    {
        char[] punctuationCharacters = text.Where(char.IsPunctuation).Distinct().ToArray();
        var words = text.Split().Select(x => x.Trim(punctuationCharacters));
        return words.Where(x => !string.IsNullOrWhiteSpace(x)).Count();
    }
}