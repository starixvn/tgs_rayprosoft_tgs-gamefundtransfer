using System.Reflection;
using StackExchange.Redis;
using Tgs.CashService.Client;
using Tgs.GameFundTransfer.Core;
using Tgs.GameProviders.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGameTransferCore(
            this IServiceCollection services,
            Func<IServiceProvider, IDatabase> redisDatabaseResolver,
            Action<CashServiceClientOptions, IServiceProvider> cashServiceClientOptions,
            Action<GameProvidersClientOptions, IServiceProvider> gameProvidersClientOptions)
            => services
                .RegisterAllServiceUtilityTypes(
                    Assembly.GetExecutingAssembly(),
                    ServiceLifetime.Transient)
                .AddCashServiceClient(cashServiceClientOptions)
                .AddGameProvidersClient(gameProvidersClientOptions)
                .AddTransient<IFundsLocked>(sp => ActivatorUtilities.CreateInstance<FundsLocked>(sp, redisDatabaseResolver(sp)));
    }
}
