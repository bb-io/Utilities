using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Annotation;
using Group = Blackbird.Filters.Transformations.Group;

namespace Apps.Utilities.Utils;

internal static class Pseudolocalizer
{
    private const string Lowercase = "áƀçďéƒğĥíĵķľḿńóṕɋŕšţúṽŵẋýž";
    private const string Uppercase = "ÁɃÇĎÉƑĞĤÍĴĶĽḾŃÓṔɊŔŠŢÚṼŴẊÝŽ";
    private static readonly Regex PrintfPlaceholder = new(
        @"\G%(?:%|(?:\d+\$)?[-+#0']*(?:\d+|\*(?:\d+\$)?)?(?:\.(?:\d+|\*(?:\d+\$)?))?(?:hh|ll|[hljztL])?[diuoxXfFeEgGaAcspn])",
        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static void Apply(Node node, ISet<SegmentState> states, int expansion, bool translatable = true)
    {
        translatable &= node.Translate != false;
        switch (node)
        {
            case Transformation transformation:
                foreach (var child in transformation.Children)
                    Apply(child, states, expansion, translatable);
                break;
            case Group group:
                foreach (var child in group.Children)
                    Apply(child, states, expansion, translatable);
                break;
            case Unit unit:
                ApplyToUnit(unit, states, expansion, translatable);
                break;
        }
    }

    private static void ApplyToUnit(Unit unit, ISet<SegmentState> states, int expansion, bool translatable)
    {
        // Annotation spans can cross segment boundaries, including unselected segments.
        var annotations = new List<AnnotationStart>();
        foreach (var segment in unit.Segments)
        {
            var selected = translatable && !segment.IsIgnorbale && states.Contains(segment.State ?? SegmentState.Initial);
            var target = new List<LineElement>();
            var hasText = false;
            long textLength = 0;
            foreach (var part in segment.Source)
            {
                if (part is AnnotationStart start)
                    annotations.Add(start);
                else if (part is AnnotationEnd end)
                    annotations.Remove(end.StartAnnotationReference!);

                if (part.GetType() == typeof(LineElement))
                {
                    var value = part.Value;
                    if (selected && annotations.All(annotation => annotation.Translate != false))
                    {
                        hasText |= !string.IsNullOrWhiteSpace(value);
                        value = TransformText(value, out var length);
                        textLength += length;
                    }
                    target.Add(new LineElement { Value = value });
                }
                else
                {
                    // Structural parts retain their cross-references and are never edited.
                    target.Add(part);
                }
            }

            if (!selected || !hasText)
                continue;

            int padding;
            try
            {
                padding = checked((int)decimal.Ceiling((decimal)textLength * expansion / 100));
            }
            catch (OverflowException)
            {
                throw new PluginMisconfigurationException("Expansion percentage produces too much padding. Use a smaller value.");
            }
            target.Insert(0, new LineElement { Value = "[" });
            target.Add(new LineElement { Value = new string('~', padding) + "]" });
            segment.Target = target;
            segment.TargetWhiteSpaceHandling = segment.SourceWhiteSpaceHandling;
        }
    }

    private static string TransformText(string text, out int textLength)
    {
        var output = new StringBuilder(text.Length);
        var braceEnds = new Dictionary<int, int>();
        var braceStarts = new Stack<int>();
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '{') braceStarts.Push(index);
            else if (text[index] == '}' && braceStarts.TryPop(out var start)) braceEnds[start] = index;
        }
        textLength = 0;
        for (var index = 0; index < text.Length;)
        {
            var placeholderLength = GetPlaceholderLength(text, index, braceEnds);
            if (placeholderLength > 0)
            {
                output.Append(text, index, placeholderLength);
                index += placeholderLength;
                continue;
            }

            var element = StringInfo.GetNextTextElement(text, index);
            foreach (var rune in element.EnumerateRunes())
            {
                if (rune.Value is >= 'a' and <= 'z') output.Append(Lowercase[rune.Value - 'a']);
                else if (rune.Value is >= 'A' and <= 'Z') output.Append(Uppercase[rune.Value - 'A']);
                else if (Rune.IsDigit(rune)) output.Append('9');
                else output.Append(rune.ToString());
            }
            textLength++;
            index += element.Length;
        }
        return output.ToString();
    }

    private static int GetPlaceholderLength(string text, int index, IReadOnlyDictionary<int, int> braceEnds)
    {
        var braceStart = text[index] == '$' && index + 1 < text.Length && text[index + 1] == '{'
            ? index + 1 : index;
        if (braceEnds.TryGetValue(braceStart, out var end)) return end - index + 1;
        if (text[index] == '%')
        {
            var match = PrintfPlaceholder.Match(text, index);
            if (match.Success) return match.Length;
        }
        return 0;
    }
}
