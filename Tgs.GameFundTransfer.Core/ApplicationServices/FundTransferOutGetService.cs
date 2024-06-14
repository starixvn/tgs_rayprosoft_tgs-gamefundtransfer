using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.Exceptions;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundTransferOutGetService(
	ILogger<FundTransferOutGetService> logger,
	IGameCurrencyClient gameCurrencyClient,
	IFundsDistributedLock fundsDistributedLock,
	IGameProvidersClient gameProvidersClient,
	ICashServiceClient cashServiceClient,
	IGetService<FundProcessingFundOutAddQuery, FundProcessingResult> service,
	IGetService<GameProviderFundOutQuery, GameProviderFundOutResult> gameProviderFundOutService,
	IFundTransferTransactionRepository fundTransferTransactionRepository) : IGetService<FundTransferOutQuery, FundTransferResult>
{
	public async ValueTask<FundTransferResult> GetAsync(FundTransferOutQuery query, CancellationToken cancellationToken = default)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.Amount);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Amount, 10000000.00m);

		var exchangeRateFundResult = await gameCurrencyClient.ConvertGameProviderFundsAsync(
			gameProviderCode: query.GameProviderCode,
			currencyCode: query.CurrencyCode,
			amount: query.Amount,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var gameProviderAmount = exchangeRateFundResult.Amount;

		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gameProviderAmount);
		ArgumentOutOfRangeException.ThrowIfLessThan(gameProviderAmount, 0.01m);

		_ = await gameProvidersClient.PlayerBalanceAsync(
			gameProviderCode: query.GameProviderCode,
			playerCode: query.PlayerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var balance = await cashServiceClient.GetBalanceAsync(
			walletId: query.WalletId,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(balance);
		ArgumentOutOfRangeException.ThrowIfLessThan(balance - query.Amount, 0.0m);

		await fundsDistributedLock.LockingAsync(
			gameProviderCode: query.GameProviderCode,
			playerCode: query.PlayerCode)
			.ConfigureAwait(false);

		try
		{
			var fundProcessingResult = await service.GetAsync(
				query: new FundProcessingFundOutAddQuery(
					GameProviderCode: query.GameProviderCode,
					PlayerCode: query.PlayerCode,
					Amount: query.Amount),
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			TransactionResponse transactionResponse;
			try
			{
				transactionResponse = await cashServiceClient.GameProviderFundOutAsync(
				   walletId: query.WalletId,
				   amount: query.Amount,
				   description: $"【Default】->【{query.GameProviderCode}】",
				   externalId: fundProcessingResult.CashTransactionId,
				   cancellationToken: cancellationToken)
				   .ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				logger.LogError(exception: ex, "LogOn:{logOn} Mesaage:The main wallet's deduction failed.({gameProviderFundTransferTransactionId})",
					DateTime.Now.ToString("o"),
					fundProcessingResult.FundTransactionId);

				await fundTransferTransactionRepository.UpdateByMainWalletFundFailedAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.FundTransactionId,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);
				throw;
			}

			if (transactionResponse.Balance < 0)
			{
				var description = $"This CashTransactionIdentifier({fundProcessingResult.CashTransactionId}) conducts the player({fundProcessingResult.PlayerCode}) main wallet transaction resulting in Balance({transactionResponse.Balance}) being negative and CashResponseTransactionIdentifier({transactionResponse.TransactionId}).";

				try
				{
					var fundRefundResponse = await cashServiceClient.GameProviderFundRefundAsync(
						walletId: query.WalletId,
						amount: query.Amount,
						description: transactionResponse.TransactionId.ToString(),
						externalId: fundProcessingResult.CashTransactionId,
						cancellationToken: cancellationToken)
						.ConfigureAwait(false);

					await fundTransferTransactionRepository.UpdateByMainWalletFundRefundSuccessAsync(
						gameProviderFundTransferTransactionId: fundProcessingResult.FundTransactionId,
						transactionId: fundRefundResponse.TransactionId,
						balance: fundRefundResponse.Balance,
						description: description,
						cancellationToken: cancellationToken)
						.ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					logger.LogError(exception: ex, "This transaction id({transactionId}) main wallet fund refund failed.", transactionResponse.TransactionId);

					await fundTransferTransactionRepository
						.UpdateByMainWalletFundRefundFailedAsync(
						gameProviderFundTransferTransactionId: fundProcessingResult.FundTransactionId,
						description: description,
						cancellationToken: cancellationToken)
						.ConfigureAwait(false);

					throw;
				}

				throw new CashTransferNegativeException(fundProcessingResult.CashTransactionId, transactionResponse.TransactionId, fundProcessingResult.PlayerCode, transactionResponse.Balance);
			}

			try
			{
				var gameProviderFundOutResult = await gameProviderFundOutService.GetAsync(
					query: new GameProviderFundOutQuery(
						GameProviderCode: fundProcessingResult.GameProviderCode,
						PlayerCode: fundProcessingResult.PlayerCode,
						GameTransactionId: fundProcessingResult.GameTransactionId,
						Amount: gameProviderAmount),
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				await fundTransferTransactionRepository.UpdateBySuccessAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.FundTransactionId,
					transactionId: transactionResponse.TransactionId,
					balance: transactionResponse.Balance,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				return new FundTransferResult(
					FundTransactionId: fundProcessingResult.FundTransactionId,
					CashTransactionId: fundProcessingResult.CashTransactionId,
					CashRespTransactionId: transactionResponse.TransactionId,
					GameTransactionId: fundProcessingResult.GameTransactionId,
					GameRespTransactionId: gameProviderFundOutResult.GameRespTransactionId);
			}
			catch (ArgumentException ex)
			{
				await fundTransferTransactionRepository.UpdateByGameProviderFundFailedAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.FundTransactionId,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				logger.LogError(ex, "LogOn:{logOn} Mesaage:The transaction number provided by this game provider ({gameProviderCode}) is empty.({gameProviderFundTransferTransactionId})",
					DateTime.Now.ToString("o"),
					fundProcessingResult.GameProviderCode,
					fundProcessingResult.FundTransactionId);

				throw;
			}
			catch (Exception ex)
			{
				await fundTransferTransactionRepository.UpdateByGameProviderFundFailedAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.FundTransactionId,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				logger.LogError(ex, "LogOn:{logOn} Mesaage:This game provider({gameProviderCode}) call failed.({gameProviderFundTransferTransactionId})",
					DateTime.Now.ToString("o"),
					fundProcessingResult.GameProviderCode,
					fundProcessingResult.FundTransactionId);

				throw;
			}
		}
		finally
		{
			await fundsDistributedLock.UnlockAsync(
				gameProviderCode: query.GameProviderCode,
				playerCode: query.PlayerCode)
				.ConfigureAwait(false);
		}
	}
}
