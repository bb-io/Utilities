using Apps.Utilities.ErrorWrapper;
using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Shared;
using Apps.Utilities.Models.Texts;
using Apps.Utilities.Utils.DocumentReader;
using Apps.Utilities.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Bilingual.Xliff2;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ICSharpCode.SharpZipLib.Zip;
using Mammoth;
using System.IO.Compression;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace Apps.Utilities.Actions;

[ActionList("Files")]
public class Files(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseInvocable(invocationContext)
{
    [Action("Pseudolocalize files", Description = "Pseudolocalizes source text into targets and returns XLIFF 2.2 files. Processes one file at a time, preserving inline codes and placeholders.")]
    public async Task<PseudolocalizeFilesResponse> PseudolocalizeFiles(
        [ActionParameter] PseudolocalizeFilesRequest request)
    {
        var files = request?.Files?.ToList();
        if (files is null || files.Count == 0 || files.Any(file => file is null || string.IsNullOrWhiteSpace(file.Name)))
            throw new PluginMisconfigurationException("Provide at least one file, with a name for every file.");
        var expansion = request!.ExpansionPercentage ?? 0;
        if (expansion is < 0 or > 1000)
            throw new PluginMisconfigurationException("Expansion percentage must be an integer from 0 to 1000.");

        var states = new HashSet<SegmentState>();
        foreach (var value in (request.StatesToPseudolocalize ?? []).Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var state = value.Trim().ToLowerInvariant().ToSegmentState();
            if (state is null)
                throw new PluginMisconfigurationException($"Invalid segment state '{value}'. Select Initial, Translated, Reviewed, or Final.");
            states.Add(state.Value);
        }
        if (states.Count == 0)
            states.Add(SegmentState.Initial);

        var response = new PseudolocalizeFilesResponse();
        foreach (var file in files)
        {
            try
            {
                await using var input = await fileManagementClient.DownloadAsync(file);
                var result = Transformation.Load(input, file.Name, file.ContentType);
                if (!result.Success)
                    throw new PluginMisconfigurationException($"The file could not be parsed by Blackbird Filters. Details: {result.Error}");

                var transformation = result.Value;
                if (string.IsNullOrWhiteSpace(transformation.SourceLanguage))
                    transformation.SourceLanguage = "en";
                Pseudolocalizer.Apply(transformation, states, expansion);

                var extension = Path.GetExtension(file.Name);
                var outputName = extension.Equals(".xlf", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".xliff", StringComparison.OrdinalIgnoreCase)
                        ? file.Name : file.Name + ".xlf";
                const string contentType = "application/xliff+xml";
                await using var output = new MemoryStream(Encoding.UTF8.GetBytes(
                    Xliff2Serializer.Serialize(transformation, Xliff2Version.Xliff22)));
                var uploaded = await fileManagementClient.UploadAsync(output, contentType, outputName);
                uploaded.Name = outputName;
                uploaded.ContentType = contentType;
                response.Files.Add(uploaded);
            }
            catch (PluginMisconfigurationException ex)
            {
                throw new PluginMisconfigurationException($"Could not pseudolocalize '{file.Name}'. {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException($"Could not pseudolocalize '{file.Name}'. {ex.Message}");
            }
        }
        return response;
    }

    [Action("Change file encoding", Description = "Changes a text file's encoding while preserving its text, name, and extension.")]
    public async Task<FileDto> ChangeFileEncoding([ActionParameter] ChangeFileEncodingRequest request)
    {
        if (request?.File is null)
            throw new PluginMisconfigurationException("File is required.");

        var targetEncoding = ResolveFileEncoding(request.TargetEncoding);
        var sourceEncoding = string.IsNullOrWhiteSpace(request.SourceEncoding)
            ? null
            : ResolveFileEncoding(request.SourceEncoding);

        await using var input = await fileManagementClient.DownloadAsync(request.File);
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer);
        var bytes = buffer.ToArray();

        // Check UTF-32 before UTF-16 because their little-endian BOMs share a prefix.
        Encoding[] bomEncodings =
        [
            new UTF32Encoding(false, true, true),
            new UTF32Encoding(true, true, true),
            new UTF8Encoding(true, true),
            new UnicodeEncoding(false, true, true),
            new UnicodeEncoding(true, true, true)
        ];
        var detectedEncoding = bomEncodings.FirstOrDefault(encoding =>
            bytes.AsSpan().StartsWith(encoding.GetPreamble()));
        if (sourceEncoding is not null && detectedEncoding is not null &&
            sourceEncoding.CodePage != detectedEncoding.CodePage)
            throw new PluginMisconfigurationException("Source encoding does not match the file's BOM.");

        sourceEncoding ??= detectedEncoding ?? new UTF8Encoding(false, true);
        var offset = detectedEncoding?.GetPreamble().Length ?? 0;
        byte[] outputBytes;
        try
        {
            var text = sourceEncoding.GetString(bytes, offset, bytes.Length - offset);
            outputBytes = targetEncoding.GetPreamble().Concat(targetEncoding.GetBytes(text)).ToArray();
        }
        catch (DecoderFallbackException)
        {
            throw new PluginMisconfigurationException(
                "The file contains invalid bytes for the source encoding. Specify the correct source encoding and provide a text file.");
        }
        catch (EncoderFallbackException)
        {
            throw new PluginMisconfigurationException("The file contains characters that cannot be represented in the target encoding.");
        }

        using var output = new MemoryStream(outputBytes);
        var contentType = string.IsNullOrWhiteSpace(request.File.ContentType)
            ? MediaTypeNames.Text.Plain
            : request.File.ContentType;
        var uploaded = await fileManagementClient.UploadAsync(output, contentType, request.File.Name);
        uploaded.ContentType = contentType;
        return new FileDto { File = uploaded };
    }

    private static Encoding ResolveFileEncoding(string? key) => key?.Trim().ToLowerInvariant() switch
    {
        "utf8" => new UTF8Encoding(false, true),
        "utf8bom" => new UTF8Encoding(true, true),
        "utf16le" => new UnicodeEncoding(false, true, true),
        _ => throw new PluginMisconfigurationException("Select a supported encoding: UTF-8 (no BOM), UTF-8 with BOM, or UTF-16LE.")
    };

    [Action("Get file name information", 
        Description = "Returns the name of a file, with or without extension, and the extension.")]
    public NameResponse GetFileName([ActionParameter] FileDto file)
    {
        return new NameResponse
        {
            NameWithoutExtension = Path.GetFileNameWithoutExtension(file.File.Name),
            NameWithExtension = Path.GetFileName(file.File.Name),
            Extension = Path.GetExtension(file.File.Name),
            FullPath = file.File.Name
        };
    }

    [Action("Get file size", Description = "Returns the size of a file in bytes.")]
    public async Task<double> GetFileSize([ActionParameter] FileDto file)
    {
        var fileStream = await fileManagementClient.DownloadAsync(file.File);
        var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream.Length;
    }

    [Action("Convert document to text",
        Description = "Load document's text. Document must be in docx/doc, pdf or any plaintext format.")]
    public async Task<LoadDocumentResponse> LoadDocument([ActionParameter] LoadDocumentRequest request)
    {
        var file = await fileManagementClient.DownloadAsync(request.File);
        var extension = Path.GetExtension(request.File.Name).ToLower();

        var reader = DocumentReaderFactory.GetReader(extension);
        var content = await reader.Read(file);
        return new(content);
    }

    [Action("Change file name", Description = "Rename a file (without extension).")]
    public FileDto ChangeFileName([ActionParameter] FileDto file, [ActionParameter] RenameRequest input)
    {
        var extension = Path.GetExtension(file.File.Name);
        var newFileName = input.Name + extension;

        if (ContainsLineBreak(newFileName))
            throw new PluginMisconfigurationException("File name cannot contain line breaks.");

        file.File.Name = newFileName;
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

    private static bool ContainsLineBreak(string value)
        => value.Contains('\r') || value.Contains('\n');

    [Action("Get file character count", Description = "Returns number of characters in the file")]
    public async Task<int> GetCharCountInFile([ActionParameter] FileDto file)
    {
        var stream = await fileManagementClient.DownloadAsync(file.File);

        var extension = Path.GetExtension(file.File.Name).ToLower();
        IDocumentReader reader = DocumentReaderFactory.GetReader(extension);

        string fileContent = await reader.Read(stream);
        return fileContent.Length;
    }

    [Action("Get file word count", Description = "Returns number of words in the file")]
    public Task<double> GetWordCountInFile(
        [ActionParameter] FileDto file,
        [ActionParameter] XliffWordCountOptions? xliffOptions = null)
    {
        return GetWordCount(file.File, xliffOptions?.SegmentStates);
    }

    [Action("Get files word count", Description = "Returns number of words in the files")]
    public async Task<FilesWordCountResponse> GetWordCountInFiles(
        [ActionParameter] FilesWordCountRequest request,
        [ActionParameter] XliffWordCountOptions? xliffOptions = null)
    {
        double totalWordCount = 0;
        var files = new List<WordCountItem>();
        foreach (var file in request.Files)
        {
            var wordCount = await GetWordCount(file, xliffOptions?.SegmentStates);
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

    private async Task<double> GetWordCount(FileReference file, IEnumerable<string>? rawSegmentStates)
    {
        await using var downloadedStream = await fileManagementClient.DownloadAsync(file);
        await using var bufferedStream = new MemoryStream();
        await downloadedStream.CopyToAsync(bufferedStream);

        if (TryDeserializeXliff(bufferedStream, out var transformation))
        {
            var segmentStates = ParseSegmentStates(rawSegmentStates);
            var sourceText = string.Join(
                Environment.NewLine,
                transformation.GetUnits()
                    .SelectMany(unit => unit.Segments)
                    .Where(segment => segmentStates.Count == 0
                        || segmentStates.Contains(segment.State ?? SegmentState.Initial))
                    .Select(segment => string.Concat(
                        segment.Source
                            .Where(part => part is not InlineTag)
                            .Select(part => part.Value))));

            return CountWords(sourceText);
        }

        if (IsDeclaredAsXliff(file))
        {
            throw new PluginMisconfigurationException(
                "The provided file is not a valid XLIFF file.");
        }

        bufferedStream.Position = 0;
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        var reader = DocumentReaderFactory.GetReader(extension);
        var fileContent = await reader.Read(bufferedStream);

        return CountWords(fileContent);
    }

    private static HashSet<SegmentState> ParseSegmentStates(IEnumerable<string>? rawSegmentStates)
    {
        var parsedStates = (rawSegmentStates ?? [])
            .Where(state => !string.IsNullOrWhiteSpace(state))
            .Select(SegmentStateHelper.ToSegmentState)
            .ToList();

        if (parsedStates.Any(state => state is null))
        {
            throw new PluginMisconfigurationException(
                "One or more segment statuses are invalid. Please select supported segment statuses.");
        }

        return parsedStates
            .Select(state => state!.Value)
            .ToHashSet();
    }

    private static bool TryDeserializeXliff(Stream stream, out Transformation transformation)
    {
        stream.Position = 0;
        if (Xliff2Serializer.IsXliff2(stream, out var xliff2Node))
        {
            transformation = Xliff2Serializer.Deserialize(xliff2Node);
            return true;
        }

        stream.Position = 0;
        if (Xliff1Serializer.IsXliff1(stream, out var xliff1Node))
        {
            transformation = Xliff1Serializer.Deserialize(xliff1Node);
            return true;
        }

        transformation = null!;
        return false;
    }

    private static bool IsDeclaredAsXliff(FileReference file)
    {
        var extension = Path.GetExtension(file.Name);
        return extension.Equals(".xlf", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".xliff", StringComparison.OrdinalIgnoreCase)
            || file.ContentType?.Contains("xliff", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static double CountWords(string text)
    {
        var punctuationCharacters = text.Where(char.IsPunctuation).Distinct().ToArray();
        var words = text.Split().Select(word => word.Trim(punctuationCharacters));
        return words.Count(word => !string.IsNullOrWhiteSpace(word));
    }

    [Action("Replace using Regex in document", Description = "Replace text in a document using Regex. Works only with text based files (txt, html, etc.). Action is pretty similar to 'Replace using Regex' but works with files")]
    public async Task<ReplaceTextInDocumentResponse> ReplaceTextInDocument(
        [ActionParameter] ReplaceTextInDocumentRequest request)
    {
        if (request?.File == null)
            throw new PluginMisconfigurationException("File is required.");

        if (string.IsNullOrWhiteSpace(request.Regex))
            throw new PluginMisconfigurationException("Regex pattern cannot be null or empty.");

        try
        {
            await using var fileStream = await fileManagementClient.DownloadAsync(request.File);

            using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            var text = await reader.ReadToEndAsync();

            string replacedText;
            try
            {
                replacedText = Regex.Replace(text, request.Regex,request.ExprimentalRegexField is not null ? Regex.Unescape(request.ExprimentalRegexField) : request.Replace ?? string.Empty);
            }
            catch (RegexParseException ex)
            {
                throw new PluginMisconfigurationException($"Error in regular expression: {ex.Message}", ex);
            }
            catch (ArgumentException ex)
            {
                throw new PluginMisconfigurationException($"Error: {ex.Message}", ex);
            }

            var bytes = Encoding.UTF8.GetBytes(replacedText);
            await using var uploadStream = new MemoryStream(bytes);

            var contentType = string.IsNullOrWhiteSpace(request.File.ContentType)
                ? "text/plain"
                : request.File.ContentType;

            var uploaded = await fileManagementClient.UploadAsync(uploadStream, contentType, request.File.Name);

            return new ReplaceTextInDocumentResponse { File = uploaded };
        }
        catch (HttpRequestException ex)
        {
            var details = ex.InnerException?.Message ?? ex.Message;
            throw new PluginApplicationException($"File service request failed: {details}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new PluginApplicationException("File service request timed out while downloading or uploading the file.", ex);
        }
    }

    [Action("Replace multiple Regex patterns in document", Description = "Use Regular Expressions to search and replace multiple patterns within text. Works only with text based files (txt, html, etc.).")]
    public async Task<ReplaceTextInDocumentResponse> ReplaceMultipleTextsInDocument(
        [ActionParameter] FileDto request,
        [ActionParameter] RegexReplaceMultipleInput regex)
    {
        if (regex.RegexPatterns.Count() != regex.Replacements.Count())
            throw new PluginMisconfigurationException("The number of regex patterns must match the number of replacement strings.");

        if (regex.RegexPatterns.Any(string.IsNullOrEmpty))
            throw new PluginMisconfigurationException("Regex patterns cannot contain empty strings.");

        if (regex.Replacements.Any(string.IsNullOrEmpty))
            throw new PluginMisconfigurationException("Replacement strings cannot contain empty strings.");

        await using var download = await fileManagementClient.DownloadAsync(request.File);
        using var reader = new StreamReader(download);
        var result = await reader.ReadToEndAsync();

        var regexPairs = regex.RegexPatterns.Zip(regex.Replacements, (r, rep) => new { Regex = r, Replace = rep });

        foreach (var pair in regexPairs)
        {
            try
            {
                var pattern = new Regex(pair.Regex);
                result = pattern.Replace(result, pair.Replace);
            }
            catch (ArgumentException ex)
            {
                throw new PluginApplicationException($"Error replacing '{pair.Regex}' with '{pair.Replace}': {ex.Message}");
            }
        }

        return new()
        {
            File = await fileManagementClient.UploadAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(result)),
                request.File.ContentType,
                request.File.Name)
        };
    }

    [Action("Extract using Regex from document", Description = "Extract text from a document using Regex. Works only with text based files (txt, html, etc.). Action is pretty similar to 'Extract using Regex' but works with files")]
    public async Task<ExtractTextFromDocumentResponse> ExtractTextFromDocument(
        [ActionParameter] ExtractTextFromDocumentRequest request)
    {
        request.Validate();

        await using var file = await fileManagementClient.DownloadAsync(request.File);
        using var reader = new StreamReader(file);
        var text = await reader.ReadToEndAsync();
            
        text = string.IsNullOrEmpty(request.Group)
            ? Regex.Match(text, request.Regex).Value
            : Regex.Match(text, request.Regex).Groups[request.Group].Value;

        return new() { ExtractedText = text };
    }

    [Action("Extract many using Regex from document",
        Description = "Extract multiple text matches from a document using regular expressiosn. Works only with text-based files (txt, html, etc.)")]
    public async Task<IEnumerable<string>> ExtractManyTextFromDocument([ActionParameter] ExtractTextFromDocumentRequest request)
    {
        request.Validate();

        await using var file = await fileManagementClient.DownloadAsync(request.File);
        using var reader = new StreamReader(file);
        var text = await reader.ReadToEndAsync();

        var matches = Regex.Matches(text, request.Regex);

        var extracted = matches
            .Cast<Match>()
            .Where(m => m.Success)
            .Select(m =>
                string.IsNullOrEmpty(request.Group)
                    ? m.Value
                    : m.Groups[request.Group].Value
            )
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();

        return extracted;
    }

    [Action("Convert text to document", Description = "Convert text to txt, html, json, csv, doc or docx document.")]
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
    public async Task<CompareContentResults> CompareFileContents([ActionParameter] CompareFilesRequest request)
    {
        string? currentContent = null;

        foreach (var file in request.Files)
        {
            var stream = await fileManagementClient.DownloadAsync(file);

            var extension = Path.GetExtension(file.Name);
            var reader = DocumentReaderFactory.GetReader(extension);
            var filecontent = await reader.Read(stream);

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
        if (request?.Files is null)
        {
            throw new PluginMisconfigurationException(
                "Files are required. Please provide at least one text file.");
        }

        var files = request.Files.ToList();
        if (files.Count == 0)
        {
            throw new PluginMisconfigurationException(
                "At least one text file must be provided for concatenation.");
        }

        if (files.Any(file => file is null))
        {
            throw new PluginMisconfigurationException(
                "The files list contains an invalid file reference. Please re-map the input files.");
        }

        var firstFile = files[0];
        var extension = Path.GetExtension(firstFile.Name);
        var mimeType = firstFile.ContentType;

        var encoding = Encoding.UTF8;

        var outputStream = new MemoryStream();

        using (var outputWriter = new StreamWriter(outputStream, encoding, leaveOpen: true))
        {
            foreach (var fileRef in files)
            {
                var file = await fileManagementClient.DownloadAsync(fileRef);

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

        var uploadedFile = await fileManagementClient.UploadAsync(
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

        var file = await fileManagementClient.DownloadAsync(request.File);
        var files = new List<FileDto>();

        using (var seekableStream = new MemoryStream())
        {
            await file.CopyToAsync(seekableStream);
            seekableStream.Position = 0;

            using (var zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(seekableStream))
            {
                foreach (ZipEntry entry in zip)
                {
                    if (!entry.CanDecompress || entry.IsDirectory)
                        continue;

                    await using var entryStream = zip.GetInputStream(entry);
                    using var buffer = new MemoryStream();
                    await entryStream.CopyToAsync(buffer);
                    buffer.Position = 0;
                    
                    // The core engine will fail without this: it will try to locate a file in the storage blob using this path
                    // Path.GetFileName(entry.Name) will cause collisions because a file with the same name
                    // can be located in different folders of the same archive
                    string fileName = entry.Name.Replace('/', '_');

                    var uploadedFile = await fileManagementClient.UploadAsync(buffer, MimeTypes.GetMimeType(fileName), fileName);
                    files.Add(new FileDto { File = uploadedFile });
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
                var inputStream = await fileManagementClient.DownloadAsync(fileRef);
                var entry = zip.CreateEntry(fileRef.Name, CompressionLevel.Optimal);

                using var entryStream = entry.Open();
                await inputStream.CopyToAsync(entryStream);
            }
        }

        archiveStream.Position = 0;
        var zipFileDto = await fileManagementClient.UploadAsync(
            archiveStream,
            "application/zip",
            $"archive_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip"
        );

        return new FileDto { File = zipFileDto };
    }

    [Action("Convert HTML file to DOCX", Description = "Converts an html file into a docx file")]
    public async Task<ConvertTextToDocumentResponse> ConvertHtmlToDocx([ActionParameter] FileDto request)
    {
        if (!request.File.Name.EndsWith(".html") && !request.File.Name.EndsWith(".htm"))
            throw new PluginMisconfigurationException("The input file must be an html file.");

        await using var htmlInputStream = await fileManagementClient.DownloadAsync(request.File);
        string htmlString;
        using (var reader = new StreamReader(htmlInputStream))
        {
            htmlString = await reader.ReadToEndAsync();
        }

        try
        {
            using var memStream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(memStream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                var converter = new HtmlToOpenXml.HtmlConverter(mainPart);
                var paragraphs = converter.Parse(htmlString);
                var body = mainPart.Document.Body;
                foreach (var p in paragraphs)
                    body.Append(p);

                mainPart.Document.Save();
            }

            memStream.Position = 0;
            var uploadedFile = await fileManagementClient.UploadAsync(memStream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", request.File.Name + ".docx");

            return new ConvertTextToDocumentResponse { File = uploadedFile };
        }
        catch (Exception e)
        {
            throw new PluginApplicationException("Conversion failed. Error message: " + e.Message);
        }
    }

    [Action("Count file pages", Description = "Counts pages in PDF and DOCX files and returns total page count.")]
    public async Task<PageCountResponse> CountPdfPages([ActionParameter] FilesToZipRequest files)
    {
        var response = new PageCountResponse();

        foreach (var fileRef in files.Files)
        {
            try
            {
                await using var inputStream = await fileManagementClient.DownloadAsync(fileRef);
                using var memoryStream = new MemoryStream();
                await inputStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var ext = Path.GetExtension(fileRef.Name)?.ToLowerInvariant();
                int pages = ext switch
                {
                    ".pdf" => GetPdfPages(memoryStream),
                    ".docx" => GetDocxPages(memoryStream)
                               ?? throw new PluginApplicationException(
                                   $"Unable to determine page count for {fileRef.Name}. " +
                                   $"The DOCX doesn't contain the built-in Pages property. " +
                                   $"Open & re-save in Word or convert to PDF, then try again."),
                    _ => throw new PluginApplicationException($"Unsupported file type for {fileRef.Name}. Only PDF and DOCX are supported.")
                };

                response.Files.Add(new PageCountResult
                {
                    FileName = fileRef.Name,
                    PageCount = pages
                });

                response.TotalPages += pages;
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException($"There was a problem processing file {fileRef.Name}. Error: {ex.Message}");
            }
        }

        return response;
    }

    private static int GetPdfPages(Stream stream)
    {
        stream.Position = 0;
        using var pdf = PdfDocument.Open(stream);
        return pdf.NumberOfPages;
    }

    private static int? GetDocxPages(Stream stream)
    {
        stream.Position = 0;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var appXml = archive.GetEntry("docProps/app.xml");
        if (appXml == null) return null;

        using var appStream = appXml.Open();
        var xdoc = XDocument.Load(appStream);

        XNamespace ep = "http://schemas.openxmlformats.org/officeDocument/2006/extended-properties";
        var pagesElem = xdoc.Root?.Element(ep + "Pages");
        if (pagesElem == null) return null;

        return int.TryParse(pagesElem.Value, out var pages) ? pages : null;
    }

    [Action("Convert docx file to html", Description = "Converts a docx file into an html file")]
    public async Task<ConvertTextToDocumentResponse> ConvertDocxToHtml([ActionParameter] FileDto request)
    {
        if (!request.File.Name.EndsWith(".doc") && !request.File.Name.EndsWith(".docx"))
            throw new PluginMisconfigurationException("The input file must be a doc or docx.");

        var docxInputStream = await fileManagementClient.DownloadAsync(request.File);
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
        var uploadedFile = await fileManagementClient.UploadAsync(htmlStream, "text/html", request.File.Name + ".html");
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


        var file = await fileManagementClient.UploadAsync(new MemoryStream(contentBytes), contentType, filename);

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
        var file = await fileManagementClient.UploadAsync(stream,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document", filename);

        return new ConvertTextToDocumentResponse
        {
            File = file
        };
    }
}
