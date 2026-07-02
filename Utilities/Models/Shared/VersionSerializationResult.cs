using Blackbird.Filters.Transformations;

namespace Apps.Utilities.Models.Shared;

public record VersionSerializationResult(Transformation Transformation, Func<Transformation, string> XliffSerializer);