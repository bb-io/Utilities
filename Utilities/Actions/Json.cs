using System.Text;
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
            if (input is null)
                throw new PluginMisconfigurationException("Input is required.");

            if (input.File is null && input.JsonString is null)
                throw new PluginMisconfigurationException("Either a JSON file or JSON string must be provided");

            if (string.IsNullOrWhiteSpace(input.PropertyPath))
                throw new PluginMisconfigurationException("Property path is required.");

            JToken jsonObj;
            if (input.File != null)
            {
                jsonObj = await GetParsedJson(input.File);
            }
            else
            {
                jsonObj = ParseJsonString(input.JsonString!);
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
                ? await GetParsedJson(input.File)
                : ParseJsonString(input.JsonString!);

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
            if (input is null)
                throw new PluginMisconfigurationException("Input is required.");

            if (input.File is null)
                throw new PluginMisconfigurationException("A JSON file is required.");

            if (string.IsNullOrWhiteSpace(input.PropertyPath))
                throw new PluginMisconfigurationException("Property path is required.");

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

        private static JToken? SafeSelectFirstToken(JToken root, string path)
        {
            try
            {
                return root.SelectTokens(path).FirstOrDefault();
            }
            catch (Exception ex) when (IsInvalidJsonPathException(ex))
            {
                throw CreateInvalidPropertyPathException(path);
            }
        }

        private async Task<JObject> GetParsedJson(FileReference file)
        {
            if (file is null)
                throw new PluginMisconfigurationException("A JSON file is required.");

            Stream fileStream;
            try
            {
                fileStream = await _fileManagementClient.DownloadAsync(file);
            }
            catch (Exception ex) when (ex.Message.Contains("Url for downloading is null", StringComparison.OrdinalIgnoreCase))
            {
                throw new PluginMisconfigurationException(
                    "The provided JSON file reference is invalid because it does not contain a download URL. Please re-map or upload the file.", ex);
            }
            catch (ArgumentException ex)
            {
                throw new PluginMisconfigurationException(
                    "The provided JSON file reference is invalid. Please re-map or upload the file.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new PluginApplicationException(
                    "The JSON file could not be downloaded because the file service request failed. Please try again later.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new PluginApplicationException(
                    "The JSON file download timed out. Please try again later.", ex);
            }

            string jsonString;
            await using (fileStream)
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
            catch (JsonReaderException ex)
            {
                throw new PluginMisconfigurationException(
                    CreateInvalidJsonMessage("file", ex));
            }
        }

        private static JToken ParseJsonString(string jsonString)
        {
            try
            {
                return JToken.Parse(jsonString);
            }
            catch (JsonReaderException ex)
            {
                throw new PluginMisconfigurationException(
                    CreateInvalidJsonMessage("JSON string", ex));
            }
        }

        private static JToken? GetTokenAtPath(JToken jsonObj, string path)
        {
            try
            {
                return jsonObj.SelectToken(path);
            }
            catch (Exception ex) when (IsInvalidJsonPathException(ex))
            {
                throw CreateInvalidPropertyPathException(path);
            }
        }

        private static bool IsInvalidJsonPathException(Exception exception) =>
            exception is JsonException or ArgumentException or IndexOutOfRangeException;

        private static string CreateInvalidJsonMessage(string source, JsonReaderException exception)
        {
            var path = string.IsNullOrWhiteSpace(exception.Path)
                ? string.Empty
                : $", path '{exception.Path}'";

            return $"The provided {source} is not valid JSON at line {exception.LineNumber}, position {exception.LinePosition}{path}. Please check the JSON syntax.";
        }

        private static PluginMisconfigurationException CreateInvalidPropertyPathException(string path) =>
            new($"The property path '{path}' is not a valid JSONPath expression. Please check its brackets, quotes, dots, and array indexes.");
    }
}
