using Apps.Utilities.ErrorWrapper;
using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Shared;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Vml.Office;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using ICSharpCode.SharpZipLib.Zip;
using Mammoth;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UglyToad.PdfPig;

namespace Apps.Utilities.Actions;

[ActionList("Files")]
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

    [Action("Change file extension", Description = "Update file extension.")]
    public FileDto ChangeFileExtension([ActionParameter] FileDto file, [ActionParameter] string Extension)
    {
        var name = Path.GetFileNameWithoutExtension(file.File.Name);
        string newExtension = Extension.Contains(".") ? Extension : "." + Extension;
        file.File.Name = name + newExtension;
        return new FileDto { File = file.File };
    }

    [Action("Sanitize file name", Description = "Remove any defined characters from a file name (without extension).")]
    public FileDto SanitizeFileName([ActionParameter] FileDto file, [ActionParameter] SanitizeRequest input)
    {
        var extension = Path.GetExtension(file.File.Name);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.File.Name);

        if (input.FilterCharacters?.Any() == true)
        {
            var escapedChars = input.FilterCharacters
                .Select(c => Regex.Escape(c.TrimEnd(' ')))
                .ToArray();

            var pattern = string.Join("|", escapedChars);

            fileNameWithoutExtension = Regex.Replace(fileNameWithoutExtension, pattern, string.Empty);
        }

        file.File.Name = fileNameWithoutExtension + extension;

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
        string replacedText = "";
        try
        {
            replacedText = Regex.Replace(text, request.Regex, request.Replace ?? string.Empty);
        }
        catch (Exception e)
        {
            throw new PluginApplicationException(e.Message);
        }
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

        try
        {
            text = String.IsNullOrEmpty(request.Group)
                ? Regex.Match(text, request.Regex).Value
                : Regex.Match(text, request.Regex).Groups[request.Group].Value;
        }
        catch (RegexParseException ex)
        {
            throw new PluginMisconfigurationException($"Error in regular expression: {ex.Message}");
        }

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
        var (encoding, includeBom) = ErrorWrapperExecute.ExecuteSafely(() => ResolveEncoding(request.Encoding));

        ConvertTextToDocumentResponse response = request.FileExtension.ToLower() switch
        {
            ".txt" => await ErrorWrapperExecute.ExecuteSafelyAsync(() => ConvertToTextFile(request.Text, filename, MediaTypeNames.Text.Plain, encoding, includeBom)),
            ".csv" => await ErrorWrapperExecute.ExecuteSafelyAsync(() => ConvertToTextFile(request.Text, filename, "text/csv", encoding, includeBom)),
            ".html" => await ErrorWrapperExecute.ExecuteSafelyAsync(() => ConvertToTextFile(request.Text, filename, MediaTypeNames.Text.Html, encoding, includeBom)),
            ".json" => await ErrorWrapperExecute.ExecuteSafelyAsync(() => ConvertToTextFile(request.Text, filename, MediaTypeNames.Application.Json, encoding, includeBom)),
            ".doc" or ".docx" =>
                 await ErrorWrapperExecute.ExecuteSafelyAsync(() => ConvertToWordDocument(request.Text, filename, request.Font ?? "Arial", request.FontSize ?? 12)),
            _ => throw new PluginMisconfigurationException("Can convert to txt, csv, html, json, doc, or docx file only.")
        };

        return response;
    }

    [Action("Compare file contents", Description = "Compare whether two files have the same content.")]
    public async Task<CompareContentResults> CompareFileContents(
    [ActionParameter] CompareFilesRequest request)
    {
        string currentContent = null;
        foreach (var file in request.Files)
        {
            var stream = await _fileManagementClient.DownloadAsync(file);
            var filecontent = await ReadPlaintextFile(stream);
            if (currentContent == null)
            {
                currentContent = filecontent;
                continue;
            }

            if (currentContent != filecontent)
            {
                return new CompareContentResults { AreEqual = false };
            }
        }

        return new CompareContentResults { AreEqual = true };
    }

    [Action("Concatenate text files", Description = "Concatenate multiple text files into one file.")]
    public async Task<FileDto> ConcatenateFiles(
    [ActionParameter] MultipleFilesRequest request)
    {
        var firstFile = request.Files.FirstOrDefault();
        var extension = Path.GetExtension(firstFile.Name);
        var mimeType = firstFile.ContentType;

        var encoding = Encoding.UTF8;

        var outputStream = new MemoryStream();

        using (var outputWriter = new StreamWriter(outputStream, encoding, leaveOpen: true))
        {
            foreach (var fileRef in request.Files)
            {
                var file = await _fileManagementClient.DownloadAsync(fileRef);

                using (var seekableStream = new MemoryStream())
                {
                    await file.CopyToAsync(seekableStream);
                    seekableStream.Position = 0;

                    using var reader = new StreamReader(seekableStream, encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        await outputWriter.WriteLineAsync(line);
                    }
                }
            }
            await outputWriter.FlushAsync();
        }

        outputStream.Position = 0;

        var uploadedFile = await _fileManagementClient.UploadAsync(
            outputStream,
            mimeType,
            "MergedFile"+extension
        );

        return new FileDto { File = uploadedFile };
    }

    [Action("Unzip files", Description = "Take a .zip file and unzips it into multiple files")]
    public async Task<MultipleFilesResponse> UnzipFiles([ActionParameter] FileDto request)
    {
        if (!request.File.Name.EndsWith(".zip"))
            throw new PluginMisconfigurationException("The input file must be a zip.");

        var file = await _fileManagementClient.DownloadAsync(request.File);
        var files = new List<FileDto>();

        using (var seekableStream = new MemoryStream())
        {
            file.CopyTo(seekableStream);
            seekableStream.Position = 0;

            using (var zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(seekableStream))
            {
                foreach (ZipEntry entry in zip)
                {
                    if (!entry.CanDecompress || entry.IsDirectory)
                        continue;

                    using (var entryStream = zip.GetInputStream(entry))
                    using (var buffer = new MemoryStream())
                    {
                        entryStream.CopyTo(buffer);
                        buffer.Position = 0;

                        var uploadedFile = await _fileManagementClient.UploadAsync(
                            buffer,
                            MimeTypes.GetMimeType(entry.Name),
                            entry.Name
                        );
                        files.Add(new FileDto { File = uploadedFile });
                    }
                }
            }
        }

        return new MultipleFilesResponse
        {
            Files = files
        };
    }

    [Action("Zip files", Description = "Take multiple files and compress them into a single .zip archive")]
    public async Task<FileDto> ToZipFiles([ActionParameter] FilesToZipRequest request)
    {
        if (request.Files == null || !request.Files.Any())
            throw new PluginMisconfigurationException("No files provided to zip.");

        using var archiveStream = new MemoryStream();
        using (var zip = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var fileRef in request.Files)
            {
                var inputStream = await _fileManagementClient.DownloadAsync(fileRef);
                var entry = zip.CreateEntry(fileRef.Name, CompressionLevel.Optimal);

                using var entryStream = entry.Open();
                await inputStream.CopyToAsync(entryStream);
            }
        }

        archiveStream.Position = 0;
        var zipFileDto = await _fileManagementClient.UploadAsync(
            archiveStream,
            "application/zip",
            $"archive_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip"
        );

        return new FileDto { File = zipFileDto };
    }


    [Action("Convert docx file to html", Description = "Converts a docx file into an html file")]
    public async Task<ConvertTextToDocumentResponse> ConvertDocxToHtml([ActionParameter] FileDto request)
    {
        if (!request.File.Name.EndsWith(".doc") && !request.File.Name.EndsWith(".docx"))
            throw new PluginMisconfigurationException("The input file must be a doc or docx.");

        var docxInputStream = await _fileManagementClient.DownloadAsync(request.File);
        var converter = new DocumentConverter();
        string htmlString = "";
        try
        {
            var result = converter.ConvertToHtml(docxInputStream);
            htmlString = result.Value;
        }
        catch (Exception e)
        {
            throw new PluginApplicationException("Conversion failed. Please check your file. Error message: " + e.Message);
        }

        var htmlBytes = Encoding.UTF8.GetBytes(htmlString);
        var htmlStream = new MemoryStream(htmlBytes);

        htmlStream.Position = 0;
        var uploadedFile = await _fileManagementClient.UploadAsync(htmlStream, "text/html", request.File.Name + ".html");
        return new ConvertTextToDocumentResponse { File = uploadedFile};    
    }

    private async Task<ConvertTextToDocumentResponse> ConvertToTextFile(string text, string filename, string contentType, Encoding encoding, bool includeBom)
    {
        var contentBytes = encoding.GetBytes(text ?? string.Empty);
        if (includeBom)
        {
            var preamble = encoding.GetPreamble();
            if (preamble?.Length > 0)
            {
                var combined = new byte[preamble.Length + contentBytes.Length];
                Buffer.BlockCopy(preamble, 0, combined, 0, preamble.Length);
                Buffer.BlockCopy(contentBytes, 0, combined, preamble.Length, contentBytes.Length);
                contentBytes = combined;
            }
        }


        var file = await _fileManagementClient.UploadAsync(new MemoryStream(contentBytes), contentType, filename);

        return new ConvertTextToDocumentResponse { File = file };
    }

    private static (Encoding Encoding, bool IncludeBom) ResolveEncoding(string? encodingKey)
    {
        return encodingKey?.ToLower() switch
        {
            "utf8bom" => (new UTF8Encoding(true), true),
            "utf16le" => (new UnicodeEncoding(false, true), true),
            _ => (new UTF8Encoding(false), false)
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
        return await ErrorWrapperExecute.ExecuteSafelyAsync(async () =>
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var doc = new HtmlDocument();
            using (var reader = new StreamReader(memoryStream))
            {
                var htmlContent = await reader.ReadToEndAsync();
                doc.LoadHtml(htmlContent);
            }

            var text = doc.DocumentNode.InnerText;
            return Regex.Replace(text, @"\s+", " ").Trim();
        });
    }

    private static async Task<string> ReadPlaintextFile(Stream file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var stringBuilder = new StringBuilder();
        using (var reader = new StreamReader(memoryStream))
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
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return await ErrorWrapperExecute.ExecuteSafelyAsync(async () =>
        {
            using var document = PdfDocument.Open(memoryStream);
            return string.Join(" ", document.GetPages().Select(p => p.Text));
        });
    }

    private static async Task<string> ReadDocxFile(Stream file)
    {
        return await ErrorWrapperExecute.ExecuteSafelyAsync(async () =>
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var document = WordprocessingDocument.Open(memoryStream, false);
            return document.MainDocumentPart.Document.Body.InnerText;
        });
    }

    private static int CountWords(string text)
    {
        char[] punctuationCharacters = text.Where(char.IsPunctuation).Distinct().ToArray();
        var words = text.Split().Select(x => x.Trim(punctuationCharacters));
        return words.Where(x => !string.IsNullOrWhiteSpace(x)).Count();
    }
}