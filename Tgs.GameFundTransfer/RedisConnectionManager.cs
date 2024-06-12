using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Tgs.GameFundTransfer;

internal class RedisConnectionManager : IDisposable
{
	private readonly ConnectionMultiplexer _connectionMultiplexer;
	private bool _disposedValue;

	public IConnectionMultiplexer Connection { get; }

	public IDatabase Database { get; }

	public RedisConnectionManager(IOptions<RedisOptions> optionsAccessor)
	{
		ArgumentNullException.ThrowIfNull(optionsAccessor);

		var options = optionsAccessor.Value;

		var connectionOptions = ConfigurationOptions.Parse(options.ConnectionString);

		_connectionMultiplexer = ConnectionMultiplexer.Connect(connectionOptions);
		Connection = _connectionMultiplexer;
		Database = _connectionMultiplexer.GetDatabase();

	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_connectionMultiplexer.Close();
				_connectionMultiplexer.Dispose();
			}
			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			_disposedValue = true;
		}
	}
}
