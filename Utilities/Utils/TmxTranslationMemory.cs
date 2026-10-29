using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Glossaries.Utils.Models.Tmx;
using Blackbird.Applications.Sdk.Glossaries.Utils.Parsers;
using System.Xml.Linq;

namespace Apps.Utilities.Utils;

internal sealed class TmxTranslationMemory
{
    private readonly Dictionary<string, string> translations;

    private TmxTranslationMemory(Dictionary<string, string> translations, List<string> warnings)
    {
        this.translations = translations;
        Warnings = warnings;
    }

    public IReadOnlyList<string> Warnings { get; }

    public bool TryGetTranslation(string source, out string target) =>
        translations.TryGetValue(source, out target!);

    public static async Task<TmxTranslationMemory> Load(
        Stream stream,
        string sourceLanguage,
        string targetLanguage,
        bool ignoreSourceCase)
    {
        Stream parserStream = stream;
        MemoryStream? bufferedStream = null;
        if (!stream.CanSeek)
        {
            bufferedStream = new MemoryStream();
            await stream.CopyToAsync(bufferedStream);
            bufferedStream.Position = 0;
            parserStream = bufferedStream;
        }

        try
        {
            var document = await XDocument.LoadAsync(parserStream, LoadOptions.None, CancellationToken.None);
            if (document.Root?.Name.LocalName != "tmx"
                || !document.Root.Descendants().Any(element => element.Name.LocalName == "body"))
            {
                throw new PluginMisconfigurationException("The provided translation memory is not a valid TMX file.");
            }

            parserStream.Position = 0;
            var entries = await TmxParser.ParseTmxFile(parserStream);
            return Create(entries, sourceLanguage, targetLanguage, ignoreSourceCase);
        }
        finally
        {
            bufferedStream?.Dispose();
        }
    }

    private static TmxTranslationMemory Create(
        IReadOnlyList<TmxEntry> entries,
        string sourceLanguage,
        string targetLanguage,
        bool ignoreSourceCase)
    {
        var sourceComparer = ignoreSourceCase
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var candidates = new Dictionary<string, HashSet<string>>(sourceComparer);
        var warnings = new List<string>();

        for (var index = 0; index < entries.Count; index++)
        {
            var terms = entries[index].Terms.ToList();
            var sourceTerms = terms
                .Where(term => LanguagesMatch(term.LanguageCode, sourceLanguage))
                .Select(term => term.Term)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var targetTerms = terms
                .Where(term => LanguagesMatch(term.LanguageCode, targetLanguage))
                .Select(term => term.Term)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (sourceTerms.Count != 1 || targetTerms.Count != 1)
            {
                warnings.Add(
                    $"TMX translation unit at position {index + 1} does not contain exactly one '{sourceLanguage}' source and one '{targetLanguage}' target and was skipped.");
                continue;
            }

            var source = sourceTerms[0];
            var target = targetTerms[0];
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            {
                warnings.Add($"TMX translation unit at position {index + 1} has an empty source or target and was skipped.");
                continue;
            }

            if (!candidates.TryGetValue(source, out var targets))
            {
                targets = new HashSet<string>(StringComparer.Ordinal);
                candidates[source] = targets;
            }

            targets.Add(target);
        }

        var translations = new Dictionary<string, string>(sourceComparer);
        foreach (var (source, targets) in candidates)
        {
            if (targets.Count == 1)
            {
                translations[source] = targets.Single();
                continue;
            }

            warnings.Add(
                $"The TMX file contains multiple different targets for source '{Shorten(source)}'; this source was skipped.");
        }

        return new TmxTranslationMemory(translations, warnings);
    }

    private static bool LanguagesMatch(string? actual, string expected) =>
        string.Equals(NormalizeLanguage(actual), NormalizeLanguage(expected), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeLanguage(string? language) =>
        (language ?? string.Empty).Trim().Replace('_', '-');

    private static string Shorten(string value)
    {
        const int maximumLength = 80;
        var normalized = value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
        return normalized.Length <= maximumLength
            ? normalized
            : $"{normalized[..maximumLength]}…";
    }
}
