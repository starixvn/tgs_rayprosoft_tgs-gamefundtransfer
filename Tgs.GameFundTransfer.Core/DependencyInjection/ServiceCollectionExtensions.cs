using System.Reflection;
using StackExchange.Redis;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core;
using Tgs.GameInterface.SabaService.Client;
using Tgs.GameProviders.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameTransferCore(
        this IServiceCollection services,
        Func<IServiceProvider, IDatabase> redisDatabaseResolver,
        Action<FundsLockedOptions, IServiceProvider> fundsLockedOptions,
        Action<CashServiceClientOptions, IServiceProvider> cashServiceClientOptions,
        Action<GameProvidersClientOptions, IServiceProvider> gameProvidersClientOptions,
        Action<GameCurrencyClientOptions, IServiceProvider> gameCurrencyClientOptions,
        Action<GameInterfaceSabaClientOptions, IServiceProvider> gameInterfaceSabaClientOptions)
        => services
            .AddOptions<FundsLockedOptions>()
            .Configure(fundsLockedOptions)
            .Services
            .RegisterAllServiceUtilityTypes(
                Assembly.GetExecutingAssembly(),
                ServiceLifetime.Transient)
            .AddCashServiceClient(cashServiceClientOptions)
            .AddGameProvidersClient(gameProvidersClientOptions)
            .AddGameCurrencyClient(gameCurrencyClientOptions)
            .AddGameInterfaceSabaServiceClient(gameInterfaceSabaClientOptions)
            .AddTransient<IFundsLocked>(sp => ActivatorUtilities.CreateInstance<FundsLocked>(sp, redisDatabaseResolver(sp)));
}
