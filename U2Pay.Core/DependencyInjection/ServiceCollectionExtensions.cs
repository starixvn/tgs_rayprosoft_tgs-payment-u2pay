using System.Reflection;
using Tgs.Payment.U2Pay;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddU2Pay(
       this IServiceCollection services,
       Action<U2PayOptions> configureOptions,
       Action<IHttpClientBuilder> ConfigureAiClient)
    {
        ConfigureAiClient?.Invoke(services.AddHttpClient<U2PayClient>());

        return services
            .AddOptions<U2PayOptions>()
            .Configure(configureOptions)
            .Services
            .RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
            .AddSingleton<U2PayHasher>();
    }
}
