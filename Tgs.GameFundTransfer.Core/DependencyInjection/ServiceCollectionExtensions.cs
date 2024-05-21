using System.Reflection;
using Tgs.CashService.Client;
using Tgs.GameFundTransfer.Core;
using Tgs.GameInterface.SabaService.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddGameTransferCore(
		this IServiceCollection services,
		Action<FundsLockedOptions, IServiceProvider> fundsLockedOptions,
		Action<CashServiceClientOptions, IServiceProvider> cashServiceClientOptions,
		Action<GameInterfaceSabaClientOptions, IServiceProvider> gameInterfaceSabaClientOptions)
		=> services
		.AddOptions<FundsLockedOptions>()
		.Configure(fundsLockedOptions)
		.Services
		.RegisterAllServiceUtilityTypes(
			Assembly.GetExecutingAssembly(),
			ServiceLifetime.Transient)
		.AddSerialNumberGenerator()
		.AddCashServiceClient(cashServiceClientOptions)
		.AddGameInterfaceSabaServiceClient(gameInterfaceSabaClientOptions)
		.AddSingleton(TimeProvider.System)
		.AddSingleton<ISnowflakeWorkUtensil, SnowflakeWorkUtensil>()
		.AddSingleton<IGameProviderTransactionIdUtensil, GameProviderTransactionIdUtensil>();
}
