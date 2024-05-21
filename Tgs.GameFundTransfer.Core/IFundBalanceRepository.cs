using Tgs.GameFundTransfer.Core.Models;

namespace Tgs.GameFundTransfer.Core;

public interface IFundBalanceRepository
{
	ValueTask SettingsAsync(string gameProviderCode, string playerCode, FundBalanceInfo source, CancellationToken cancellationToken = default);

	ValueTask<FundBalanceInfo> GetAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default);

	ValueTask<bool> ReleaseAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default);

	ValueTask<bool> ExistsAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default);
}
