using Tgs.GameFundTransfer.Common.ViewModels;

namespace Tgs.GameFundTransfer.Client;

public interface IGameFundTransferClient
{
	ValueTask<FundTransferBalanceResultViewModel> FundTransferBalanceAsync(string playerCode, string gameProviderCode, CancellationToken cancellationToken = default);

	ValueTask<FundTransferResultViewModel?> FundTransferInAsync(string playerCode, string gameProviderCode, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<FundTransferResultViewModel?> FundTransferInOneClickAsync(string playerCode, string gameProviderCode, CancellationToken cancellationToken = default);

	ValueTask<FundTransferResultViewModel?> FundTransferOutAsync(string playerCode, string gameProviderCode, decimal amount, CancellationToken cancellationToken = default);
}
