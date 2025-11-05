using System.Text;
using Apps.Utilities.ErrorWrapper;
using Apps.Utilities.Models.Enums;
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
                jsonObj = await ErrorWrapperExecute.ExecuteSafely(() => GetParsedJson(input.File));
            }
            else
            {
                jsonObj = ErrorWrapperExecute.ExecuteSafely(() => JToken.Parse(input.JsonString));
            }

            var token = GetTokenAtPath(jsonObj, input.PropertyPath);
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
            var arrayToken = GetTokenAtPath(jsonObj, input.LookupArrayPropertyPath);
            if (arrayToken == null)
                return new GetJsonPropertyOutput { Value = string.Empty };

            if (arrayToken.Type != JTokenType.Array)
                throw new PluginMisconfigurationException(
                    $"The specified path '{input.LookupArrayPropertyPath}' does not point to a JSON array. Actual type: {arrayToken.Type}.");


            var jArray = (JArray)arrayToken;

            var matchingItem = jArray
                .OfType<JToken>()
                .FirstOrDefault(item =>
                {
                    var prop = SafeSelectFirstToken(item, input.LookupPropertyPath);
                    var propValue = prop?.ToObject<string>();
                    return propValue == input.LookupPropertyValue;
                });

            var resultToken = matchingItem != null
                ? SafeSelectFirstToken(matchingItem, input.ResultPropertyPath)
                : null;

            return new GetJsonPropertyOutput
            {
                Value = resultToken?.ToObject<string>() ?? string.Empty
            };
        }

        [Action("Get JSON array property values", Description = "Returns all elements under a JSON array property as text list")]
        public async Task<GetJsonArrayPropertyOutput> GetJsonArrayPropertyValues([ActionParameter] GetJsonPropertyInput input)
        {
            if (input.File is null && input.JsonString is null)
                throw new PluginMisconfigurationException("Either a JSON file or JSON string must be provided");

            var jsonObj = input.File != null
                ? await ErrorWrapperExecute.ExecuteSafely(() => GetParsedJson(input.File))
                : ErrorWrapperExecute.ExecuteSafely(() => JToken.Parse(input.JsonString ?? string.Empty));

            var token = GetTokenAtPath(jsonObj, input.PropertyPath)
                ?? throw new PluginMisconfigurationException($"Property '{input.PropertyPath}' not found in JSON.");

            if (token.Type != JTokenType.Array)
                throw new PluginMisconfigurationException($"Property '{input.PropertyPath}' is not a JSON array.");

            return new GetJsonArrayPropertyOutput
            {
                Values = token.Select(el => el.Type == JTokenType.String
                    ? el.Value<string>() ?? string.Empty
                    : el.ToString(Formatting.None))
            };
        }

        [Action("Change JSON property value")]
        public async Task<ChangeJsonPropertyOutput> ChangeJsonProperty([ActionParameter] ChangeJsonPropertyInput input)
        {
            var nullValueHandling = input.GetNullValueHandlingStrategy();
            if(nullValueHandling == NullValueHandlingStrategy.Ignore && string.IsNullOrEmpty(input.NewValue))
            {
                return new ChangeJsonPropertyOutput
                {
                    File = input.File
                };
            }
            
            if(nullValueHandling == NullValueHandlingStrategy.Error && string.IsNullOrEmpty(input.NewValue))
            {
                throw new PluginMisconfigurationException("The new value cannot be null or empty. Please check the input.");
            }
            
            
            var jsonObj = await GetParsedJson(input.File);
            var tokenToChange = GetTokenAtPath(jsonObj, input.PropertyPath);

            if (tokenToChange != null)
            {
                if(input.NewValue == null)
                {
                    tokenToChange.Replace(JValue.CreateNull());
                }
                else
                {
                    tokenToChange.Replace(JToken.FromObject(input.NewValue));
                }
            }
            else
            {
                if (string.IsNullOrEmpty(input.NewValue))
                {
                    jsonObj[input.PropertyPath] = null;
                }
                else
                {
                    jsonObj[input.PropertyPath] = JToken.FromObject(input.NewValue);
                }
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

        private JToken? SafeSelectFirstToken(JToken root, string path) => ErrorWrapperExecute.ExecuteSafely(() => root.SelectTokens(path).FirstOrDefault());

        private async Task<JObject> GetParsedJson(FileReference file)
        {
            var fileStream = await _fileManagementClient.DownloadAsync(file);

            string jsonString;
            using (var reader = new StreamReader(fileStream))
            {
                jsonString = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                throw new PluginMisconfigurationException("The file is empty. Please check the input.");
            }

            try
            {
                return JObject.Parse(jsonString);
            }
            catch (JsonReaderException)
            {
                throw new PluginMisconfigurationException(
                    "The file content is not valid JSON. Please check the file input");
            }
        }

        private JToken? GetTokenAtPath(JToken jsonObj, string path)
        {
            try
            {
                return jsonObj.SelectToken(path);
            }
            catch (JsonException ex)
            {
                throw new PluginMisconfigurationException(
                    $"The provided property path is invalid. Check the file/string you are sending and the property path. Error details: {ex.Message}");
            }
        }
    }
}
