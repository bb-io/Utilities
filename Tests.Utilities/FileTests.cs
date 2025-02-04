using Apps.Utilities.Actions;
using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Texts;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Xml;
using System.Xml.Linq;
using Tests.Utilities.Base;

namespace Tests.Utilities
{
    [TestClass]
    public class FileTests : TestBase
    {
        private Files _fileActions;

        [TestInitialize]
        public void Init()
        {
            var outputDirectory = Path.Combine(GetTestFolderPath(), "Output");
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
            Directory.CreateDirectory(outputDirectory);

            _fileActions = new Files(InvocationContext, FileManager, CreateLogger<Files>());
        }

        [TestMethod]

        public async Task UnzipFiles_ZipFileInput_Success()
        {
            var file = new FileReference { Name = "test.zip" };

            var response = await _fileActions.UnzipFiles(new FileDto
            {
                File = file
            });

            Assert.IsNotNull(response.Files);
        }

        [TestMethod]

        public async Task UnzipFiles_NotZipFileInput_ThrowsPluginMisconfigurationException()
        {
            var file = new FileReference { Name = "test.txt" };
            var fileDto = new FileDto
            {
                File = file
            };

            await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(()=> _fileActions.UnzipFiles(fileDto));
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
