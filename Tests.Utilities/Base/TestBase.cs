using Apps.Utilities.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Utilities.Base;
public class TestBase
{
    public IEnumerable<AuthenticationCredentialsProvider> Creds { get; set; }

    public InvocationContext InvocationContext { get; set; }

    public FileManager FileManager { get; set; }

    public TestBase()
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        Creds = config.GetSection("ConnectionDefinition").GetChildren().Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)).ToList();
        var folderLocation = config.GetSection("TestFolder").Value;

        InvocationContext = new InvocationContext
        {
            AuthenticationCredentialsProviders = Creds,
        };

        FileManager = new FileManager(folderLocation);
    }

    public ILogger<T> CreateLogger<T>()
    {
        using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace));

        return loggerFactory.CreateLogger<T>();
    }
    public static string GetTestFolderPath()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        return config["TestFolder"];
    }
}
