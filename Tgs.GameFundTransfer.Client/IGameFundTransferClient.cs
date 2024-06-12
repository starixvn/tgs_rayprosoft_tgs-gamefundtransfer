using Tgs.GameFundTransfer.Protos;

namespace Tgs.GameFundTransfer.Client;

public interface IGameFundTransferClient
{
	ValueTask<FundBalanceReply> FundBalanceAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default);

	[Obsolete("已過時，請改使用【FundTransferInAsync】", error: true)]
	ValueTask<GameFundTransferResultReply> FundInResultAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default);
	[Obsolete("已過時，請改使用【FundTransferInOneClickAsync】", error: true)]
	ValueTask<GameFundTransferResultReply> FundInZeroResultAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, CancellationToken cancellationToken = default);
	[Obsolete("已過時，請改使用【FundTransferOutAsync】", error: true)]
	ValueTask<bool> FundOutAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<FundTransferReply> FundTransferInAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<FundTransferReply> FundTransferInOneClickAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, CancellationToken cancellationToken = default);

	ValueTask<FundTransferReply> FundTransferOutAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default);
}
