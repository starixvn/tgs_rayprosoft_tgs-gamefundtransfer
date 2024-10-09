using System.Reflection;
using Tgs.GameFundTransfer.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddGameTransferCore(
		this IServiceCollection services)
		=> services
		.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
		.AddSerialNumberGenerator()
		.AddSingleton<ISnowflakeWorkUtensil, SnowflakeWorkUtensil>()
		.AddSingleton<IGameProviderTransactionIdUtensil, GameProviderTransactionIdUtensil>()
		.AddSingleton<IFundBalanceCacheDispatcher, FundBalanceCacheDispatcher>();
}
