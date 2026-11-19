using Apps.Utilities.Models.Coti;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace Apps.Utilities.Actions;

[ActionList("Files")]
public class Coti(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseInvocable(invocationContext)
{
    [Action("Parse COTI file", Description = "Extracts translation files, packaged reference files, languages, and original archive paths from a COTI package.")]
    public async Task<ParseCotiResponse> ParseCotiFile([ActionParameter] CotiFileRequest request)
    {
        if (request?.File is null)
            throw new PluginMisconfigurationException("COTI file is required.");

        await using var packageStream = await DownloadToMemory(request.File);

        try
        {
            using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: true);
            var package = ReadPackage(archive);
            var response = new ParseCotiResponse
            {
                SourceLanguages = DistinctLanguages(package.Projects.Select(project => project.SourceLanguage)),
                TargetLanguages = DistinctLanguages(package.Projects.Select(project => project.TargetLanguage))
            };

            foreach (var project in package.Projects)
            {
                foreach (var translationFile in project.TranslationFiles)
                {
                    var entry = package.Entries[translationFile.ArchivePath];
                    var uploaded = await UploadEntry(entry, translationFile.MimeType);
                    response.TranslationFiles.Add(new CotiTranslationFile
                    {
                        File = uploaded,
                        OriginalArchivePath = translationFile.ArchivePath,
                        ProjectId = project.ProjectId,
                        SourceLanguage = project.SourceLanguage,
                        TargetLanguage = project.TargetLanguage
                    });
                }

                foreach (var referenceFile in project.ReferenceFiles)
                {
                    var entry = package.Entries[referenceFile.ArchivePath];
                    var uploaded = await UploadEntry(entry, referenceFile.MimeType);
                    response.ReferenceFiles.Add(new CotiReferenceFile
                    {
                        File = uploaded,
                        OriginalArchivePath = referenceFile.ArchivePath,
                        ProjectId = project.ProjectId,
                        ManifestFileReference = referenceFile.FileReference,
                        External = referenceFile.External,
                        MimeType = referenceFile.MimeType,
                        Encoding = referenceFile.Encoding,
                        Type = referenceFile.Type,
                        Description = referenceFile.Description,
                        TmsId = referenceFile.TmsId
                    });
                }
            }

            return response;
        }
        catch (PluginMisconfigurationException)
        {
            throw;
        }
        catch (InvalidDataException ex)
        {
            throw new PluginMisconfigurationException("The provided file is not a valid COTI ZIP package.", ex);
        }
        catch (XmlException ex)
        {
            throw new PluginMisconfigurationException($"A COTI.xml manifest is not valid XML. {ex.Message}", ex);
        }
    }

    [Action("Assemble COTI file", Description = "Creates a partial-delivery COTI package containing only projects for which completed translation files were supplied.")]
    public async Task<AssembleCotiResponse> AssembleCotiFile([ActionParameter] AssembleCotiRequest request)
    {
        if (request?.OriginalCotiFile is null)
            throw new PluginMisconfigurationException("Original COTI file is required.");

        var suppliedFiles = request.TranslatedFiles?.ToList() ?? [];
        if (suppliedFiles.Count == 0)
            throw new PluginMisconfigurationException("Provide at least one translated file.");

        var suppliedPaths = request.OriginalArchivePaths?.ToList() ?? [];
        if (suppliedFiles.Count != suppliedPaths.Count)
            throw new PluginMisconfigurationException(
                "Provide one original archive path for every translated file, in the same order.");

        await using var originalStream = await DownloadToMemory(request.OriginalCotiFile);

        try
        {
            using var originalArchive = new ZipArchive(originalStream, ZipArchiveMode.Read, leaveOpen: true);
            var package = ReadPackage(originalArchive);
            var declaredFiles = package.Projects
                .SelectMany(project => project.TranslationFiles.Select(file => new { Project = project, File = file }))
                .ToDictionary(item => item.File.ArchivePath, item => item, StringComparer.OrdinalIgnoreCase);

            var replacements = new Dictionary<string, FileReference>(StringComparer.OrdinalIgnoreCase);
            var selectedProjects = new HashSet<ProjectDescriptor>();

            for (var index = 0; index < suppliedFiles.Count; index++)
            {
                var suppliedFile = suppliedFiles[index];
                if (suppliedFile is null)
                    throw new PluginMisconfigurationException("Every translated file must contain a valid file reference.");

                var originalArchivePath = suppliedPaths[index];
                var archivePath = NormalizeRelativePath(originalArchivePath, "Original archive path");
                if (!declaredFiles.TryGetValue(archivePath, out var declaredFile))
                    throw new PluginMisconfigurationException(
                        $"Translated file path '{originalArchivePath}' is not declared by the original COTI package.");

                if (!replacements.TryAdd(archivePath, suppliedFile))
                    throw new PluginMisconfigurationException(
                        $"More than one translated file was supplied for archive path '{archivePath}'.");

                selectedProjects.Add(declaredFile.Project);
            }

            foreach (var project in selectedProjects)
            {
                var missingPaths = project.TranslationFiles
                    .Select(file => file.ArchivePath)
                    .Where(path => !replacements.ContainsKey(path))
                    .ToList();

                if (missingPaths.Count > 0)
                    throw new PluginMisconfigurationException(
                        $"Project '{project.ProjectId}' is incomplete. Provide translated files for: {string.Join(", ", missingPaths)}.");
            }

            using var outputStream = new MemoryStream();
            using (var outputArchive = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var originalEntry in originalArchive.Entries)
                {
                    var entryPath = NormalizeExistingEntryPath(originalEntry.FullName);
                    if (!selectedProjects.Any(project => BelongsToProject(entryPath, project.RootPath)))
                        continue;

                    var outputEntry = outputArchive.CreateEntry(originalEntry.FullName, CompressionLevel.Optimal);
                    outputEntry.LastWriteTime = originalEntry.LastWriteTime;
                    outputEntry.ExternalAttributes = originalEntry.ExternalAttributes;

                    if (originalEntry.FullName.EndsWith('/') || originalEntry.FullName.EndsWith('\\'))
                        continue;

                    await using var outputEntryStream = outputEntry.Open();
                    if (replacements.TryGetValue(entryPath, out var replacement))
                    {
                        await using var replacementStream = await fileManagementClient.DownloadAsync(replacement);
                        await replacementStream.CopyToAsync(outputEntryStream);
                    }
                    else
                    {
                        await using var originalEntryStream = originalEntry.Open();
                        await originalEntryStream.CopyToAsync(outputEntryStream);
                    }
                }
            }

            outputStream.Position = 0;
            var outputName = EnsureCotiExtension(request.OriginalCotiFile.Name);
            var uploaded = await fileManagementClient.UploadAsync(outputStream, "application/zip", outputName);
            uploaded.Name = outputName;
            uploaded.ContentType = "application/zip";
            return new AssembleCotiResponse { File = uploaded };
        }
        catch (PluginMisconfigurationException)
        {
            throw;
        }
        catch (InvalidDataException ex)
        {
            throw new PluginMisconfigurationException("The original file is not a valid COTI ZIP package.", ex);
        }
        catch (XmlException ex)
        {
            throw new PluginMisconfigurationException($"A COTI.xml manifest is not valid XML. {ex.Message}", ex);
        }
    }

    private async Task<MemoryStream> DownloadToMemory(FileReference file)
    {
        await using var downloaded = await fileManagementClient.DownloadAsync(file);
        var buffered = new MemoryStream();
        await downloaded.CopyToAsync(buffered);
        buffered.Position = 0;
        return buffered;
    }

    private async Task<FileReference> UploadEntry(ZipArchiveEntry entry, string? declaredMimeType)
    {
        await using var entryStream = entry.Open();
        using var buffer = new MemoryStream();
        await entryStream.CopyToAsync(buffer);
        buffer.Position = 0;

        var uploadName = entry.FullName.Replace('/', '_').Replace('\\', '_');
        var mimeType = string.IsNullOrWhiteSpace(declaredMimeType)
            ? MimeTypes.GetMimeType(uploadName)
            : declaredMimeType;
        var uploaded = await fileManagementClient.UploadAsync(buffer, mimeType, uploadName);
        uploaded.Name = uploadName;
        uploaded.ContentType = mimeType;
        return uploaded;
    }

    private static PackageDescriptor ReadPackage(ZipArchive archive)
    {
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries.Where(entry =>
                     !entry.FullName.EndsWith('/') && !entry.FullName.EndsWith('\\')))
        {
            var path = NormalizeExistingEntryPath(entry.FullName);
            if (!entries.TryAdd(path, entry))
                throw new PluginMisconfigurationException(
                    $"The COTI package contains duplicate archive path '{path}'.");
        }

        var manifestEntries = entries
            .Where(pair => GetFileName(pair.Key).Equals("COTI.xml", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (manifestEntries.Count == 0)
            throw new PluginMisconfigurationException("The COTI package does not contain a COTI.xml manifest.");

        var projects = manifestEntries
            .Select(pair => ReadProject(pair.Key, pair.Value, entries))
            .ToList();

        var duplicateProjectId = projects
            .GroupBy(project => project.ProjectId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateProjectId is not null)
            throw new PluginMisconfigurationException(
                $"The COTI package contains duplicate project ID '{duplicateProjectId.Key}'.");

        return new PackageDescriptor(entries, projects);
    }

    private static ProjectDescriptor ReadProject(
        string manifestPath,
        ZipArchiveEntry manifestEntry,
        IReadOnlyDictionary<string, ZipArchiveEntry> entries)
    {
        XDocument document;
        using (var manifestStream = manifestEntry.Open())
        using (var xmlReader = XmlReader.Create(manifestStream, new XmlReaderSettings
               {
                   DtdProcessing = DtdProcessing.Prohibit,
                   XmlResolver = null
               }))
            document = XDocument.Load(xmlReader, LoadOptions.None);

        var root = document.Root;
        if (root is null || !root.Name.LocalName.Equals("coti", StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException($"Manifest '{manifestPath}' does not have a coti root element.");

        var ns = root.Name.Namespace;
        var projectElements = root.Elements(ns + "project").ToList();
        if (projectElements.Count != 1)
            throw new PluginMisconfigurationException($"Manifest '{manifestPath}' must contain exactly one project.");
        var projectElement = projectElements[0];

        var translationElements = projectElement.Elements(ns + "translation").ToList();
        if (translationElements.Count != 1)
            throw new PluginMisconfigurationException($"Manifest '{manifestPath}' must contain exactly one translation.");
        var translationElement = translationElements[0];

        var projectId = RequiredAttribute(projectElement, "project-id", manifestPath);
        var sourceLanguage = RequiredAttribute(translationElement, "source-language", manifestPath);
        var targetLanguage = RequiredAttribute(translationElement, "target-language", manifestPath);
        var rootPath = GetDirectoryName(manifestPath);

        var translationFiles = translationElement.Elements(ns + "translation-file")
            .Select(element =>
            {
                var fileRef = RequiredAttribute(element, "file-ref", manifestPath);
                var archivePath = ResolveFileReference(rootPath, fileRef);
                EnsureEntryExists(entries, archivePath, manifestPath);
                return new TranslationFileDescriptor(
                    archivePath,
                    RequiredAttribute(element, "mime-type", manifestPath));
            })
            .ToList();

        if (translationFiles.Count == 0)
            throw new PluginMisconfigurationException($"Manifest '{manifestPath}' does not declare any translation files.");

        var duplicateTranslationPath = translationFiles
            .GroupBy(file => file.ArchivePath, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateTranslationPath is not null)
            throw new PluginMisconfigurationException(
                $"Manifest '{manifestPath}' declares translation file '{duplicateTranslationPath.Key}' more than once.");

        var referenceFiles = translationElement.Elements(ns + "reference-file")
            .Where(element => string.Equals((string?)element.Attribute("external"), "no", StringComparison.OrdinalIgnoreCase))
            .Select(element =>
            {
                var fileRef = RequiredAttribute(element, "file-ref", manifestPath);
                var external = RequiredAttribute(element, "external", manifestPath);
                var archivePath = ResolveFileReference(rootPath, fileRef);
                EnsureEntryExists(entries, archivePath, manifestPath);
                return new ReferenceFileDescriptor(
                    archivePath,
                    fileRef,
                    external,
                    RequiredAttribute(element, "mime-type", manifestPath),
                    (string?)element.Attribute("encoding"),
                    (string?)element.Attribute("type"),
                    (string?)element.Attribute("description"),
                    (string?)element.Attribute("tms-id"));
            })
            .ToList();

        return new ProjectDescriptor(
            rootPath,
            manifestPath,
            projectId,
            sourceLanguage,
            targetLanguage,
            translationFiles,
            referenceFiles);
    }

    private static string RequiredAttribute(XElement element, string name, string manifestPath)
    {
        var value = (string?)element.Attribute(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new PluginMisconfigurationException(
                $"Manifest '{manifestPath}' has a {element.Name.LocalName} element without required '{name}'.");
        return value;
    }

    private static void EnsureEntryExists(
        IReadOnlyDictionary<string, ZipArchiveEntry> entries,
        string archivePath,
        string manifestPath)
    {
        if (!entries.ContainsKey(archivePath))
            throw new PluginMisconfigurationException(
                $"Manifest '{manifestPath}' references missing packaged file '{archivePath}'.");
    }

    private static string ResolveFileReference(string rootPath, string fileRef)
    {
        var relativePath = NormalizeRelativePath(fileRef, "COTI file-ref");
        return string.IsNullOrEmpty(rootPath) ? relativePath : $"{rootPath}/{relativePath}";
    }

    private static string NormalizeRelativePath(string? path, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new PluginMisconfigurationException($"{fieldName} is required.");

        var normalized = path.Replace('\\', '/').Trim();
        if (normalized.StartsWith('/') || normalized.Contains(':'))
            throw new PluginMisconfigurationException($"{fieldName} '{path}' must be a relative archive path.");

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
            throw new PluginMisconfigurationException($"{fieldName} '{path}' is not a safe archive path.");

        return string.Join('/', segments);
    }

    private static string NormalizeExistingEntryPath(string path)
        => NormalizeRelativePath(path, "Archive entry path");

    private static string GetFileName(string path)
    {
        var separator = path.LastIndexOf('/');
        return separator < 0 ? path : path[(separator + 1)..];
    }

    private static string GetDirectoryName(string path)
    {
        var separator = path.LastIndexOf('/');
        return separator < 0 ? string.Empty : path[..separator];
    }

    private static bool BelongsToProject(string entryPath, string projectRoot)
        => string.IsNullOrEmpty(projectRoot)
            || entryPath.Equals(projectRoot, StringComparison.OrdinalIgnoreCase)
            || entryPath.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase);

    private static List<string> DistinctLanguages(IEnumerable<string> languages)
        => languages.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static string EnsureCotiExtension(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "assembled.coti";
        return Path.GetExtension(name).Equals(".coti", StringComparison.OrdinalIgnoreCase)
            ? name
            : Path.ChangeExtension(name, ".coti");
    }

    private sealed record PackageDescriptor(
        IReadOnlyDictionary<string, ZipArchiveEntry> Entries,
        List<ProjectDescriptor> Projects);

    private sealed record ProjectDescriptor(
        string RootPath,
        string ManifestPath,
        string ProjectId,
        string SourceLanguage,
        string TargetLanguage,
        List<TranslationFileDescriptor> TranslationFiles,
        List<ReferenceFileDescriptor> ReferenceFiles);

    private sealed record TranslationFileDescriptor(string ArchivePath, string MimeType);

    private sealed record ReferenceFileDescriptor(
        string ArchivePath,
        string FileReference,
        string External,
        string MimeType,
        string? Encoding,
        string? Type,
        string? Description,
        string? TmsId);
}
