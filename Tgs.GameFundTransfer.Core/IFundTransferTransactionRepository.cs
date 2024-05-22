using Tgs.GameFundTransfer.Common.Enums;

namespace Tgs.GameFundTransfer.Core;

public interface IFundTransferTransactionRepository
{
	ValueTask AddProcessingAsync(Guid gameProviderFundTransferTransactionId, Guid gameProviderId, Guid walletId, string gameProviderTransactionId, string playerCode, FundSource fundSource, string fromWallet, string toWallet, decimal amount, CancellationToken cancellationToken = default);

	ValueTask UpdateByGameProviderFundFailedAsync(Guid gameProviderFundTransferTransactionId, CancellationToken cancellationToken = default);

	ValueTask UpdateByMainWalletFundFailedAsync(Guid gameProviderFundTransferTransactionId, CancellationToken cancellationToken = default);

	ValueTask UpdateByMainWalletFundRefundFailedAsync(Guid gameProviderFundTransferTransactionId, string description, CancellationToken cancellationToken = default);

	ValueTask UpdateByMainWalletFundRefundSuccessAsync(Guid gameProviderFundTransferTransactionId, Guid transactionId, decimal balance, string description, CancellationToken cancellationToken = default);

	ValueTask UpdateBySuccessAsync(Guid gameProviderFundTransferTransactionId, Guid transactionId, decimal balance, CancellationToken cancellationToken = default);
}
