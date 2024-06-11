using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;
using Tgs.GameFundTransfer.Core.ApplicationServices;

namespace Tgs.GameFundTransfer.Core;

internal class FundBalanceCacheDispatcher(IServiceProvider serviceProvider) : IFundBalanceCacheDispatcher, IDisposable, IAsyncDisposable
{
	private readonly ConcurrentDictionary<string, FundBalanceCacheChannel> _counters = new();
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	private bool _disposedValue;

	~FundBalanceCacheDispatcher()
	{
		Dispose(false);
	}

	public async ValueTask PushCommandAsync(FundBalanceCacheCommand command, CancellationToken cancellationToken = default)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

		var counter = _counters.GetOrAdd(
			command.GameProviderCode,
			key =>
			{
				var scope = serviceProvider.CreateScope();
				var newCounter = ActivatorUtilities.CreateInstance<FundBalanceCacheChannel>(
					scope.ServiceProvider,
					key);

				newCounter.Disposed += (_, _) =>
				{
					_ = _counters.TryRemove(newCounter.GameProviderCode, out _);
					scope.Dispose();
				};

				return newCounter;
			});

		await counter.PrepareAsync(cts.Token).ConfigureAwait(false);
		await counter.PushCommandAsync(command, cts.Token).ConfigureAwait(false);
	}

	public ValueTask DisposeAsync()
	{
		Dispose(true);
		GC.SuppressFinalize(this);

		return ValueTask.CompletedTask;
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
				_cancellationTokenSource.Cancel();
			}

			_cancellationTokenSource.Dispose();
			_disposedValue = true;
		}
	}
}
