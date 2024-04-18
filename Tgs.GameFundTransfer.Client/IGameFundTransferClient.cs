using Tgs.GameFundTransfer.Protos;

namespace Tgs.GameFundTransfer.Client;

public interface IGameFundTransferClient
{
	ValueTask<string> CheckingServiceAsync(string machineName, CancellationToken cancellationToken = default);

	ValueTask<bool> FundInAsync(Guid gameProviderId, string gameProviderCode, string brandCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<GameFundTransferResultReply> FundInResultAsync(Guid gameProviderId, string gameProviderCode, string brandCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<GameFundTransferResultReply> FundInZeroResultAsync(Guid gameProviderId, string gameProviderCode, string brandCode, string playerCode, Guid walletId, string currencyCode, CancellationToken cancellationToken = default);

	ValueTask<bool> FundOutAsync(Guid gameProviderId, string gameProviderCode, string brandCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default);
}
