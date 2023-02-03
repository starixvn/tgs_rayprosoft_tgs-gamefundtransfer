using Microsoft.EntityFrameworkCore;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer;
using Tgs.GameFundTransfer.Core;
using Tgs.GameInterface.SabaService.Client;
using Tgs.GameProviders.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollecitonExtensions
{
    public static IServiceCollection AddGameFundTransfer(
        this IServiceCollection services,
        Action<RedisOptions, IServiceProvider> redisDBconfigure,
        Action<IServiceProvider, DbContextOptionsBuilder> efConfigure,
        Action<FundsLockedOptions, IServiceProvider> fundsLockedOptions,
        Action<CashServiceClientOptions, IServiceProvider> cashServiceClientOptions,
        Action<GameProvidersClientOptions, IServiceProvider> gameProvidersClientOptions,
        Action<GameCurrencyClientOptions, IServiceProvider> gameCurrencyClientOptions,
        Action<GameInterfaceSabaClientOptions, IServiceProvider> gameInterfaceSabaClientOptions)
        => services
        .AddOptions<RedisOptions>()
        .Configure(redisDBconfigure)
        .Services
        .AddSingleton<RedisConnectionManager>()
        .AddGameTransferCore(
            fundsLockedOptions: fundsLockedOptions,
            cashServiceClientOptions: cashServiceClientOptions,
            gameProvidersClientOptions: gameProvidersClientOptions,
            gameCurrencyClientOptions: gameCurrencyClientOptions,
            gameInterfaceSabaClientOptions: gameInterfaceSabaClientOptions)
        .AddGameTransferRepository(
            efConfigure,
            (sp) => sp.GetRequiredService<RedisConnectionManager>().Database);
}
