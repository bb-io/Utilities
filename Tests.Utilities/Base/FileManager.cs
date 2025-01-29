using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Utilities.Base;
public class FileManager(string folderLocation) : IFileManagementClient
{
    public Task<Stream> DownloadAsync(FileReference reference)
    {
        var outputPath = Path.Combine(folderLocation, @$"Output\{reference.Name}");
        if (File.Exists(outputPath))
        {
            var bytes = File.ReadAllBytes(outputPath);
            var stream = new MemoryStream(bytes);
            return Task.FromResult((Stream)stream);
        }

        var inputPath = Path.Combine(folderLocation, @$"Input\{reference.Name}");
        if (File.Exists(inputPath))
        {
            var bytes = File.ReadAllBytes(inputPath);
            var stream = new MemoryStream(bytes);
            return Task.FromResult((Stream)stream);
        }
        throw new FileNotFoundException($"File '{reference.Name}' not found in either Input or Output folder.");
    }

    public Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName)
    {
        var path = Path.Combine(folderLocation, @$"Output\{fileName}");
        new FileInfo(path).Directory.Create();
        using (var fileStream = File.Create(path))
        {
            stream.CopyTo(fileStream);
        }

        return Task.FromResult(new FileReference() { Name = fileName });
    }
}
