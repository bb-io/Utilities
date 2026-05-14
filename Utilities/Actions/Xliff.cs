using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff1;
using Blackbird.Filters.Xliff.Xliff2;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Apps.Utilities.Utils;
using DocumentFormat.OpenXml.Presentation;

namespace Apps.Utilities.Actions
{
    [ActionList("XLIFF")]
    public class Xliff(IFileManagementClient fileManagementClient)
    {
        [Action("Replace XLIFF source with target", Description = "Swap <source> and <target> contents, exchange language attributes, and optionally remove target elements or set a new target language.")]
        public async Task<ConvertTextToDocumentResponse> ReplaceXliffSourceWithTarget([ActionParameter] ReplaceXliffRequest request)
        {
            var doc = await DocumentLoader.LoadXDocument(request.File, fileManagementClient, LoadOptions.PreserveWhitespace, "XLIFF");
            XNamespace ns = doc.Root.GetDefaultNamespace();

            var transUnits = doc.Descendants(ns + "trans-unit").ToList();
            foreach (var transUnit in transUnits)
            {
                var sourceElement = transUnit.Element(ns + "source");
                var targetElement = transUnit.Element(ns + "target");

                if (sourceElement == null || targetElement == null)
                    continue;

                var sourceNodes = sourceElement.Nodes().ToList();
                var targetNodes = targetElement.Nodes().ToList();

                sourceElement.RemoveNodes();
                targetElement.RemoveNodes();

                sourceElement.Add(targetNodes);
                targetElement.Add(sourceNodes);

                if (request.DeleteTargets == true)
                {
                    targetElement.Remove();
                }
            }

            XElement fileElement = doc.Root.Element(ns + "file");
            if (fileElement != null)
            {
                var sourceLangAttr = fileElement.Attribute("source-language");
                var targetLangAttr = fileElement.Attribute("target-language");

                if (sourceLangAttr != null && targetLangAttr != null)
                {
                    string originalSourceLang = sourceLangAttr.Value;
                    string originalTargetLang = targetLangAttr.Value;

                    if (!string.IsNullOrEmpty(request.SetNewSourceLanguage))
                    {
                        sourceLangAttr.Value = request.SetNewSourceLanguage;
                    }
                    else
                    {
                        sourceLangAttr.Value = originalTargetLang;
                    }

                    if (!string.IsNullOrEmpty(request.SetNewTargetLanguage))
                    {
                        targetLangAttr.Value = request.SetNewTargetLanguage;
                    }
                    else
                    {
                        targetLangAttr.Value = originalSourceLang;
                    }
                }
            }
            using var streamOut = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineHandling = NewLineHandling.Replace
            };
            using (var writer = XmlWriter.Create(streamOut, settings))
            {
                doc.Save(writer);
            }
            streamOut.Position = 0;

            var resultFile = await fileManagementClient.UploadAsync(streamOut, request.File.ContentType, request.File.Name);
            return new ConvertTextToDocumentResponse { File = resultFile };
        }

        [Action("Confirm and lock final targets", Description = "Set confirmed and locked attributes to 1 for translation units with target state 'final', and remove the target state attribute in mxliff files.")]
        public async Task<ConvertTextToDocumentResponse> ConfirmAndLockFinalTargets([ActionParameter] ConvertTextToDocumentResponse request)
        {
            var doc = await DocumentLoader.LoadXDocument(request.File, fileManagementClient, LoadOptions.PreserveWhitespace, "XLIFF");
            XNamespace ns = doc.Root.GetDefaultNamespace();
            XNamespace mNs = "http://www.memsource.com/mxlf/2.0";

            var transUnits = doc.Descendants(ns + "trans-unit").ToList();

            foreach (var transUnit in transUnits)
            {
                var targetElement = transUnit.Element(ns + "target");
                if (targetElement == null) continue;

                var stateAttr = targetElement.Attribute("state");
                if (stateAttr != null && stateAttr.Value == "final")
                {
                    var confirmedAttr = transUnit.Attribute(mNs + "confirmed");
                    if (confirmedAttr != null)
                    {
                        confirmedAttr.Value = "1";
                    }
                    else
                    {
                        transUnit.Add(new XAttribute(mNs + "confirmed", "1"));
                    }

                    var lockedAttr = transUnit.Attribute(mNs + "locked");
                    if (lockedAttr != null)
                    {
                        lockedAttr.Value = "1";
                    }
                    else
                    {
                        transUnit.Add(new XAttribute(mNs + "locked", "1"));
                    }

                    stateAttr.Remove();
                }
            }

            using var streamOut = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineHandling = NewLineHandling.Replace
            };
            using (var writer = XmlWriter.Create(streamOut, settings))
            {
                doc.Save(writer);
            }

            streamOut.Position = 0;
            var resultFile = await fileManagementClient.UploadAsync(streamOut, request.File.ContentType, request.File.Name);

            return new ConvertTextToDocumentResponse
            {
                File = resultFile
            };
        }

        [Action("Add context notes to XLIFF", Description = "Adds notes with optional context to units containing segments not in 'final' state.")]
        public async Task<FileDto> AddNoteToXliff([ActionParameter] AddNoteToXliffRequest request)
        {
            request.RawStatesToProcess ??= [SegmentStateHelper.Serialize(SegmentState.Initial), SegmentStateHelper.Serialize(SegmentState.Translated), SegmentStateHelper.Serialize(SegmentState.Reviewed)];
            request.SurroundingUnitsToInclude ??= 3;
            request.IncludeSegmentState ??= true;
            request.IncludeQualityScore ??= true;
            request.IncludeSurroundingUnits ??= true;

            var statesToProcess = request.RawStatesToProcess
                .Select(SegmentStateHelper.ToSegmentState)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();

            if (request.IncludeSurroundingUnits == true && !statesToProcess.Any())
                throw new PluginMisconfigurationException("At least one segment state must be specified in both 'Segment states to add notes into' and 'Segment states to be added as note'.");

            var originalXliffStream = await fileManagementClient.DownloadAsync(request.File);
            var originalXliff = await originalXliffStream.ReadString();

            if (!Xliff2Serializer.TryGetXliffVersion(originalXliff, out var originalXliffVersion))
                throw new PluginMisconfigurationException("The provided file is not a valid XLIFF file.");

            Func<Transformation, string> xliffSerializer = originalXliffVersion switch
            {
                "1.2" => t => Xliff1Serializer.Serialize(t),
                ['2', ..] => t => Xliff2Serializer.Serialize(t, Xliff2VersionHelper.ToXliff2Version(originalXliffVersion) ?? throw new PluginMisconfigurationException($"XLIFF version {originalXliffVersion} is not supported.")),
                _ => throw new PluginMisconfigurationException($"XLIFF version {originalXliffVersion} is not supported."),
            };

            var transformation = Transformation.Parse(originalXliff, request.File.Name)
                ?? throw new PluginMisconfigurationException("Can't parse the provided XLIFF file.");

            var units = transformation.GetUnits().ToList();

            for ( var currentUnitIndex = 0; currentUnitIndex < units.Count; currentUnitIndex++ )
            { 
                var unit = units[currentUnitIndex];

                if (!unit.Segments.Any(s => statesToProcess.Contains(s.State ?? SegmentState.Initial)))
                    continue;

                foreach (var segment in unit.Segments.Where(s => statesToProcess.Contains(s.State ?? SegmentState.Initial)))
                {
                    var noteContent = new StringBuilder();

                    if (request.IncludeSurroundingUnits == true)
                    {
                        int start = Math.Max(0, currentUnitIndex - request.SurroundingUnitsToInclude.Value);

                        var prevUnits = units
                            .Skip(start)
                            .Take(currentUnitIndex - start)
                            .ToList();

                        var nextUnits = units
                            .Skip(currentUnitIndex + 1)
                            .Take(request.SurroundingUnitsToInclude.Value)
                            .ToList();

                        if (prevUnits.Any())
                        {
                            noteContent.AppendLine("Previous source text:");
                            prevUnits
                                .Select(u => string.Join(' ', u.GetSource().Parts.Where(x => x is not InlineCode).Select(x => x.Value)))
                                .Distinct()
                                .ToList().ForEach(t => noteContent.AppendLine(t));

                            noteContent.AppendLine();

                            noteContent.AppendLine("Previous target text:");
                            prevUnits
                                .Select(u => string.Join(' ', u.GetTarget().Parts.Where(x => x is not InlineCode).Select(x => x.Value)))
                                .ToList().ForEach(t => noteContent.AppendLine(t));
                        }

                        if (prevUnits.Any() && nextUnits.Any())
                            noteContent.AppendLine();

                        if (nextUnits.Any())
                        {
                            noteContent.AppendLine("Following source text:");
                            nextUnits
                                .Select(u => string.Join(' ', u.GetSource().Parts.Where(x => x is not InlineCode).Select(x => x.Value)))
                                .ToList().ForEach(t => noteContent.AppendLine(t));

                            noteContent.AppendLine();

                            noteContent.AppendLine("Following target text:");
                            nextUnits
                                .Select(u => string.Join(' ', u.GetTarget().Parts.Where(x => x is not InlineCode).Select(x => x.Value)))
                                .ToList().ForEach(t => noteContent.AppendLine(t));
                        }

                        if (noteContent.Length > 0)
                            noteContent.AppendLine();
                    }

                    if (request.IncludeSegmentState == true)
                    {
                        var stateSerialized = segment.State is not null
                            ? SegmentStateHelper.Serialize(segment.State.Value)
                            : "empty";
                        noteContent.AppendLine($"State: {stateSerialized}");
                    }

                    if (request.IncludeQualityScore == true && unit.Quality.Score is not null)
                        noteContent.AppendLine($"Quality score: {unit.Quality.Score:F3}");

                    if (noteContent.Length == 0)
                        continue;

                    var note = new Note(noteContent.ToString());

                    if (segment.Id is not null)
                        note.Reference = segment.Id;

                    if (note.Reference is null && unit.Notes.Any(n => n.Text == note.Text && n.Reference is null))
                        continue;

                    unit.Notes.Add(note);
                }
            }

            var processedXliff = xliffSerializer(transformation);
            var processedXliffStream = new MemoryStream(Encoding.UTF8.GetBytes(processedXliff));

            return new FileDto
            {
                File = await fileManagementClient.UploadAsync(processedXliffStream, "application/xliff+xml", request.File.Name)
            };
        }

        [Action("Extract context notes from XLIFF", Description = "Get notes for each segment")]
        public async Task<ExtractXliffNotesResponse> ExtractXliffNotes([ActionParameter] FileDto fileInput)
        {
            await using var stream = await fileManagementClient.DownloadAsync(fileInput.File);
            using var reader = new StreamReader(stream);
            var fileContent = await reader.ReadToEndAsync();

            var transformation = Transformation.Parse(fileContent, fileInput.File.Name) ?? 
                                 throw new PluginMisconfigurationException("The provided file is not a valid XLIFF file.");

            var segmentIds = new List<string>();
            var notes = new List<string>();
            foreach (var unit in transformation.GetUnits())
            {
                if (unit.Notes.Count == 0) 
                    continue;

                segmentIds.Add(unit.Id ?? string.Empty);
                notes.Add(string.Join("; ", unit.Notes.Select(x => x.Text)));
            }

            return new ExtractXliffNotesResponse(segmentIds, notes);
        }

        [Action("Move XLIFF content to notes", Description = "Move selected element text or attribute values into XLIFF notes.")]
        public async Task<FileDto> MoveXliffContentToNotes([ActionParameter] MoveXliffContentToNotesRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.XPath))
                throw new PluginMisconfigurationException("XPath is required.");

            request.RemoveSource ??= true;
            request.IncludeSourceNameInNote ??= true;
            
            var doc = await DocumentLoader.LoadXDocument(request.File, fileManagementClient, LoadOptions.PreserveWhitespace, "XLIFF");
            if (doc.Root is null || doc.Root.Name.LocalName != "xliff")
                throw new PluginMisconfigurationException("The provided file is not a valid XLIFF file.");

            XNamespace ns = doc.Root.GetDefaultNamespace();
            var version = doc.Root.Attribute("version")?.Value;
            var isXliff12 = version == "1.2";
            var isXliff2 = version?.StartsWith('2') == true;

            if (!isXliff12 && !isXliff2)
                throw new PluginMisconfigurationException($"XLIFF version {version} is not supported.");

            var nsManager = new XmlNamespaceManager(new NameTable());
            var xpathNamespace = string.IsNullOrWhiteSpace(request.Namespace)
                ? ns.NamespaceName
                : request.Namespace;

            if (!string.IsNullOrWhiteSpace(xpathNamespace))
                nsManager.AddNamespace("ns", xpathNamespace);

            var matchedElements = doc.XPathSelectElements(request.XPath, nsManager).ToList();
            if (!matchedElements.Any())
                throw new PluginMisconfigurationException("No elements found for the specified XPath.");

            var notesAdded = 0;

            foreach (var element in matchedElements)
            {
                var container = GetNoteContainer(element, ns, isXliff12);
                if (container is null)
                    continue;

                string? noteText;
                if (!string.IsNullOrWhiteSpace(request.Attribute))
                {
                    var attribute = FindAttribute(element, request.Attribute);
                    if (attribute is null || string.IsNullOrWhiteSpace(attribute.Value))
                        continue;

                    var attributeName = GetDisplayName(attribute);
                    noteText = request.IncludeSourceNameInNote == true
                        ? $"{attributeName}=\"{attribute.Value}\""
                        : attribute.Value;

                    if (request.RemoveSource == true)
                        attribute.Remove();
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(element.Value))
                        continue;

                    var elementName = GetDisplayName(element);
                    noteText = request.IncludeSourceNameInNote == true
                        ? $"{elementName}: {element.Value}"
                        : element.Value;

                    if (request.RemoveSource == true)
                        element.Remove();
                }

                if (string.IsNullOrWhiteSpace(noteText))
                    continue;

                if (AddNote(container, noteText, ns, isXliff12))
                    notesAdded++;
            }

            if (notesAdded == 0)
                throw new PluginMisconfigurationException("No note content was created from the selected XLIFF content.");

            using var streamOut = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineHandling = NewLineHandling.Replace
            };

            using (var writer = XmlWriter.Create(streamOut, settings))
            {
                doc.Save(writer);
            }

            streamOut.Position = 0;

            return new FileDto
            {
                File = await fileManagementClient.UploadAsync(streamOut, request.File.ContentType ?? "application/xliff+xml", request.File.Name)
            };
        }

        [Action("Convert HTML to XLIFF", Description = "Convert HTML file to XLIFF 2.2 format")]
        public async Task<ConvertTextToDocumentResponse> ConvertHtmlToXliff([ActionParameter] ConvertHtmlToXliffRequest request)
        {
            var ext = Path.GetExtension(request.File.Name)?.ToLowerInvariant();
            if (ext != ".html" && ext != ".htm")
            {
                throw new PluginMisconfigurationException(
                    $"Wrong format file: expected HTML (.html or .htm), not {ext}");
            }

            try
            {
                var htmlContent = await DownloadFileAsStringAsync(request.File);
                if (string.IsNullOrWhiteSpace(htmlContent))
                {
                    throw new PluginMisconfigurationException("The provided HTML file is empty.");
                }

                var codedContent = CodedContent.Parse(htmlContent, request.File.Name);

                codedContent.Language = string.IsNullOrWhiteSpace(request.SourceLanguage)
                    ? "en"
                    : request.SourceLanguage;

                var transformation = codedContent.CreateTransformation(
                    string.IsNullOrWhiteSpace(request.TargetLanguage) ? null : request.TargetLanguage);

                transformation.OriginalName = request.File.Name;
                transformation.OriginalMediaType = "text/html";

                var xliff = Xliff2Serializer.Serialize(transformation, Xliff2Version.Xliff22);
                var streamOut = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

                var fileName = Path.GetFileNameWithoutExtension(request.File.Name) + ".xlf";
                var resultFile = await fileManagementClient.UploadAsync(
                    streamOut,
                    "application/xliff+xml",
                    fileName);

                return new ConvertTextToDocumentResponse
                {
                    File = resultFile
                };
            }
            catch (PluginMisconfigurationException)
            {
                throw;
            }
            catch (XmlException ex)
            {
                throw new PluginMisconfigurationException($"Invalid HTML/XML-compatible content: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException("Error converting HTML to XLIFF 2.2", ex);
            }
        }

        [Action("Convert XLIFF to HTML", Description = "Convert XLIFF file to HTML file")]
        public async Task<ConvertTextToDocumentResponse> ConvertXliffToHtml([ActionParameter] ConvertXliffToHtmlRequest request)
        {
            var ext = Path.GetExtension(request.File.Name)?.ToLowerInvariant();
            if (ext != ".xliff" && ext != ".xlf")
            {
                throw new PluginMisconfigurationException(
                    $"Wrong format file: expected XLIFF (.xliff or .xlf), not {ext}");
            }

            try
            {
                var xliffContent = await DownloadFileAsStringAsync(request.File);
                if (string.IsNullOrWhiteSpace(xliffContent))
                {
                    throw new PluginMisconfigurationException("The provided XLIFF file is empty.");
                }

                var transformation = Transformation.Parse(xliffContent, request.File.Name);
                if (transformation == null)
                {
                    throw new PluginMisconfigurationException("Can't parse the provided XLIFF file.");
                }

                string html;

                try
                {
                    if (!string.IsNullOrWhiteSpace(transformation.Original))
                    {
                        var codedContent = transformation.Target();

                        if (codedContent.TextUnits.All(x => string.IsNullOrWhiteSpace(x.GetPlainText())))
                        {
                            codedContent = transformation.Source();
                        }

                        html = codedContent.Serialize();
                    }
                    else
                    {
                        html = BuildSimpleHtmlFromTransformation(transformation);
                    }
                }
                catch (NullReferenceException ex) when (ex.Message.Contains("Cannot convert to content, no original data found"))
                {
                    html = BuildSimpleHtmlFromTransformation(transformation);
                }

                html = EnsureHtmlDocument(html, transformation.OriginalName);

                var streamOut = new MemoryStream(Encoding.UTF8.GetBytes(html));
                var fileName = Path.GetFileNameWithoutExtension(request.File.Name) + ".html";

                var resultFile = await fileManagementClient.UploadAsync(
                    streamOut,
                    "text/html",
                    fileName);

                return new ConvertTextToDocumentResponse
                {
                    File = resultFile
                };
            }
            catch (PluginMisconfigurationException)
            {
                throw;
            }
            catch (XmlException ex)
            {
                throw new PluginMisconfigurationException($"Invalid XLIFF file: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException("Error converting XLIFF to HTML", ex);
            }
        }

        private async Task<string> DownloadFileAsStringAsync(FileReference file)
        {
            await using var stream = await fileManagementClient.DownloadAsync(file);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        private static string BuildSimpleHtmlFromTransformation(Transformation transformation)
        {
            var units = transformation.GetUnits().ToList();
            var parts = new List<string>();

            foreach (var unit in units)
            {
                var segments = unit.Segments.OrderBy(x => x.Order ?? int.MaxValue).ToList();

                foreach (var segment in segments)
                {
                    var text = segment.Target?.Any() == true
                        ? segment.GetTarget()
                        : segment.GetSource();

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    parts.Add($"<p>{System.Security.SecurityElement.Escape(text)}</p>");
                }
            }

            if (!parts.Any())
            {
                return "<p></p>";
            }

            return string.Join(Environment.NewLine, parts);
        }

        private static string EnsureHtmlDocument(string html, string? title = null)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return """
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
</head>
<body></body>
</html>
""";
            }

            var trimmed = html.TrimStart();

            if (trimmed.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                return html;
            }

            var safeTitle = System.Security.SecurityElement.Escape(
                string.IsNullOrWhiteSpace(title) ? "Converted HTML" : title);

            return $"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>{safeTitle}</title>
</head>
<body>
{html}
</body>
</html>
""";
        }

        [Action("Remove target text from XLIFF", Description = "Removes only inline text inside target. Optionally process only targets with a specific state.")]
        public async Task<ConvertTextToDocumentResponse> RemoveTargetText([ActionParameter] RemoveTargetTextRequest request)
        {
            if (request.File == null)
                throw new PluginMisconfigurationException("File is required.");

            var doc = await DocumentLoader.LoadXDocument(request.File, fileManagementClient, LoadOptions.PreserveWhitespace, "XLIFF");

            var statesFilter = (request.TargetStates ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var targets = doc.Descendants().Where(e => e.Name.LocalName == "target").ToList();

            foreach (var target in targets)
            {
                if (statesFilter.Count > 0)
                {
                    var segment = target.Ancestors().FirstOrDefault(a => a.Name.LocalName == "segment");
                    var segState = segment?.Attributes().FirstOrDefault(a => a.Name.LocalName == "state")?.Value;

                    var targetState = target.Attributes().FirstOrDefault(a => a.Name.LocalName == "state")?.Value;

                    var effectiveState = !string.IsNullOrEmpty(segState) ? segState : (targetState ?? string.Empty);
                    if (!statesFilter.Contains(effectiveState))
                        continue;
                }

                target.RemoveNodes();
            }

            using var streamOut = new MemoryStream();

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                OmitXmlDeclaration = false,
                Indent = false,
                NewLineHandling = NewLineHandling.None
            };

            using (var writer = XmlWriter.Create(streamOut, settings))
                doc.Save(writer);

            streamOut.Position = 0;

            var contentType = request.File.ContentType ?? "application/xliff+xml";
            var result = await fileManagementClient.UploadAsync(streamOut, contentType, request.File.Name);

            return new ConvertTextToDocumentResponse { File = result };
        }

        [Action("Copy source to target in XLIFF", Description = "Copies all source content into targets.")]
        public async Task<CopySourceToTargetResponse> CopySourceToTarget([ActionParameter] CopySourceToTargetRequest request)
        {
            var originalXliffStream = await fileManagementClient.DownloadAsync(request.File);
            var originalXliff = await originalXliffStream.ReadString();

            if (!Xliff2Serializer.TryGetXliffVersion(originalXliff, out var originalXliffVersion))
                throw new PluginMisconfigurationException("The provided file is not a valid XLIFF file.");

            Func<Transformation, string> xliffSerializer = originalXliffVersion switch
            {
                "1.2" => t => Xliff1Serializer.Serialize(t),
                ['2', ..] => t => Xliff2Serializer.Serialize(t, Xliff2VersionHelper.ToXliff2Version(originalXliffVersion) ?? throw new PluginMisconfigurationException($"XLIFF version {originalXliffVersion} is not supported.")),
                _ => throw new PluginMisconfigurationException($"XLIFF version {originalXliffVersion} is not supported."),
            };

            var transformation = Transformation.Parse(originalXliff, request.File.Name)
                ?? throw new PluginMisconfigurationException("Can't parse the provided XLIFF file.");

            var units = transformation.GetUnits().ToList();
            var segmentsCopied = 0;

            units.ForEach(u =>
            {
                u.Segments.ForEach(s =>
                {
                    if (s.Source == null)
                        return;

                    s.Target = s.Source.ToList();
                    segmentsCopied++;
                });
            });

            var processedXliff = xliffSerializer(transformation);
            var processedXliffStream = new MemoryStream(Encoding.UTF8.GetBytes(processedXliff));

            return new CopySourceToTargetResponse
            {
                File = await fileManagementClient.UploadAsync(processedXliffStream, "application/xliff+xml", request.File.Name),
                SegmentsCopied = segmentsCopied,
            };
        }

        [Action("Convert XLIFF to CSV", Description = "Convert XLIFF file to CSV file.")]
        public async Task<ConvertXliffToCsvResponse> ConvertXliffToCsv([ActionParameter] ConvertXliffToCsvRequest request)
        {
            using var stream = await fileManagementClient.DownloadAsync(request.File);
            using var reader = new StreamReader(stream);
            var fileContent = await reader.ReadToEndAsync();

            var transformation = Transformation.Parse(fileContent, request.File.Name)
                ?? throw new PluginMisconfigurationException("The provided file is not a valid XLIFF file.");

            var segments = new List<XliffSegmentDto>();
            long totalCharacters = 0;

            foreach (var unit in transformation.GetUnits())
            {
                foreach (var segment in unit.Segments)
                {
                    if (segment.IsIgnorbale) 
                        continue;

                    var resolvedId = !string.IsNullOrWhiteSpace(segment.Id) ? segment.Id : unit.Id;

                    string source = segment.GetSource();
                    string target = segment.GetTarget();

                    if (string.IsNullOrWhiteSpace(target)) 
                        target = source;

                    segments.Add(new XliffSegmentDto(resolvedId ?? string.Empty, source, target));

                    totalCharacters += source.Length + target.Length;
                }
            }

            if (segments.Count == 0)
                return new([]);

            int limit = request.BatchSize ?? 10000;
            int numberOfBatches = (int)Math.Ceiling((double)totalCharacters / limit);
            if (numberOfBatches < 1) 
                numberOfBatches = 1;

            long optimalBatchSize = (long)Math.Ceiling((double)totalCharacters / numberOfBatches);

            var outputFiles = new List<FileReference>();
            int currentSegmentIndex = 0;

            for (int i = 0; i < numberOfBatches; i++)
            {
                if (currentSegmentIndex >= segments.Count) 
                    break;

                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("id,source,target");

                long currentBatchChars = 0;

                while (currentSegmentIndex < segments.Count)
                {
                    var segment = segments[currentSegmentIndex];

                    var cleanId = EscapeCsv(RemoveInvalidXmlChars(segment.Id));
                    var cleanSource = EscapeCsv(RemoveInvalidXmlChars(segment.Source));
                    var cleanTarget = EscapeCsv(RemoveInvalidXmlChars(segment.Target));

                    csvBuilder.AppendLine($"{cleanId},{cleanSource},{cleanTarget}");

                    currentBatchChars += segment.CharacterCount;
                    currentSegmentIndex++;

                    if (i < numberOfBatches - 1 && currentBatchChars >= optimalBatchSize)
                        break;
                }

                using var outStream = new MemoryStream(Encoding.UTF8.GetBytes(csvBuilder.ToString()));
                var fileName = $"{Path.GetFileNameWithoutExtension(request.File.Name)}_part{i + 1}.csv";

                var csvFile = await fileManagementClient.UploadAsync(outStream, "text/csv", fileName);
                outputFiles.Add(csvFile);
            }

            return new(outputFiles);
        }

        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field)) 
                return string.Empty;

            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                return $"\"{field.Replace("\"", "\"\"")}\"";

            return field;
        }

        private static string RemoveInvalidXmlChars(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return Regex.Replace(text, @"[\u0000-\u0008\u000B\u000C\u000E-\u001F]", "");
        }

        private static XElement? GetNoteContainer(XElement element, XNamespace ns, bool isXliff12)
        {
            return isXliff12
                ? element.AncestorsAndSelf(ns + "trans-unit").FirstOrDefault()
                : element.AncestorsAndSelf(ns + "unit").FirstOrDefault();
        }

        private static XAttribute? FindAttribute(XElement element, string attributeName)
        {
            var normalizedName = attributeName.Contains(':')
                ? attributeName.Split(':', 2)[1]
                : attributeName;

            return element.Attributes()
                .FirstOrDefault(a =>
                    string.Equals(a.Name.LocalName, normalizedName, StringComparison.Ordinal) ||
                    string.Equals(GetDisplayName(a), attributeName, StringComparison.Ordinal));
        }

        private static bool AddNote(XElement container, string noteText, XNamespace ns, bool isXliff12)
        {
            if (isXliff12)
            {
                if (container.Elements(ns + "note").Any(n => n.Value == noteText))
                    return false;

                var note = new XElement(ns + "note", noteText);
                var insertionTarget = container.Elements()
                    .LastOrDefault(e => e.Name == ns + "note"
                        || e.Name == ns + "target"
                        || e.Name == ns + "source"
                        || e.Name == ns + "seg-source"
                        || e.Name == ns + "alt-trans"
                        || e.Name == ns + "bin-unit");

                if (insertionTarget is null)
                    container.AddFirst(note);
                else
                    insertionTarget.AddAfterSelf(note);

                return true;
            }

            var notes = container.Element(ns + "notes");
            if (notes is null)
            {
                notes = new XElement(ns + "notes");
                var insertionTarget = container.Elements()
                    .FirstOrDefault(e => e.Name == ns + "segment" || e.Name == ns + "ignorable");

                if (insertionTarget is null)
                    container.Add(notes);
                else
                    insertionTarget.AddBeforeSelf(notes);
            }

            if (notes.Elements(ns + "note").Any(n => n.Value == noteText))
                return false;

            notes.Add(new XElement(ns + "note", noteText));
            return true;
        }

        private static string GetDisplayName(XAttribute attribute)
        {
            var prefix = attribute.Parent?.GetPrefixOfNamespace(attribute.Name.Namespace);
            return string.IsNullOrWhiteSpace(prefix)
                ? attribute.Name.LocalName
                : $"{prefix}:{attribute.Name.LocalName}";
        }

        private static string GetDisplayName(XElement element)
        {
            var prefix = element.GetPrefixOfNamespace(element.Name.Namespace);
            return string.IsNullOrWhiteSpace(prefix)
                ? element.Name.LocalName
                : $"{prefix}:{element.Name.LocalName}";
        }
    }
}
