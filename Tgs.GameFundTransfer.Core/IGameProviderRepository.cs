namespace Tgs.GameFundTransfer.Core;

public interface IGameProviderRepository
{
	ValueTask<Models.GameProviderInfo> GetAsync(string gameProviderCode, CancellationToken cancellationToken = default);

	IAsyncEnumerable<string> QueryAsync(CancellationToken cancellationToken = default);
}
