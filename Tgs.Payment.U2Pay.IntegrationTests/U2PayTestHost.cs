using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;

namespace Tgs.Payment.U2Pay.IntegrationTests;

internal static class U2PayTestHost
{
    public static WebApplicationFactory<Program> Factory => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(hostBuilder => hostBuilder
            .ConfigureLogging((_, logging) => logging.ClearProviders())
            .ConfigureAppConfiguration(configBuilder =>
            {
                configBuilder.Sources.Clear();

                _ = configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["U2Pay:PaymentApiUrl"] = "https://xr.hmpas.com/",
                    ["U2Pay:CallbackBaseUrl"] = "https://dev-payment.rpgp.cc/u2pay",
                    ["U2Pay:PaymentServiceBaseUrl"] = "http://localhost:5541"
                });
            })
            .ConfigureServices(services => services
                .AddFakeNatsMessageQueue()
                .AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
                    sp => new ConfigureNamedOptions<OtlpExporterOptions>(
                        Options.DefaultName,
                        exporterOptions => exporterOptions.TimeoutMilliseconds = 0))));
}
