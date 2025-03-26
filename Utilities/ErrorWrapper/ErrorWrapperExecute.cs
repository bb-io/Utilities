using Blackbird.Applications.Sdk.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                throw new PluginApplicationException($"Error: {ex.Message}");
            }
        }
    }
}
