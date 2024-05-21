using Tgs.GameFundTransfer.Protos;

namespace Tgs.GameFundTransfer.Client;

public interface IGameFundTransferClient
{
	ValueTask<string> CheckingServiceAsync(string machineName, CancellationToken cancellationToken = default);

	ValueTask<FundBalanceReply> FundBalanceAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default);

	[Obsolete("已過時,請改使用FundInResultAsync")]
	ValueTask<bool> FundInAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<GameFundTransferResultReply> FundInResultAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<GameFundTransferResultReply> FundInZeroResultAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, CancellationToken cancellationToken = default);

	ValueTask<bool> FundOutAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default);
}
