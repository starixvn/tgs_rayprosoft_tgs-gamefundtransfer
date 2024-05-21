using Microsoft.EntityFrameworkCore;
using Tgs.GameFundTransfer.Common.Enums;

using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Entities;

namespace Tgs.GameFundTransfer.Repositories;

internal class FundTransferTransactionRepository(
	TgsContext context) : IFundTransferTransactionRepository
{
	public async ValueTask AddProcessingAsync(Guid gameProviderFundTransferTransactionId, Guid gameProviderId, Guid walletId, string gameProviderTransactionId, string playerCode, FundSource fundSource, string fromWallet, string toWallet, decimal amount, CancellationToken cancellationToken = default)
	{
		var systemDateTime = DateTime.UtcNow;
		var fundTransferTransaction = new GameProviderFundTransferTransaction()
		{
			GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
			GameProviderId = gameProviderId,
			WalletId = walletId,
			GameProviderTransactionId = gameProviderTransactionId,
			PlayerCode = playerCode,
			FundSource = (short)fundSource,
			FromWallet = fromWallet,
			ToWallet = toWallet,
			Amount = amount,
			Status = (short)FundStatus.Processing,
			CreatedOn = systemDateTime,
			UpdatedOn = systemDateTime
		};
		_ = await context.GameProviderFundTransferTransactions.AddAsync(
			entity: fundTransferTransaction,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		_ = await context.SaveChangesAsync(
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask UpdateByGameProviderFundFailedAsync(Guid gameProviderFundTransferTransactionId, CancellationToken cancellationToken = default)
	{
		var fundTransferTransaction = await context.GameProviderFundTransferTransactions
			.Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
			.FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

		fundTransferTransaction.TransactionId = null;
		fundTransferTransaction.Status = (short)FundStatus.GameProviderFundFailed;
		fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

		_ = context.GameProviderFundTransferTransactions
		   .Update(fundTransferTransaction);

		_ = await context
			 .SaveChangesAsync(
			cancellationToken: cancellationToken)
			 .ConfigureAwait(false);
	}

	public async ValueTask UpdateByMainWalletFundFailedAsync(Guid gameProviderFundTransferTransactionId, string gameProviderTransactionId, CancellationToken cancellationToken = default)
	{
		var fundTransferTransaction = await context.GameProviderFundTransferTransactions
			.Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
			.FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

		fundTransferTransaction.GameProviderTransactionId = gameProviderTransactionId;
		fundTransferTransaction.Status = (short)FundStatus.MainWalletFundFailed;
		fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

		_ = context.GameProviderFundTransferTransactions
		   .Update(fundTransferTransaction);

		_ = await context
			 .SaveChangesAsync(
			cancellationToken: cancellationToken)
			 .ConfigureAwait(false);
	}

	public async ValueTask UpdateByMainWalletFundRefundFailedAsync(Guid gameProviderFundTransferTransactionId, string description, CancellationToken cancellationToken = default)

	{
		var fundTransferTransaction = await context.GameProviderFundTransferTransactions
			.Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
			.FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

		fundTransferTransaction.Status = (short)FundStatus.MainWalletFundRefundFailed;
		fundTransferTransaction.Description = description;
		fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

		_ = context.GameProviderFundTransferTransactions
		   .Update(fundTransferTransaction);

		_ = await context
			 .SaveChangesAsync(
			cancellationToken: cancellationToken)
			 .ConfigureAwait(false);
	}

	public async ValueTask UpdateByMainWalletFundRefundSuccessAsync(Guid gameProviderFundTransferTransactionId, Guid transactionId, decimal balance, string description, CancellationToken cancellationToken = default)
	{
		var fundTransferTransaction = await context.GameProviderFundTransferTransactions
			.Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
			.FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

		fundTransferTransaction.TransactionId = transactionId;
		fundTransferTransaction.Status = (short)FundStatus.MainWalletFundRefundSuccess;
		fundTransferTransaction.Description = description;
		fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

		_ = context.GameProviderFundTransferTransactions
		   .Update(fundTransferTransaction);

		_ = await context
			 .SaveChangesAsync(
			cancellationToken: cancellationToken)
			 .ConfigureAwait(false);
	}

	public async ValueTask UpdateBySuccessAsync(Guid gameProviderFundTransferTransactionId, Guid transactionId, string gameProviderTransactionId, decimal balance, CancellationToken cancellationToken = default)
	{
		var fundTransferTransaction = await context.GameProviderFundTransferTransactions
			.Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
			.FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

		fundTransferTransaction.TransactionId = transactionId;
		fundTransferTransaction.GameProviderTransactionId = gameProviderTransactionId;
		fundTransferTransaction.Status = (short)FundStatus.Success;
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
