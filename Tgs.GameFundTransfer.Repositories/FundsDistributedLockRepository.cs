using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Core.Exceptions;

namespace Tgs.GameFundTransfer.Repositories;

internal class FundsDistributedLock(
	IDatabase database,
	IOptions<FundsLockedOptions> options) : IFundsDistributedLock
{
	public async ValueTask LockingAsync(string gameProviderCode, string playerCode)
	{
		var locking = await database.StringSetAsync(
			key: GetKey(gameProviderCode, playerCode),
			value: DateTime.UtcNow.ToString("O"),
			expiry: TimeSpan.FromSeconds(options.Value.LockSeconds),
		when: When.NotExists)
		.ConfigureAwait(false);

		if (!locking)
		{
			throw new FundTransferException(gameProviderCode: gameProviderCode, playerCode: playerCode);
		}
	}

	public async ValueTask UnlockAsync(string gameProviderCode, string playerCode)
		=> _ = await database.KeyDeleteAsync(
			key: GetKey(gameProviderCode, playerCode),
			flags: CommandFlags.FireAndForget)
		.ConfigureAwait(false);

	private static string GetKey(string gameProviderCode, string playerCode)
		=> string.Format("fundslocked:{0}:{1}", gameProviderCode, playerCode).ToLower();
}
