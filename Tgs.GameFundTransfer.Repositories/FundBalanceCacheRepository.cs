using System.Text.Json;
using StackExchange.Redis;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Core.Models;

namespace Tgs.GameFundTransfer.Repositories;

internal class FundBalanceCacheRepository(
	IFundBalanceRepository fundBalanceRepository,
	IDatabase database,
	TimeSpan expiry) : IFundBalanceRepository
{
	public async ValueTask<FundBalanceInfo> GetAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default)
	{
		var key = GetKey(gameProviderCode, playerCode);

		if (await database.KeyExistsAsync(
			key: key)
			.ConfigureAwait(false))
		{
			var redisValue = await database.StringGetAsync(key).ConfigureAwait(false);
			if (redisValue.HasValue)
			{
				return JsonSerializer.Deserialize<FundBalanceInfo>(redisValue!)!;
			}
		}

		return await fundBalanceRepository.GetAsync(
			gameProviderCode: gameProviderCode,
			playerCode: playerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<bool> ReleaseAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default)
		=> await database.KeyDeleteAsync(
			key: GetKey(gameProviderCode, playerCode),
			flags: CommandFlags.FireAndForget)
		.ConfigureAwait(false);

	public async ValueTask SettingsAsync(string gameProviderCode, string playerCode, FundBalanceInfo source, CancellationToken cancellationToken = default)
	{
		var key = GetKey(gameProviderCode, playerCode);

		_ = await database.StringSetAsync(
			key: key,
			value: JsonSerializer.Serialize(source),
			expiry: expiry)
			.ConfigureAwait(false);
	}

	public async ValueTask<bool> ExistsAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default)
	{
		var key = GetKey(gameProviderCode, playerCode);
		return await database.KeyExistsAsync(key).ConfigureAwait(false);
	}

	private static string GetKey(string gameProviderCode, string playerCode)
		=> string.Concat(typeof(FundBalanceCacheRepository).Namespace!.Replace(".", "-").ToLower(), ":", "balance", ":", gameProviderCode, ":", playerCode);
}
