using Microsoft.Extensions.Caching.Memory;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Core.Models;

namespace Tgs.GameFundTransfer.Repositories;

internal class PlayerCacheRepository(
	IPlayerRepository repository,
	IMemoryCache memoryCache,
	TimeSpan absoluteExpiration) : IPlayerRepository
{
	public async ValueTask<PlayerInfo> GetAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var key = string.Concat(typeof(PlayerCacheRepository).Namespace!.Replace(".", "-").ToLower(), ":", "player", ":", "get", ":", playerCode);

		if (!memoryCache.TryGetValue(key, out PlayerInfo? playerInfo))
		{
			playerInfo = await repository.GetAsync(
				playerCode: playerCode,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			if (playerInfo is not null)
			{
				return memoryCache.Set(key, playerInfo, absoluteExpiration);
			}
		}

		return playerInfo!;
	}
}
