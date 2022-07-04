namespace Tgs.GameFundTransfer.Core
{
    public interface IFundTransferTransactionRepository
    {
        ValueTask<Guid> AddFundInAsync(Guid gameProviderId, string gameProviderCode, string playerCode, Guid walletId, decimal amount, CancellationToken cancellationToken = default);

        ValueTask<Guid> AddFundOutAsync(Guid gameProviderId, string gameProviderCode, string playerCode, Guid walletId, decimal amount, CancellationToken cancellationToken = default);

        ValueTask UpdateByFailureOnGameProviderAsync(Guid gameProviderFundTransferTransactionId, CancellationToken cancellationToken = default);

        ValueTask UpdateByFailureOnMainWalletAsync(Guid gameProviderFundTransferTransactionId, string gameProviderTransactionId, CancellationToken cancellationToken = default);

        ValueTask UpdateBySuccessAsync(Guid gameProviderFundTransferTransactionId, Guid? transactionsId, string gameProviderTransactionId, decimal postBalance, CancellationToken cancellationToken = default);
    }
}
