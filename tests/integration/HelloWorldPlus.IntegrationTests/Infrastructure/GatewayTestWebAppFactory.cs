using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HelloWorldPlus.IntegrationTests.Infrastructure;

public sealed class GatewayTestWebAppFactory
    : WebApplicationFactory<HelloWorldPlus.Gateway.Program>
{
    private readonly Uri _apiBaseAddress;

    public GatewayTestWebAppFactory(Uri? apiBaseAddress = null)
    {
        _apiBaseAddress = apiBaseAddress ?? new Uri("http://127.0.0.1:1");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var solutionRoot = FindSolutionRoot(Directory.GetCurrentDirectory())
            ?? FindSolutionRoot(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not find solution root directory.");
        builder.UseContentRoot(Path.Combine(
            solutionRoot,
            "src",
            "HelloWorldPlus.Gateway"));
        builder.UseEnvironment("Testing");
        builder.UseStaticWebAssets();
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gateway:ApiBaseUrl"] = _apiBaseAddress.AbsoluteUri
            });
        });
    }

    private static string? FindSolutionRoot(string startDirectory)
    {
        for (var directory = new DirectoryInfo(startDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (directory.GetFiles("*.slnx").Length > 0)
            {
                return directory.FullName;
            }
        }

        return null;
    }
}
