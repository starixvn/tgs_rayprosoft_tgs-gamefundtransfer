using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Tgs.GameFundTransfer.Core;

namespace Tgs.GameFundTransfer.Repositories;

internal class FundsDistributedLock(
	IDatabase database,
	IOptions<FundsLockedOptions> options) : IFundsDistributedLock
{
	public Task<bool> LockingAsync(string gameProviderCode, string playerCode)
		=> database.StringSetAsync(
			key: GetKey(gameProviderCode, playerCode),
			value: DateTime.UtcNow.ToString("O"),
			expiry: TimeSpan.FromSeconds(options.Value.LockSeconds),
			when: When.NotExists);

	public Task UnlockAsync(string gameProviderCode, string playerCode)
		=> database.KeyDeleteAsync(
			key: GetKey(gameProviderCode, playerCode),
			flags: CommandFlags.FireAndForget);

	private static string GetKey(string gameProviderCode, string playerCode)
		=> string.Format("fundslocked:{0}:{1}", gameProviderCode, playerCode).ToLower();
}
