using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Apps.Utilities.Extensions;
using Apps.Utilities.Models.Shared;
using Apps.Utilities.Utils;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Bilingual.Xliff2;

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

        [Action("Remove units from XLIFF", Description = "Removes XLIFF units by segment state, empty targets, or quality threshold. The resulting XLIFF cannot be merged back into a target file.")]
        public async Task<RemoveXliffUnitsResponse> RemoveXliffUnits([ActionParameter] RemoveXliffUnitsRequest request)
        {
            if (request.QualityThresholdLimit is not null
                && (!double.IsFinite(request.QualityThresholdLimit.Value)
                    || request.QualityThresholdLimit is < 0 or > 100))
            {
                throw new PluginMisconfigurationException("Quality threshold limit must be between 0 and 100.");
            }

            var rawStates = (request.SegmentStates ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (rawStates.Count == 0)
                rawStates.Add(SegmentStateHelper.Serialize(SegmentState.Final));

            var selectedStates = rawStates
                .Select(SegmentStateHelper.ToSegmentState)
                .ToList();

            if (selectedStates.Any(x => x is null))
                throw new PluginMisconfigurationException("One or more segment states are invalid.");

            var stateSet = selectedStates
                .Select(x => x!.Value)
                .ToHashSet();

            await using var inputStream = await fileManagementClient.DownloadAsync(request.File);
            var (transformation, xliffSerializer) = await SerializeXliffVersion(inputStream);
            var units = transformation.GetUnits().ToList();

            var unitsMatchingState = new HashSet<Unit>();
            var unitsWithEmptyTargets = new HashSet<Unit>();
            var unitsUnderQualityThreshold = new HashSet<Unit>();
            var qualityFilterEnabled = request.RemoveUnitsUnderQualityThreshold == true
                || request.QualityThresholdLimit.HasValue;

            foreach (var unit in units)
            {
                if (unit.Segments.Count > 0
                    && unit.Segments.All(segment => stateSet.Contains(segment.State ?? SegmentState.Initial)))
                {
                    unitsMatchingState.Add(unit);
                }

                if (request.RemoveUnitsWithEmptyTargets == true
                    && unit.Segments.Count > 0
                    && unit.Segments.All(segment => string.IsNullOrWhiteSpace(segment.GetTarget())))
                {
                    unitsWithEmptyTargets.Add(unit);
                }

                if (qualityFilterEnabled
                    && unit.Quality.Score.HasValue)
                {
                    var threshold = request.QualityThresholdLimit ?? unit.Quality.ScoreThreshold;
                    if (threshold.HasValue && unit.Quality.Score.Value < threshold.Value)
                        unitsUnderQualityThreshold.Add(unit);
                }
            }

            var unitsToRemove = unitsMatchingState
                .Concat(unitsWithEmptyTargets)
                .Concat(unitsUnderQualityThreshold)
                .ToHashSet();

            var totalSegmentsBefore = units.Sum(unit => unit.Segments.Count);
            var removedSegmentsByState = unitsMatchingState.Sum(unit => unit.Segments.Count);
            var removedSegmentsWithEmptyTarget = unitsWithEmptyTargets.Sum(unit => unit.Segments.Count);
            var removedSegmentsUnderQualityThreshold = unitsUnderQualityThreshold.Sum(unit => unit.Segments.Count);

            RemoveUnits(transformation, unitsToRemove);

            if (request.StripSkeleton != false)
                RemoveSkeleton(transformation);

            var totalSegmentsAfter = transformation.GetUnits().Sum(unit => unit.Segments.Count);
            var processedXliff = xliffSerializer(transformation);
            await using var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(processedXliff));
            var outputFile = await fileManagementClient.UploadAsync(
                outputStream,
                request.File.ContentType ?? "application/xliff+xml",
                request.File.Name);

            return new RemoveXliffUnitsResponse
            {
                File = outputFile,
                TotalSegmentsBefore = totalSegmentsBefore,
                TotalSegmentsAfter = totalSegmentsAfter,
                RemovedSegmentsByState = removedSegmentsByState,
                RemovedSegmentsWithEmptyTarget = removedSegmentsWithEmptyTarget,
                RemovedSegmentsUnderQualityThreshold = removedSegmentsUnderQualityThreshold,
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

            await using var originalXliffStream = await fileManagementClient.DownloadAsync(request.File);
            var (transformation, xliffSerializer) = await SerializeXliffVersion(originalXliffStream);
            
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
            var transformation = stream.LoadTransformation(fileInput.File.Name);

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

        [Action("Check character limits", Description = "Checks target text against XLIFF character limits using Unicode grapheme length.")]
        public async Task<CheckXliffCharacterLimitsResponse> CheckCharacterLimits([ActionParameter] CheckXliffCharacterLimitsRequest request)
        {
            await using var stream = await fileManagementClient.DownloadAsync(request.File);
            var transformation = stream.LoadTransformation(request.File.Name);

            var units = transformation.GetUnits().ToList();
            var selectedStates = (request.SegmentStatesToInclude ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(SegmentStateHelper.ToSegmentState)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToHashSet();

            var response = new CheckXliffCharacterLimitsResponse
            {
                TotalUnits = units.Count
            };

            foreach (var unit in units)
            {
                if (selectedStates.Count > 0 && !unit.Segments.Any(s => selectedStates.Contains(s.State ?? SegmentState.Initial)))
                    continue;

                response.UnitsMatchingStateFilter++;

                var maximumSize = unit.SizeRestrictions?.MaximumSize;
                if (maximumSize is null)
                    continue;

                response.TotalUnitsWithLimits++;

                var targetText = string.Join(string.Empty, unit.GetTarget().Parts.Where(p => p is not InlineCode).Select(p => p.Value));
                var currentLength = StringInfo.ParseCombiningCharacters(targetText).Length;
                var maximumLength = Convert.ToInt32(maximumSize.Value);

                if (currentLength <= maximumLength)
                    continue;

                response.Units.Add(new XliffCharacterLimitUnit
                {
                    UnitId = unit.Id ?? string.Empty,
                    MaximumLength = maximumLength,
                    CurrentLength = currentLength
                });
            }

            response.UnitsOverLimits = response.Units.Count;

            return response;
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
                await using var fileStream = await fileManagementClient.DownloadAsync(request.File);
                var transformation = fileStream.LoadTransformation(request.File.Name);

                transformation.SourceLanguage = string.IsNullOrWhiteSpace(request.SourceLanguage) ? "en" : request.SourceLanguage;
                if (!string.IsNullOrWhiteSpace(request.TargetLanguage))
                    transformation.TargetLanguage = request.TargetLanguage;

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
                await using var stream = await fileManagementClient.DownloadAsync(request.File);
                var transformation = stream.LoadTransformation(request.File.Name);

                string html;

                if (!string.IsNullOrWhiteSpace(transformation.Original))
                {
                    var codedContentLoadResult = transformation.Target();
                    if (!codedContentLoadResult.Success)
                        throw new PluginMisconfigurationException(codedContentLoadResult.Error);

                    var codedContent = codedContentLoadResult.Value;
                    if (codedContent.TextUnits.All(x => string.IsNullOrWhiteSpace(x.GetPlainText())))
                    {
                        var sourceLoadResult = transformation.Source();
                        if (!sourceLoadResult.Success)
                            throw new PluginMisconfigurationException(sourceLoadResult.Error);
                                
                        codedContent = sourceLoadResult.Value;
                    }

                    await using var codedStream = codedContent.ToStream();
                    using var reader = new StreamReader(codedStream);
                    html = await reader.ReadToEndAsync();
                }
                else
                    html = BuildSimpleHtmlFromTransformation(transformation);

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
            await using var originalXliffStream = await fileManagementClient.DownloadAsync(request.File);
            var (transformation, xliffSerializer) = await SerializeXliffVersion(originalXliffStream);

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
            await using var stream = await fileManagementClient.DownloadAsync(request.File);
            var transformation = stream.LoadTransformation(request.File.Name);

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

        private static void RemoveUnits(Transformation transformation, ISet<Unit> unitsToRemove)
        {
            for (var index = transformation.Children.Count - 1; index >= 0; index--)
            {
                switch (transformation.Children[index])
                {
                    case Unit unit when unitsToRemove.Contains(unit):
                        transformation.Children.RemoveAt(index);
                        break;
                    case Blackbird.Filters.Transformations.Group group:
                        RemoveUnits(group, unitsToRemove);
                        break;
                    case Transformation childTransformation:
                        RemoveUnits(childTransformation, unitsToRemove);
                        break;
                }
            }
        }

        private static void RemoveUnits(Blackbird.Filters.Transformations.Group group, ISet<Unit> unitsToRemove)
        {
            for (var index = group.Children.Count - 1; index >= 0; index--)
            {
                switch (group.Children[index])
                {
                    case Unit unit when unitsToRemove.Contains(unit):
                        group.Children.RemoveAt(index);
                        break;
                    case Blackbird.Filters.Transformations.Group childGroup:
                        RemoveUnits(childGroup, unitsToRemove);
                        break;
                }
            }
        }

        private static void RemoveSkeleton(Transformation transformation)
        {
            transformation.Original = null;
            transformation.OriginalReference = null;

            foreach (var childTransformation in transformation.Children.OfType<Transformation>())
                RemoveSkeleton(childTransformation);
        }

        private static async Task<VersionSerializationResult> SerializeXliffVersion(Stream fileStream)
        {
            using var xliffStream = new MemoryStream();
            await fileStream.CopyToAsync(xliffStream);

            Transformation transformation;
            Func<Transformation, string> xliffSerializer;

            if (Xliff2Serializer.IsXliff2(xliffStream, out var xliff2Node))
            {
                var version = xliff2Node.Attribute("version")?.Value;
                var xliff2Version = version?.ToXliff2Version() ?? 
                                    throw new PluginMisconfigurationException($"XLIFF version {version} is not supported.");

                transformation = Xliff2Serializer.Deserialize(xliff2Node);
                xliffSerializer = t => Xliff2Serializer.Serialize(t, xliff2Version);
            }
            else if (Xliff1Serializer.IsXliff1(xliffStream, out var xliff1Node))
            {
                transformation = Xliff1Serializer.Deserialize(xliff1Node);
                xliffSerializer = Xliff1Serializer.Serialize;
            }
            else
                throw new PluginMisconfigurationException("The provided file is not a valid XLIFF file.");

            return new(transformation, xliffSerializer);
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
