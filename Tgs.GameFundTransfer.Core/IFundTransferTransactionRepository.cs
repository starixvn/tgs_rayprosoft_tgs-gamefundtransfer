namespace Tgs.GameFundTransfer.Core
{
    public interface IFundTransferTransactionRepository
    {
        ValueTask<Guid> AddFundInAsync(Guid gameProviderId, Guid WalletId, string playerCode, string gameProviderCode, decimal amount, string description);

        ValueTask<Guid> AddFundOutAsync(Guid gameProviderId, Guid WalletId, string playerCode, string gameProviderCode, decimal amount, string description);

        ValueTask UpdateByFailureOnGameProviderAsync(Guid gameProviderFundTransferTransactionId, Guid? transactionsId, string gameProviderTransactionId, decimal postBalance, string description);

        ValueTask UpdateByFailureOnMainWalletAsync(Guid gameProviderFundTransferTransactionId, Guid? transactionsId, string gameProviderTransactionId, decimal postBalance, string description);

        ValueTask UpdateBySuccessAsync(Guid gameProviderFundTransferTransactionId, Guid? transactionsId, string gameProviderTransactionId, decimal postBalance, string description);
    }
}
