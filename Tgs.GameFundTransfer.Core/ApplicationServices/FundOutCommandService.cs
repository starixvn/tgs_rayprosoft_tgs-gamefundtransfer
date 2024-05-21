using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.Exceptions;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundOutCommandService(
	ILogger<FundOutCommandService> logger,
	IGameCurrencyClient gameCurrencyClient,
	IFundsDistributedLock fundsDistributedLock,
	IGameProvidersClient gameProvidersClient,
	ICashServiceClient cashServiceClient,
	IGetService<FundProcessingFundOutAddQuery, FundProcessingResult> service,
	IFundTransferTransactionRepository fundTransferTransactionRepository) : ICommandService<FundOutCommand>
{
	public async ValueTask ExecuteAsync(FundOutCommand query, CancellationToken cancellationToken = default)
	{
		if (query.Amount < 0)
		{
			throw new AmountFormatException(query.Amount);
		}

		var exchangeRateFundResult = await gameCurrencyClient
			.ConvertGameProviderFundsAsync(
			gameProviderCode: query.GameProviderCode,
			currencyCode: query.CurrencyCode,
			amount: query.Amount,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var gameProviderAmount = exchangeRateFundResult.Amount;

		if (!await fundsDistributedLock
			.LockingAsync(
			gameProviderCode: query.GameProviderCode,
			playerCode: query.PlayerCode)
			.ConfigureAwait(false))
		{
			logger.LogWarning("LogOn:{logOn} Mesaage:Player ({playerCode}) funds transfer game provider ({gameProviderCode}) has been locked.",
				DateTime.Now.ToString("o"),
				query.PlayerCode,
				query.GameProviderCode);
			return;
		}

		try
		{
			_ = await gameProvidersClient.PlayerBalanceAsync(
				gameProviderCode: query.GameProviderCode,
				playerCode: query.PlayerCode,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			var playerBalance = await cashServiceClient.GetBalanceAsync(
				walletId: query.WalletId,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			if (playerBalance <= 0)
			{
				throw new MainWalletBalanceException(query.PlayerCode, playerBalance);
			}

			if (playerBalance - query.Amount < 0)
			{
				throw new MainWalletBalanceInsufficientException(query.PlayerCode, query.GameProviderCode, gameProviderAmount, playerBalance);
			}

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
				   externalId: fundProcessingResult.GameProviderFundTransferTransactionId.ToString(),
				   cancellationToken: cancellationToken)
				   .ConfigureAwait(false);

				if (transactionResponse == null)
				{
					throw new Exception("The main wallet transfer failed.");
				}
			}
			catch (Exception ex)
			{
				logger.LogError(exception: ex, "LogOn:{logOn} Mesaage:The main wallet's deduction failed.({gameProviderFundTransferTransactionId})",
					DateTime.Now.ToString("o"),
					fundProcessingResult.GameProviderFundTransferTransactionId);

				await fundTransferTransactionRepository.UpdateByMainWalletFundFailedAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
					gameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);
				throw;
			}

			if (transactionResponse.Balance < 0)
			{
				var description = $"The main wallet balance ({transactionResponse.Balance}) is negative to perform this transaction transaction id ({transactionResponse.TransactionId}) refund.";

				try
				{
					var fundRefundResponse = await cashServiceClient.GameProviderFundRefundAsync(
						walletId: query.WalletId,
						amount: query.Amount,
						description: transactionResponse.TransactionId.ToString(),
						externalId: fundProcessingResult.GameProviderFundTransferTransactionId.ToString(),
						cancellationToken: cancellationToken)
						.ConfigureAwait(false);

					await fundTransferTransactionRepository.UpdateByMainWalletFundRefundSuccessAsync(
						gameProviderFundTransferTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
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
						gameProviderFundTransferTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
						description: description,
						cancellationToken: cancellationToken)
						.ConfigureAwait(false);
				}
				finally
				{
					logger.LogWarning("The main wallet balance ({balance}) is negative to perform this transaction transaction id ({transactionId}) refund.", transactionResponse.Balance, transactionResponse.TransactionId);
				}

				return;
			}

			try
			{
				var gameProviderTransactionId = await gameProvidersClient
					.PlayerFundOutAsync(
					gameProviderCode: fundProcessingResult.GameProviderCode,
					brandCode: fundProcessingResult.BrandCode,
					playerCode: fundProcessingResult.PlayerCode,
					currencyCode: query.CurrencyCode,
					orderId: fundProcessingResult.GameProviderTransactionId,
					amount: gameProviderAmount,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				if (string.IsNullOrWhiteSpace(gameProviderTransactionId))
				{
					throw new Exception($"The game manufacturer's transfer failed.({fundProcessingResult.GameProviderFundTransferTransactionId})");
				}

				await fundTransferTransactionRepository
					.UpdateBySuccessAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
					transactionId: transactionResponse.TransactionId,
					gameProviderTransactionId: gameProviderTransactionId,
					balance: transactionResponse.Balance,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				logger.LogError(exception: ex, "The game manufacturer's transfer failed.({gameProviderFundTransferTransactionId})", fundProcessingResult.GameProviderFundTransferTransactionId);

				await fundTransferTransactionRepository.UpdateByGameProviderFundFailedAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);
			}
		}
		finally
		{
			await fundsDistributedLock
				.UnlockAsync(
				gameProviderCode: query.GameProviderCode,
				playerCode: query.PlayerCode)
				.ConfigureAwait(false);
		}
	}
}
