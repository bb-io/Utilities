using Apps.Utilities.Actions;
using Apps.Utilities.Models.Json;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Tests.Utilities.Base;

namespace Tests.Utilities
{
    [TestClass]
    public class JsonTests:TestBase
    {

        private Json _jsonActions;

        [TestInitialize]
        public void Init()
        {
            var outputDirectory = Path.Combine(GetTestFolderPath(), "Output");
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
            Directory.CreateDirectory(outputDirectory);

            _jsonActions = new Json(InvocationContext, FileManager);
        }

        [TestMethod]
        public async Task GetJsonValue()
        {
            var action = new Json(InvocationContext,FileManager);

            var input = new GetJsonPropertyInput {JsonString= "{\r\n    \"id\": \"lMOqwANyFNSr0TkQ0_dc9:3\",\r\n    \"issues\": \"\\\"cycle\\\" has been mistranslated as \\\"torta\\\" (cake).\",\r\n    \"source\": \"{1}  ", 
                PropertyPath = "source"};

            var response = await action.GetJsonPropertyValue(input);

            Console.WriteLine(response.Value);
            Assert.IsNotNull(response.Value);
            //Assert.AreEqual("Apikey", response.Value);
        }

        [TestMethod]
        public async Task GetJsonValue_ThrowsMisconfiguration_WhenInputIsNull()
        {
            var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(
                () => _jsonActions.GetJsonPropertyValue(null!));

            Assert.AreEqual("Input is required.", exception.Message);
        }

        [TestMethod]
        public async Task GetJsonValue_ThrowsMisconfiguration_WhenJsonStringIsInvalid()
        {
            var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
                _jsonActions.GetJsonPropertyValue(new GetJsonPropertyInput
                {
                    JsonString = "Garbage",
                    PropertyPath = "value"
                }));

            StringAssert.Contains(exception.Message, "The provided JSON string is not valid JSON at line");
            StringAssert.Contains(exception.Message, "Please check the JSON syntax.");
        }

        [TestMethod]
        public async Task GetJsonValue_ThrowsMisconfiguration_WhenArrayIndexIsOutOfRange()
        {
            var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
                _jsonActions.GetJsonPropertyValue(new GetJsonPropertyInput
                {
                    JsonString = "{\"items\":[\"first\"]}",
                    PropertyPath = "items[-2]"
                }));

            StringAssert.Contains(exception.Message, "not a valid JSONPath expression");
        }

        [TestMethod]
        public async Task GetJsonValue_ThrowsMisconfiguration_WhenFileDownloadUrlIsMissing()
        {
            var actions = new Json(InvocationContext, new MissingDownloadUrlFileManager());

            var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
                actions.GetJsonPropertyValue(new GetJsonPropertyInput
                {
                    File = new FileReference { Name = "input.json" },
                    PropertyPath = "value"
                }));

            Assert.AreEqual(
                "The provided JSON file reference is invalid because it does not contain a download URL. Please re-map or upload the file.",
                exception.Message);
        }

        [TestMethod]
        public async Task GetJsonValue_ThrowsApplicationException_WhenFileServiceRequestFails()
        {
            var actions = new Json(InvocationContext, new UnavailableFileManager());

            var exception = await Assert.ThrowsExceptionAsync<PluginApplicationException>(() =>
                actions.GetJsonPropertyValue(new GetJsonPropertyInput
                {
                    File = new FileReference { Name = "input.json" },
                    PropertyPath = "value"
                }));

            Assert.AreEqual(
                "The JSON file could not be downloaded because the file service request failed. Please try again later.",
                exception.Message);
        }

        [TestMethod]
        public async Task Lookup()
        {
            var input = new JsonLookupInput
            {
                File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "lookup.json" },
                LookupArrayPropertyPath = "$.fields",
                LookupPropertyPath = "$.field_id",
                LookupPropertyValue = "id1",
                ResultPropertyPath = "$.field_value"
            };

            var actions = new Json(InvocationContext, FileManager);
            var response = await actions.Lookup(input);

            Assert.AreEqual("text value", response.Value);
        }

        [TestMethod]
        public async Task ChangeJsonValue()
        {
            var action = new Json(InvocationContext, FileManager);

            await action.ChangeJsonProperty(new ChangeJsonPropertyInput
            {
                File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "config.json" },
                PropertyPath = "settings.theme",
                NewValue = null,
                NullValueHandlingStrategy = "ignore"
            });

            var check = await action.GetJsonPropertyValue(new GetJsonPropertyInput
            {
                File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "config.json" },
                PropertyPath = "settings.theme"
            });

            Console.WriteLine(check.Value);
        }

        private sealed class MissingDownloadUrlFileManager : IFileManagementClient
        {
            public Task<Stream> DownloadAsync(FileReference reference) =>
                Task.FromException<Stream>(new InvalidOperationException("Url for downloading is null"));

            public Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName) =>
                throw new NotSupportedException();
        }

        private sealed class UnavailableFileManager : IFileManagementClient
        {
            public Task<Stream> DownloadAsync(FileReference reference) =>
                Task.FromException<Stream>(new HttpRequestException("Service unavailable"));

            public Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName) =>
                throw new NotSupportedException();
        }
    }
}
