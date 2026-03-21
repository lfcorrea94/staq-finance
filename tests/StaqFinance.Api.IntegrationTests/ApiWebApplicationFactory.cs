using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StaqFinance.Api.IntegrationTests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestDbName"] = "TestDb_" + Guid.NewGuid()
            });
        });

        builder.ConfigureServices(services =>
        {
            // Serilog freezes its ReloadableLogger when ILoggerFactory is first resolved.
            // Running multiple WebApplicationFactory instances in the same process causes
            // "The logger is already frozen." Replace with a plain test logger.
            var loggerFactoryDescriptors = services
                .Where(d => d.ServiceType == typeof(ILoggerFactory))
                .ToList();

            foreach (var d in loggerFactoryDescriptors)
                services.Remove(d);

            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
        });
    }
}
