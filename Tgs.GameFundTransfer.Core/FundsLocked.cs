using StackExchange.Redis;

namespace Tgs.GameFundTransfer.Core
{
    internal class FundsLocked : IFundsLocked
    {
        private readonly IDatabase _database;
        private readonly string _redisKey = "fundslocked:{0}:{1}";

        public FundsLocked(IDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
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
                expiry: TimeSpan.FromMinutes(1),
                when: When.NotExists);
        }

        public async ValueTask UnlockAsync(string gameProviderCode, string playerCode)
            => await _database.KeyDeleteAsync(
                key: string.Format(_redisKey, gameProviderCode, playerCode).ToLower(),
                flags: CommandFlags.FireAndForget).ConfigureAwait(false);
    }
}
