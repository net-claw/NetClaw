using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace NetClaw.IntegrationTests;

public abstract class BaseApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var contentRoot = context.HostingEnvironment.ContentRootPath;
            var testConfigPath = Path.Combine(contentRoot, "appsettings.test.json");

            config.AddJsonFile(testConfigPath, optional: false, reloadOnChange: false);
            config.AddEnvironmentVariables();
        });
    }
}
