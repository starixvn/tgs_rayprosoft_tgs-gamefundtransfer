using Microsoft.Extensions.Caching.Memory;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Core.Models;

namespace Tgs.GameFundTransfer.Repositories;

internal class GameProviderCacheRepository(
	IGameProviderRepository repository,
	IMemoryCache memoryCache,
	TimeSpan absoluteExpiration) : IGameProviderRepository
{
	public async ValueTask<GameProviderInfo> GetAsync(string gameProviderCode, CancellationToken cancellationToken = default)
	{
		var key = string.Concat(typeof(GameProviderCacheRepository).Namespace!.Replace(".", "-").ToLower(), ":", "gameprovider", ":", "get", ":", gameProviderCode);
		if (!memoryCache.TryGetValue(key, out GameProviderInfo? gameProviderInfo))
		{
			gameProviderInfo = await repository.GetAsync(
				gameProviderCode: gameProviderCode,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			if (gameProviderInfo is not null)
			{
				return memoryCache.Set(key, gameProviderInfo, absoluteExpiration);
			}
		}

		return gameProviderInfo!;
	}

	public IAsyncEnumerable<string> QueryAsync(CancellationToken cancellationToken = default)
		=> repository.QueryAsync(cancellationToken);
}
