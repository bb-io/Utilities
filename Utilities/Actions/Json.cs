using System.Text;
using Apps.Utilities.ErrorWrapper;
using Apps.Utilities.Models.Json;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Utilities.Actions
{
    [ActionList("JSON")]
    public class Json : BaseInvocable
    {
        private readonly IFileManagementClient _fileManagementClient;

        public Json(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
            : base(invocationContext)
        {
            _fileManagementClient = fileManagementClient;
        }

        [Action("Get JSON property value")]
        public async Task<GetJsonPropertyOutput> GetJsonPropertyValue([ActionParameter] GetJsonPropertyInput input)
        {
            if (input.File is null && input.JsonString is null)
                throw new PluginMisconfigurationException("Either a JSON file or JSON string must be provided");

            JToken jsonObj;
            if (input.File != null)
            {
                jsonObj = await ErrorWrapperExecute.ExecuteSafely(()=> GetParsedJson(input.File));
            }
            else
            {
                jsonObj = ErrorWrapperExecute.ExecuteSafely(() => JToken.Parse(input.JsonString));
            }

            JToken? token = jsonObj.SelectToken(input.PropertyPath);

            var value = token?.ToString() ?? string.Empty;

            return new GetJsonPropertyOutput
            {
                Value = value
            };
        }

        [Action("Get text value from JSON array (lookup by property)")]
        public async Task<GetJsonPropertyOutput> Lookup([ActionParameter] JsonLookupInput input)
        {
            var jsonObj = await GetParsedJson(input.File);
            JToken? arrayToken = jsonObj.SelectToken(input.LookupArrayPropertyPath);
            if (arrayToken == null || arrayToken.Type != JTokenType.Array)
                throw new PluginMisconfigurationException("The specified property does not exist or is not an array.");

            var matchingItem = arrayToken
                .FirstOrDefault(item => item?.SelectToken(input.LookupPropertyPath)?.ToObject<string>() == input.LookupPropertyValue);

            return new GetJsonPropertyOutput
            {
                Value = matchingItem?.SelectToken(input.ResultPropertyPath)?.ToObject<string>() ?? string.Empty
            };
        }

        [Action("Change JSON property value")]
        public async Task<ChangeJsonPropertyOutput> ChangeJsonProperty([ActionParameter] ChangeJsonPropertyInput input)
        {
            var jsonObj = await GetParsedJson(input.File);
            JToken? tokenToChange = jsonObj.SelectToken(input.PropertyPath);

            if (tokenToChange != null)
            {
                tokenToChange.Replace(JToken.FromObject(input.NewValue));
            }
            else
            {
                jsonObj[input.PropertyPath] = JToken.FromObject(input.NewValue);
            }

            var updatedJson = jsonObj.ToString(Formatting.Indented);

            var updatedBytes = Encoding.UTF8.GetBytes(updatedJson);
            using var updatedStream = new MemoryStream(updatedBytes);

            var updatedFile = await _fileManagementClient.UploadAsync(
                updatedStream,
                "application/json",
                input.File.Name);

            return new ChangeJsonPropertyOutput
            {
                File = updatedFile
            };
        }

        private async Task<JObject> GetParsedJson(FileReference file)
        {
            var fileStream = await _fileManagementClient.DownloadAsync(file);

            string jsonString;
            using (var reader = new StreamReader(fileStream))
            {
                jsonString = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(jsonString))
                throw new PluginMisconfigurationException("The file is empty. Please check the input.");

            try
            {
                return JObject.Parse(jsonString);
            }
            catch (JsonReaderException)
            {
                throw new PluginMisconfigurationException("The file content is not valid JSON. Please check the file input");
            }
        }
    }
}
