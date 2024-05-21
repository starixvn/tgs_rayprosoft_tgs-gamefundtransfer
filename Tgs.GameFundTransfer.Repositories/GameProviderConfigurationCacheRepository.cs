using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Tgs.GameFundTransfer.Core;

namespace Tgs.GameFundTransfer.Repositories;

internal class GameProviderConfigurationCacheRepository(
	IGameProviderConfigurationRepository repository,
	IMemoryCache memoryCache,
	TimeSpan absoluteExpiration) : IGameProviderConfigurationRepository
{
	public async ValueTask<JsonNode> GetConfigurationDataAsync(Guid gameProviderConfigurationId, CancellationToken cancellationToken = default)
	{
		var key = string.Concat(typeof(GameProviderConfigurationCacheRepository).Namespace!.Replace(".", "-").ToLower(), ":", "gameproviderconfiguration", ":", "get", ":", "configurationdata", ":", gameProviderConfigurationId);

		if (!memoryCache.TryGetValue(key, out JsonNode? configurationData))
		{
			configurationData = await repository.GetConfigurationDataAsync(
				gameProviderConfigurationId: gameProviderConfigurationId,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			if (configurationData is not null)
			{
				return memoryCache.Set(key, configurationData, absoluteExpiration);
			}
		}

		return configurationData!;
	}
}
