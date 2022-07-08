using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Tgs.GameFundTransfer.Core
{
    internal class FundsLocked : IFundsLocked
    {
        private readonly IDatabase _database;
        private readonly FundsLockedOptions _fundslocked;
        private readonly string _redisKey = "fundslocked:{0}:{1}";

        public FundsLocked(
            IDatabase database,
            IOptions<FundsLockedOptions> options)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _fundslocked = options.Value;
        }

        public Task<bool> ExistAsync(string gameProviderCode, string playerCode)
        {
            try
            {
                return _database.KeyExistsAsync(string.Format(_redisKey, gameProviderCode, playerCode).ToLower());
            }
            catch
            {
                return Task.FromResult(true);
            }
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

        public async ValueTask UnlockAsync(string gameProviderCode, string playerCode)
            => await _database.KeyDeleteAsync(
                key: string.Format(_redisKey, gameProviderCode, playerCode).ToLower(),
                flags: CommandFlags.FireAndForget).ConfigureAwait(false);
    }
}
