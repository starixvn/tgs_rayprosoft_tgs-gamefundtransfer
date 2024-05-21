using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Entities;
using Tgs.GameFundTransfer.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddGameTransferRepository(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder> efConfigure,
		Func<IServiceProvider, IDatabase> redisDatabaseResolver)
		=> services
		.AddDbContext<TgsContext>(efConfigure)
		.AddTransient<IFundsDistributedLock>(sp => ActivatorUtilities.CreateInstance<FundsDistributedLock>(sp, redisDatabaseResolver(sp)))
		.AddTransient<IFundBalanceRepository, FundBalanceRepository>()
		.Decorate<IFundBalanceRepository>((innerRepository, sp) => ActivatorUtilities.CreateInstance<FundBalanceCacheRepository>(sp, innerRepository, redisDatabaseResolver(sp), TimeSpan.FromSeconds(30)))
		.AddTransient<IFundTransferTransactionRepository, FundTransferTransactionRepository>()
		.AddTransient<IPlayerRepository, PlayerRepository>()
		.Decorate<IPlayerRepository>((innerRepository, sp) => ActivatorUtilities.CreateInstance<PlayerCacheRepository>(sp, innerRepository, TimeSpan.FromMinutes(5)))
		.AddTransient<IGameProviderRepository, GameProviderRepository>()
		.Decorate<IGameProviderRepository>((innerRepository, sp) => ActivatorUtilities.CreateInstance<GameProviderCacheRepository>(sp, innerRepository, TimeSpan.FromMinutes(60)))
		.AddTransient<IBrandGameProviderRepository, BrandGameProviderRepository>()
		.Decorate<IBrandGameProviderRepository>((innerRepository, sp) => ActivatorUtilities.CreateInstance<BrandGameProviderCacheRepository>(sp, innerRepository, TimeSpan.FromMinutes(5)))
		.AddTransient<IGameProviderConfigurationRepository, GameProviderConfigurationRepository>()
		.Decorate<IGameProviderConfigurationRepository>((innerRepository, sp) => ActivatorUtilities.CreateInstance<GameProviderConfigurationCacheRepository>(sp, innerRepository, TimeSpan.FromMinutes(5)));
}
