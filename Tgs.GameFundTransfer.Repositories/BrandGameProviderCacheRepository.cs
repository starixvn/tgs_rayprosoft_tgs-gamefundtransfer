using Microsoft.Extensions.Caching.Memory;
using Tgs.GameFundTransfer.Core;

namespace Tgs.GameFundTransfer.Repositories;

internal class BrandGameProviderCacheRepository(
	IBrandGameProviderRepository repository,
	IMemoryCache memoryCache,
	TimeSpan absoluteExpiration) : IBrandGameProviderRepository
{
	public async ValueTask<Guid> GetGameProviderConfigurationIdAsync(Guid brandId, Guid gameProviderId, CancellationToken cancellationToken = default)
	{
		var key = string.Concat(typeof(BrandGameProviderCacheRepository).Namespace!.Replace(".", "-").ToLower(), ":", "brandgameprovider", ":", "get", ":", "gameproviderconfigurationid", ":", brandId, ":", gameProviderId);

		if (!memoryCache.TryGetValue(key, out Guid? gameProviderConfigurationId))
		{
			gameProviderConfigurationId = await repository.GetGameProviderConfigurationIdAsync(
				brandId: brandId,
				gameProviderId: gameProviderId,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			if (gameProviderConfigurationId.HasValue)
			{
				return memoryCache.Set(key, gameProviderConfigurationId!.Value, absoluteExpiration);
			}
		}

		return gameProviderConfigurationId!.Value;
	}
}
