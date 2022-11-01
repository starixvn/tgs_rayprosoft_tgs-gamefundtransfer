namespace Tgs.GameFundTransfer.Core;

public interface IFundTransferTransactionRepository
{
    ValueTask<Guid> AddFundInAsync(Guid gameProviderId, string gameProviderCode, string playerCode, Guid walletId, decimal amount, CancellationToken cancellationToken = default);

    ValueTask<Guid> AddFundOutAsync(Guid gameProviderId, string gameProviderCode, string playerCode, Guid walletId, decimal amount, CancellationToken cancellationToken = default);

    ValueTask UpdateByGameProviderFundFailedAsync(Guid gameProviderFundTransferTransactionId, CancellationToken cancellationToken = default);

    ValueTask UpdateByMainWalletFundFailedAsync(Guid gameProviderFundTransferTransactionId, string gameProviderTransactionId, CancellationToken cancellationToken = default);

    ValueTask UpdateByMainWalletFundRefundFailedAsync(Guid gameProviderFundTransferTransactionId, string description, CancellationToken cancellationToken = default);

    ValueTask UpdateByMainWalletFundRefundSuccessAsync(Guid gameProviderFundTransferTransactionId, Guid transactionId, decimal balance, string description, CancellationToken cancellationToken = default);

    ValueTask UpdateBySuccessAsync(Guid gameProviderFundTransferTransactionId, Guid transactionId, string gameProviderTransactionId, decimal balance, CancellationToken cancellationToken = default);
}
