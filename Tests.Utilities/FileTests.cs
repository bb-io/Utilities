using Apps.Utilities.Actions;
using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Shared;
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


        [TestMethod]
        public async Task ConvertDocumentToText_ReturnsConvertedText()
        {
            var file = new LoadDocumentRequest { File=new FileReference { Name= "test.pdf" } };

            var response = _fileActions.LoadDocument(file);

            Console.WriteLine(response.Result.Text);
            Assert.IsNotNull(response.Result);
        }

        [TestMethod]
        public async Task SanitizeFileName_ReturnsSucces()
        {
            var file = new FileDto { File = new FileReference { Name = "test.pdf" } };
            var request = new SanitizeRequest { FilterCharacters = new List<string> { "t" } };
            var response = _fileActions.SanitizeFileName(file, request);

            Console.WriteLine(response.File.Name);
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public async Task Compare_same_file_returns_true()
        {
            var file = new FileReference { Name = "test.txt" };
            var file2 = new FileReference { Name = "test.txt" };
            var response = await _fileActions.CompareFileContents(new CompareFilesRequest { Files = new List<FileReference> { file, file2 } });
            Assert.IsTrue(response.AreEqual);
        }

        [TestMethod]
        public async Task Compare_different_file_returns_false()
        {
            var file = new FileReference { Name = "test.html" };
            var file2 = new FileReference { Name = "test.txt" };
            var response = await _fileActions.CompareFileContents(new CompareFilesRequest { Files = new List<FileReference> { file, file2 } });
            Assert.IsFalse(response.AreEqual);
        }


        [TestMethod]
        public async Task Convert_Text_To_File_returns_true()
        {
            var response = _fileActions.ConvertTextToDocument( 
                new ConvertTextToDocumentRequest { 
                    Text = "Some text oh lala", 
                    FileExtension=".txt", 
                    Filename="TestNamea1", 
                    Encoding= "utf8bom"
                });
            Assert.IsNotNull(response.Result);

        }
    }
}
