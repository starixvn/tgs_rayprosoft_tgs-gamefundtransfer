using Microsoft.EntityFrameworkCore;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Repositories.Entities;
using Tgs.SerialNumberGenerator;

namespace Tgs.GameFundTransfer.Repositories;

internal class FundTransferTransactionRepository(
    TgsContext context,
    ISerialNumberGenerator serialNumberGenerator) : IFundTransferTransactionRepository
{
    public async ValueTask<Guid> AddFundInAsync(
        Guid gameProviderId,
        string gameProviderCode,
        string playerCode,
        Guid walletId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var gameProviderFundTransferTransactionId = await serialNumberGenerator
            .GenerateAsync(
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var systemDateTime = DateTime.UtcNow;
        var fundTransferTransaction = new GameProviderFundTransferTransaction()
        {
            GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
            GameProviderId = gameProviderId,
            WalletId = walletId,
            PlayerCode = playerCode,
            FundSource = (short)FundSource.FundIn,
            FromWallet = gameProviderCode,
            ToWallet = WalletDef.MainWallet,
            Amount = amount,
            Status = (short)FundTransferTransactionStatus.GameProviderFundFailed,
            CreatedOn = systemDateTime,
            UpdatedOn = systemDateTime
        };
        _ = await context.GameProviderFundTransferTransactions
            .AddAsync(
            entity: fundTransferTransaction,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _ = await context
            .SaveChangesAsync(
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return gameProviderFundTransferTransactionId;
    }

    public async ValueTask<Guid> AddFundOutAsync(
        Guid gameProviderId,
        string gameProviderCode,
        string playerCode,
        Guid walletId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var gameProviderFundTransferTransactionId = await serialNumberGenerator
            .GenerateAsync(
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var systemDateTime = DateTime.UtcNow;
        var fundTransferTransaction = new GameProviderFundTransferTransaction()
        {
            GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
            GameProviderId = gameProviderId,
            WalletId = walletId,
            PlayerCode = playerCode,
            FundSource = (short)FundSource.FundOut,
            FromWallet = WalletDef.MainWallet,
            ToWallet = gameProviderCode,
            Amount = -amount,
            Status = (short)FundTransferTransactionStatus.MainWalletFundFailed,
            CreatedOn = systemDateTime,
            UpdatedOn = systemDateTime
        };
        _ = await context.GameProviderFundTransferTransactions
            .AddAsync(
            entity: fundTransferTransaction,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _ = await context
            .SaveChangesAsync(
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return gameProviderFundTransferTransactionId;
    }

    public async ValueTask UpdateByGameProviderFundFailedAsync(
        Guid gameProviderFundTransferTransactionId,
        CancellationToken cancellationToken = default)
    {
        var fundTransferTransaction = await context.GameProviderFundTransferTransactions
            .Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        fundTransferTransaction.TransactionId = null;
        fundTransferTransaction.GameProviderTransactionId = null;
        fundTransferTransaction.Status = (short)FundTransferTransactionStatus.GameProviderFundFailed;
        fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

        _ = context.GameProviderFundTransferTransactions
           .Update(fundTransferTransaction);

        _ = await context
             .SaveChangesAsync(
            cancellationToken: cancellationToken)
             .ConfigureAwait(false);
    }

    public async ValueTask UpdateByMainWalletFundFailedAsync(
        Guid gameProviderFundTransferTransactionId,
        string gameProviderTransactionId,
        CancellationToken cancellationToken = default)
    {
        var fundTransferTransaction = await context.GameProviderFundTransferTransactions
            .Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        fundTransferTransaction.GameProviderTransactionId = gameProviderTransactionId;
        fundTransferTransaction.Status = (short)FundTransferTransactionStatus.MainWalletFundFailed;
        fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

        _ = context.GameProviderFundTransferTransactions
           .Update(fundTransferTransaction);

        _ = await context
             .SaveChangesAsync(
            cancellationToken: cancellationToken)
             .ConfigureAwait(false);
    }

    public async ValueTask UpdateByMainWalletFundRefundFailedAsync(
        Guid gameProviderFundTransferTransactionId,
        string description,
        CancellationToken cancellationToken = default)

    {
        var fundTransferTransaction = await context.GameProviderFundTransferTransactions
            .Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        fundTransferTransaction.Status = (short)FundTransferTransactionStatus.MainWalletFundRefundFailed;
        fundTransferTransaction.Description = description;
        fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

        _ = context.GameProviderFundTransferTransactions
           .Update(fundTransferTransaction);

        _ = await context
             .SaveChangesAsync(
            cancellationToken: cancellationToken)
             .ConfigureAwait(false);
    }

    public async ValueTask UpdateByMainWalletFundRefundSuccessAsync(
        Guid gameProviderFundTransferTransactionId,
        Guid transactionId,
        decimal balance,
        string description,
        CancellationToken cancellationToken = default)
    {
        var fundTransferTransaction = await context.GameProviderFundTransferTransactions
            .Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        fundTransferTransaction.TransactionId = transactionId;
        fundTransferTransaction.Status = (short)FundTransferTransactionStatus.MainWalletFundRefundSuccess;
        fundTransferTransaction.Description = description;
        fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

        _ = context.GameProviderFundTransferTransactions
           .Update(fundTransferTransaction);

        _ = await context
             .SaveChangesAsync(
            cancellationToken: cancellationToken)
             .ConfigureAwait(false);
    }

    public async ValueTask UpdateBySuccessAsync(
        Guid gameProviderFundTransferTransactionId,
        Guid transactionId,
        string gameProviderTransactionId,
        decimal balance,
        CancellationToken cancellationToken = default)
    {
        var fundTransferTransaction = await context.GameProviderFundTransferTransactions
            .Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
            .FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        fundTransferTransaction.TransactionId = transactionId;
        fundTransferTransaction.GameProviderTransactionId = gameProviderTransactionId;
        fundTransferTransaction.Status = (short)FundTransferTransactionStatus.Success;
        fundTransferTransaction.Balance = balance;
        fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

        _ = context.GameProviderFundTransferTransactions
           .Update(fundTransferTransaction);

        _ = await context
            .SaveChangesAsync(
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
