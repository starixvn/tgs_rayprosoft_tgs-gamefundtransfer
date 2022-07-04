using System.Reflection;
using Tgs.CashService.Client;
using Tgs.GameProviders.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGameTransferCore(
            this IServiceCollection services,
            Action<CashServiceClientOptions, IServiceProvider> cashServiceClientOptions,
            Action<GameProvidersClientOptions, IServiceProvider> gameProvidersClientOptions)
            => services
                .RegisterAllServiceUtilityTypes(
                    Assembly.GetExecutingAssembly(),
                    ServiceLifetime.Transient)
                .AddCashServiceClient(cashServiceClientOptions)
                .AddGameProvidersClient(gameProvidersClientOptions);
    }
}
