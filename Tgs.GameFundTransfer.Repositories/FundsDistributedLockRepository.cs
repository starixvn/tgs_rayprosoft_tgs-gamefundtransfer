using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Tgs.GameFundTransfer.Core;

namespace Tgs.GameFundTransfer.Repositories;

internal class FundsDistributedLock : IFundsDistributedLock
{
    private readonly IDatabase _database;
    private readonly FundsLockedOptions _fundslocked;
    private readonly string _redisKey = "fundslocked:{0}:{1}";

    public FundsDistributedLock(
        IDatabase database,
        IOptions<FundsLockedOptions> options)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _fundslocked = options.Value;
    }

    public Task<bool> LockingAsync(string gameProviderCode, string playerCode)
    {
        string key = string.Format(_redisKey, gameProviderCode, playerCode).ToLower();
        return _database.StringSetAsync(
            key: key,
            value: DateTime.UtcNow.ToString("O"),
            expiry: TimeSpan.FromSeconds(_fundslocked.LockSeconds),
            when: When.NotExists);
    }

    public Task UnlockAsync(string gameProviderCode, string playerCode)
        => _database.KeyDeleteAsync(
            key: string.Format(_redisKey, gameProviderCode, playerCode).ToLower(),
            flags: CommandFlags.FireAndForget);
}
