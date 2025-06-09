using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Utilities.ErrorWrapper
{
    public static class ErrorWrapperExecute
    {
        public static async Task<T> ExecuteSafelyAsync<T>(Func<Task<T>> operation, Func<Exception, T>? fallback = null)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                if (fallback != null)
                    return fallback(ex);

                throw new PluginApplicationException($"Error: {ex.Message}");
            }
        }
    }
}
