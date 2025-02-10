using System.Text;
using Apps.Utilities.Models.Json;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Utilities.Actions
{
    [ActionList]
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
            var fileStream = await _fileManagementClient.DownloadAsync(input.File);
            string jsonString;
            using (var reader = new StreamReader(fileStream))
            {
                jsonString = await reader.ReadToEndAsync();
            }

            var jsonObj = JObject.Parse(input.JsonString);

            JToken token = jsonObj.SelectToken(input.PropertyPath);
            var value = token?.ToObject<object>();

            return new GetJsonPropertyOutput
            {
                Value = value
            };
        }


        [Action("Change JSON property value")]
        public async Task<ChangeJsonPropertyOutput> ChangeJsonProperty([ActionParameter] ChangeJsonPropertyInput input)
        {
            var fileStream = await _fileManagementClient.DownloadAsync(input.File);
            string jsonString;
            using (var reader = new StreamReader(fileStream))
            {
                jsonString = await reader.ReadToEndAsync();
            }

            var jsonObj = JObject.Parse(input.JsonString);
          
            JToken tokenToChange = jsonObj.SelectToken(input.PropertyPath);
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
    }
}
