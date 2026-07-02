using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Transformations;

namespace Apps.Utilities.Extensions;

public static class StreamExtensions
{
    public static Transformation LoadTransformation(this Stream stream, string fileName)
    {
        var loadResult = Transformation.Load(stream, fileName);
        if (!loadResult.Success)
            throw new PluginMisconfigurationException(loadResult.Error);

        var loadResultValue = loadResult.Value;
        return loadResultValue ?? throw new PluginMisconfigurationException("The provided file is not a valid XLIFF file.");
    }
}