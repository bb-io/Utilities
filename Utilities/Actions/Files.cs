using Apps.Utilities.ErrorWrapper;
using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Shared;
using Apps.Utilities.Models.Texts;
using Apps.Utilities.Utils.DocumentReader;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using DocumentFormat.OpenXml;
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

    [Action("Copy PO keys to translations",
        Description = "Copies each PO source key into its translation while preserving all other file content.")]
    public async Task<FileDto> CopyPoKeysToTranslations([ActionParameter] FileDto request)
    {
        if (request?.File is null)
            throw new PluginMisconfigurationException("File is required.");

        if (!Path.GetExtension(request.File.Name).Equals(".po", StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("The input file must be a PO file.");

        await using var input = await fileManagementClient.DownloadAsync(request.File);
        using var reader = new StreamReader(
            input,
            new UTF8Encoding(false, true),
            detectEncodingFromByteOrderMarks: true);
        var inputText = await reader.ReadToEndAsync();

        var completedLines = new Queue<string>();
        var lineStart = 0;
        for (var index = 0; index < inputText.Length; index++)
        {
            if (inputText[index] == '\r')
            {
                if (index + 1 < inputText.Length && inputText[index + 1] == '\n')
                    index++;

                completedLines.Enqueue(inputText[lineStart..(index + 1)]);
                lineStart = index + 1;
            }
            else if (inputText[index] == '\n')
            {
                completedLines.Enqueue(inputText[lineStart..(index + 1)]);
                lineStart = index + 1;
            }
        }

        if (lineStart < inputText.Length)
            completedLines.Enqueue(inputText[lineStart..]);

        var output = new StringBuilder(inputText.Length);
        var entryLines = new List<string>();
        var foundPoEntry = false;

        while (true)
        {
            var currentLine = completedLines.Count > 0 ? completedLines.Dequeue() : null;
            var isBoundary = currentLine is null;

            if (currentLine is not null)
            {
                var contentLength = GetPoLineContentLength(currentLine);

                isBoundary = true;
                for (var index = 0; index < contentLength; index++)
                {
                    if (currentLine[index] is not (' ' or '\t'))
                    {
                        isBoundary = false;
                        break;
                    }
                }
            }

            if (!isBoundary)
            {
                entryLines.Add(currentLine!);
                continue;
            }

            if (entryLines.Count > 0)
            {
                var directives =
                    new List<(string Kind, int PluralIndex, int StartLine, int EndLine, int FirstQuote, bool IsEmpty)>();

                for (var lineIndex = 0; lineIndex < entryLines.Count; lineIndex++)
                {
                    var line = entryLines[lineIndex];
                    var contentLength = GetPoLineContentLength(line);

                    var position = 0;
                    while (position < contentLength && line[position] is ' ' or '\t')
                        position++;

                    if (position >= contentLength || line[position] is '#' or '"')
                        continue;

                    string? kind = null;
                    var pluralIndex = -1;
                    var keywordEnd = position;

                    if (line.IndexOf("msgid_plural", position, StringComparison.Ordinal) == position)
                    {
                        kind = "id_plural";
                        keywordEnd += "msgid_plural".Length;
                    }
                    else if (line.IndexOf("msgid", position, StringComparison.Ordinal) == position)
                    {
                        kind = "id";
                        keywordEnd += "msgid".Length;
                    }
                    else if (line.IndexOf("msgctxt", position, StringComparison.Ordinal) == position)
                    {
                        kind = "context";
                        keywordEnd += "msgctxt".Length;
                    }
                    else if (line.IndexOf("msgstr[", position, StringComparison.Ordinal) == position)
                    {
                        var numberStart = position + "msgstr[".Length;
                        var numberEnd = numberStart;
                        while (numberEnd < contentLength && line[numberEnd] is >= '0' and <= '9')
                            numberEnd++;

                        if (numberEnd == numberStart ||
                            numberEnd >= contentLength ||
                            line[numberEnd] != ']' ||
                            !int.TryParse(line[numberStart..numberEnd], out pluralIndex))
                        {
                            throw new PluginMisconfigurationException(
                                $"Invalid plural translation directive at line {lineIndex + 1} of a PO entry.");
                        }

                        kind = "str_plural";
                        keywordEnd = numberEnd + 1;
                    }
                    else if (line.IndexOf("msgstr", position, StringComparison.Ordinal) == position)
                    {
                        kind = "str";
                        keywordEnd += "msgstr".Length;
                    }

                    if (kind is null)
                        continue;

                    if (keywordEnd >= contentLength ||
                        line[keywordEnd] is not (' ' or '\t'))
                    {
                        throw new PluginMisconfigurationException(
                            $"Invalid {kind} directive at line {lineIndex + 1} of a PO entry.");
                    }

                    var firstQuote = keywordEnd;
                    while (firstQuote < contentLength && line[firstQuote] is ' ' or '\t')
                        firstQuote++;

                    if (firstQuote >= contentLength || line[firstQuote] != '"')
                    {
                        throw new PluginMisconfigurationException(
                            $"Missing quoted value at line {lineIndex + 1} of a PO entry.");
                    }

                    var endLine = lineIndex;
                    var isEmpty = true;

                    while (true)
                    {
                        var stringLine = entryLines[endLine];
                        var stringContentLength = GetPoLineContentLength(stringLine);

                        var quoteStart = endLine == lineIndex ? firstQuote : 0;
                        if (endLine != lineIndex)
                        {
                            while (quoteStart < stringContentLength &&
                                   stringLine[quoteStart] is ' ' or '\t')
                            {
                                quoteStart++;
                            }
                        }

                        if (quoteStart >= stringContentLength || stringLine[quoteStart] != '"')
                            throw new PluginMisconfigurationException(
                                $"Invalid continued string at line {endLine + 1} of a PO entry.");

                        var closingQuote = -1;
                        for (var quoteIndex = quoteStart + 1;
                             quoteIndex < stringContentLength;
                             quoteIndex++)
                        {
                            if (stringLine[quoteIndex] != '"')
                                continue;

                            var backslashCount = 0;
                            for (var escapeIndex = quoteIndex - 1;
                                 escapeIndex > quoteStart && stringLine[escapeIndex] == '\\';
                                 escapeIndex--)
                            {
                                backslashCount++;
                            }

                            if (backslashCount % 2 == 0)
                            {
                                closingQuote = quoteIndex;
                                break;
                            }
                        }

                        if (closingQuote < 0)
                            throw new PluginMisconfigurationException(
                                $"Unterminated quoted value at line {endLine + 1} of a PO entry.");

                        for (var suffixIndex = closingQuote + 1;
                             suffixIndex < stringContentLength;
                             suffixIndex++)
                        {
                            if (stringLine[suffixIndex] is not (' ' or '\t'))
                                throw new PluginMisconfigurationException(
                                    $"Unexpected content after quoted value at line {endLine + 1} of a PO entry.");
                        }

                        if (closingQuote > quoteStart + 1)
                            isEmpty = false;

                        if (endLine + 1 >= entryLines.Count)
                            break;

                        var nextLine = entryLines[endLine + 1];
                        var nextContentLength = GetPoLineContentLength(nextLine);

                        var nextPosition = 0;
                        while (nextPosition < nextContentLength &&
                               nextLine[nextPosition] is ' ' or '\t')
                        {
                            nextPosition++;
                        }

                        if (nextPosition >= nextContentLength || nextLine[nextPosition] != '"')
                            break;

                        endLine++;
                    }

                    directives.Add((kind, pluralIndex, lineIndex, endLine, firstQuote, isEmpty));
                    lineIndex = endLine;
                }

                var ids = directives.Where(x => x.Kind == "id").ToList();
                if (ids.Count > 1)
                    throw new PluginMisconfigurationException("A PO entry contains more than one msgid.");

                if (ids.Count == 0)
                {
                    foreach (var line in entryLines)
                        output.Append(line);
                }
                else
                {
                    var id = ids[0];
                    var contexts = directives.Count(x => x.Kind == "context");
                    var idPlurals = directives.Where(x => x.Kind == "id_plural").ToList();
                    var singularTargets = directives.Where(x => x.Kind == "str").ToList();
                    var pluralTargets = directives.Where(x => x.Kind == "str_plural").ToList();

                    if (contexts > 1)
                        throw new PluginMisconfigurationException("A PO entry contains more than one msgctxt.");
                    if (idPlurals.Count > 1)
                        throw new PluginMisconfigurationException("A PO entry contains more than one msgid_plural.");

                    var isHeader = contexts == 0 && id.IsEmpty && idPlurals.Count == 0;
                    if (isHeader)
                    {
                        if (singularTargets.Count != 1 || pluralTargets.Count > 0)
                            throw new PluginMisconfigurationException("PO header must contain one msgstr.");

                        foundPoEntry = true;
                        foreach (var line in entryLines)
                            output.Append(line);
                    }
                    else
                    {
                        if (idPlurals.Count == 0)
                        {
                            if (singularTargets.Count != 1 || pluralTargets.Count > 0)
                                throw new PluginMisconfigurationException(
                                    "A singular PO entry must contain one msgstr.");
                        }
                        else
                        {
                            if (singularTargets.Count > 0 || pluralTargets.Count == 0)
                                throw new PluginMisconfigurationException(
                                    "A plural PO entry must contain msgstr[n] translations.");

                            if (pluralTargets.Select(x => x.PluralIndex).Distinct().Count() != pluralTargets.Count)
                                throw new PluginMisconfigurationException(
                                    "A plural PO entry contains duplicate msgstr indexes.");
                        }

                        foundPoEntry = true;
                        var replacementTargets = singularTargets.Concat(pluralTargets)
                            .OrderBy(x => x.StartLine)
                            .ToList();
                        var nextOutputLine = 0;

                        foreach (var target in replacementTargets)
                        {
                            while (nextOutputLine < target.StartLine)
                            {
                                output.Append(entryLines[nextOutputLine]);
                                nextOutputLine++;
                            }

                            var source = target.Kind == "str" || target.PluralIndex == 0
                                ? id
                                : idPlurals[0];
                            var targetFirstLine = entryLines[target.StartLine];
                            var sourceFirstLine = entryLines[source.StartLine];

                            var sourceFirstContentLength = GetPoLineContentLength(sourceFirstLine);

                            var targetLastLine = entryLines[target.EndLine];
                            var targetLastContentLength = GetPoLineContentLength(targetLastLine);

                            output.Append(targetFirstLine, 0, target.FirstQuote);
                            output.Append(sourceFirstLine,
                                source.FirstQuote,
                                sourceFirstContentLength - source.FirstQuote);

                            if (source.EndLine > source.StartLine)
                            {
                                var sourceFirstTerminatorLength =
                                    sourceFirstLine.Length - sourceFirstContentLength;
                                output.Append(sourceFirstLine,
                                    sourceFirstContentLength,
                                    sourceFirstTerminatorLength);

                                for (var sourceLineIndex = source.StartLine + 1;
                                     sourceLineIndex <= source.EndLine;
                                     sourceLineIndex++)
                                {
                                    var sourceLine = entryLines[sourceLineIndex];
                                    var sourceContentLength = GetPoLineContentLength(sourceLine);

                                    output.Append(sourceLine, 0, sourceContentLength);

                                    if (sourceLineIndex < source.EndLine)
                                    {
                                        output.Append(sourceLine,
                                            sourceContentLength,
                                            sourceLine.Length - sourceContentLength);
                                    }
                                    else
                                    {
                                        output.Append(targetLastLine,
                                            targetLastContentLength,
                                            targetLastLine.Length - targetLastContentLength);
                                    }
                                }
                            }
                            else
                            {
                                output.Append(targetLastLine,
                                    targetLastContentLength,
                                    targetLastLine.Length - targetLastContentLength);
                            }

                            nextOutputLine = target.EndLine + 1;
                        }

                        while (nextOutputLine < entryLines.Count)
                        {
                            output.Append(entryLines[nextOutputLine]);
                            nextOutputLine++;
                        }
                    }
                }

                entryLines.Clear();
            }

            if (currentLine is not null)
            {
                output.Append(currentLine);
                continue;
            }

            if (!foundPoEntry)
                throw new PluginMisconfigurationException(
                    "The input file does not contain a valid PO msgid/msgstr entry.");

            break;
        }

        using var uploadStream = new MemoryStream();
        await using (var writer = new StreamWriter(
                         uploadStream,
                         new UTF8Encoding(false),
                         leaveOpen: true))
        {
            await writer.WriteAsync(output.ToString());
        }
        uploadStream.Position = 0;

        var contentType = string.IsNullOrWhiteSpace(request.File.ContentType)
            ? "text/x-gettext-translation"
            : request.File.ContentType;
        var uploaded = await fileManagementClient.UploadAsync(uploadStream, contentType, request.File.Name);
        uploaded.ContentType = contentType;

        return new FileDto { File = uploaded };
    }

    private static int GetPoLineContentLength(string line)
    {
        var contentLength = line.Length;
        if (contentLength > 0 && line[contentLength - 1] == '\n')
            contentLength--;
        if (contentLength > 0 && line[contentLength - 1] == '\r')
            contentLength--;
        return contentLength;
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
    public async Task<double> GetWordCountInFile([ActionParameter] FileDto file)
    {
        var stream = await fileManagementClient.DownloadAsync(file.File);

        var extension = Path.GetExtension(file.File.Name).ToLowerInvariant();
        IDocumentReader reader = DocumentReaderFactory.GetReader(extension);

        string fileContent = await reader.Read(stream);

        char[] punctuationCharacters = fileContent.Where(char.IsPunctuation).Distinct().ToArray();
        var words = fileContent.Split().Select(x => x.Trim(punctuationCharacters));
        return words.Count(x => !string.IsNullOrWhiteSpace(x));
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
        var firstFile = request.Files.FirstOrDefault();
        var extension = Path.GetExtension(firstFile.Name);
        var mimeType = firstFile.ContentType;

        var encoding = Encoding.UTF8;

        var outputStream = new MemoryStream();

        using (var outputWriter = new StreamWriter(outputStream, encoding, leaveOpen: true))
        {
            foreach (var fileRef in request.Files)
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

                        var uploadedFile = await fileManagementClient.UploadAsync(
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
