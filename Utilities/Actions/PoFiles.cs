using Blackbird.Applications.Sdk.Common;
using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Po;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Apps.Utilities.Actions;

[ActionList("PO files")]
public class PoFiles(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseInvocable(invocationContext)
{
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



    [Action("Get PO header", Description = "Outputs the value of a named PO header. Header names are matched without case; a missing header causes an error.")]
    public async Task<PoHeaderValueResponse> GetPoHeader([ActionParameter] FileDto file, [ActionParameter] PoHeaderRequest request)
    {
        ValidateHeaderName(request.HeaderName);
        var document = await ReadHeader(file);
        var header = ParseHeaders(document.Value).FirstOrDefault(x =>
            string.Equals(x.Name, request.HeaderName, StringComparison.OrdinalIgnoreCase));
        if (header == null)
            throw new PluginMisconfigurationException($"PO header '{request.HeaderName}' was not found.");
        return new PoHeaderValueResponse { HeaderValue = header.Value };
    }

    [Action("Set PO header", Description = "Updates a named PO header or appends it if missing, and outputs the modified file and all headers.")]
    public async Task<SetPoHeaderResponse> SetPoHeader([ActionParameter] FileDto file, [ActionParameter] SetPoHeaderRequest request)
    {
        ValidateHeaderName(request.HeaderName);
        if (request.HeaderValue == null || request.HeaderValue.Contains('\n') || request.HeaderValue.Contains('\r'))
            throw new PluginMisconfigurationException("Header value must be a single line.");
        var document = await ReadHeader(file);
        var lines = document.Value.Split('\n').ToList();
        var index = lines.FindIndex(line =>
        {
            var colon = line.IndexOf(':');
            return colon >= 0 && string.Equals(line[..colon].Trim(), request.HeaderName, StringComparison.OrdinalIgnoreCase);
        });
        var newLine = $"{request.HeaderName}: {request.HeaderValue}";
        if (index >= 0)
            lines[index] = newLine;
        else
        {
            if (lines.Count > 0 && lines[^1] == "") lines.RemoveAt(lines.Count - 1);
            lines.Add(newLine);
        }
        var value = string.Join("\n", lines);
        if (!value.EndsWith('\n')) value += "\n";
        var newline = document.Text.Contains("\r\n") ? "\r\n" : "\n";
        var serialized = "msgstr \"\"" + newline + string.Join(newline,
            value[..^1].Split('\n').Select(line =>
                "\"" + EscapePo(line) + "\\n\""));
        var updated = document.Text[..document.Start] + serialized + document.Text[document.End..];
        var bytes = document.Encoding.GetPreamble().Concat(document.Encoding.GetBytes(updated)).ToArray();
        using var stream = new MemoryStream(bytes);
        var contentType = string.IsNullOrWhiteSpace(file.File.ContentType) ? "text/x-gettext-translation" : file.File.ContentType;
        var uploaded = await fileManagementClient.UploadAsync(stream, contentType, file.File.Name);
        uploaded.ContentType = contentType;
        return new SetPoHeaderResponse { File = uploaded, Headers = ParseHeaders(value) };
    }

    private static void ValidateHeaderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name != name.Trim() || name.IndexOfAny([':', '\r', '\n']) >= 0)
            throw new PluginMisconfigurationException("Header name must be nonempty and cannot contain a colon or line break.");
    }

    private static List<PoHeader> ParseHeaders(string value) => value.Split('\n')
        .Select(line => (Line: line.TrimEnd('\r'), Colon: line.IndexOf(':')))
        .Where(x => x.Colon > 0)
        .Select(x => new PoHeader { Name = x.Line[..x.Colon].Trim(), Value = x.Line[(x.Colon + 1)..].TrimStart(' ', '\t') })
        .ToList();

    private static string EscapePo(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\t", "\\t").Replace("\r", "\\r");

    private async Task<(string Text, Encoding Encoding, int Start, int End, string Value)> ReadHeader(FileDto file)
    {
        if (file?.File == null) throw new PluginMisconfigurationException("File is required.");
        if (!Path.GetExtension(file.File.Name).Equals(".po", StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("The input file must be a PO file.");
        await using var input = await fileManagementClient.DownloadAsync(file.File);
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        using var reader = new StreamReader(new MemoryStream(bytes), new UTF8Encoding(false, true), true);
        var text = await reader.ReadToEndAsync();
        var encoding = reader.CurrentEncoding;
        if (!bytes.AsSpan().StartsWith(encoding.GetPreamble())) encoding = new UTF8Encoding(false, true);
        var entryPattern = @"(?m)^[ \t]*msgid[ \t]+""""[ \t]*\r?\n[ \t]*msgstr[ \t]+(?<first>""(?:\\.|[^""\\])*"")[ \t]*(?:\r?\n[ \t]*""(?:\\.|[^""\\])*""[ \t]*)*";
        foreach (Match match in Regex.Matches(text, entryPattern))
        {
            var before = text[..match.Index];
            var boundary = Regex.Matches(before, @"\r?\n[ \t]*\r?\n").Cast<Match>().LastOrDefault();
            var entryPrefix = before[(boundary == null ? 0 : boundary.Index + boundary.Length)..];
            if (Regex.IsMatch(entryPrefix, @"(?m)^[ \t]*msgctxt\b")) continue;
            var strStart = text.IndexOf("msgstr", match.Index, StringComparison.Ordinal);
            var strText = text[strStart..(match.Index + match.Length)];
            var chunks = Regex.Matches(strText, @"""(?:\\.|[^""\\])*""").Cast<Match>();
            string value;
            try { value = string.Concat(chunks.Select(x => JsonConvert.DeserializeObject<string>(x.Value))); }
            catch (JsonException) { throw new PluginMisconfigurationException("The PO header contains an invalid quoted string."); }
            return (text, encoding, strStart, match.Index + match.Length, value);
        }
        throw new PluginMisconfigurationException("The PO file does not contain a valid header entry.");
    }

}
