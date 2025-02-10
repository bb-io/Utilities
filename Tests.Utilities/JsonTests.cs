using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.Utilities.Actions;
using Apps.Utilities.Models.Json;
using Microsoft.Extensions.Configuration;
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

            var input = new GetJsonPropertyInput {File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name="appsettings.json" } , 
                PropertyPath = "ConnectionDefinition.apiKey"};

            var response = await action.GetJsonPropertyValue(input);

            Console.WriteLine(response.Value);
            Assert.IsNotNull(response.Value);
            Assert.AreEqual("Apikey", response.Value);
        }

        [TestMethod]
        public async Task ChangeJsonValue()
        {
            var action = new Json(InvocationContext, FileManager);

            var response = await action.ChangeJsonProperty(new ChangeJsonPropertyInput
            {
                File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "appsettings.json" },
                PropertyPath = "ConnectionDefinition.apiKey", NewValue = "NewApikey"
            });
            
            var check = await action.GetJsonPropertyValue(new GetJsonPropertyInput
            {
                File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "appsettings.json" },
                PropertyPath = "ConnectionDefinition.apiKey"
            });

            Console.WriteLine(check.Value);
            Assert.AreEqual("NewApikey", check.Value);
        }

        private string GetTestFolderPath()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            return config["TestFolder"];
        }
    }
}
