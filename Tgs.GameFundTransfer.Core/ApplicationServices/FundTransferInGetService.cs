using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundTransferInGetService(
	ILogger<FundTransferInGetService> logger,
	IGameCurrencyClient gameCurrencyClient,
	IFundsDistributedLock fundsDistributedLock,
	IGameProvidersClient gameProvidersClient,
	ICashServiceClient cashServiceClient,
	IGetService<FundProcessingFundInAddQuery, FundProcessingResult> service,
	IGetService<GameProviderFundInQuery, GameProviderFundInResult> gameProviderFundInService,
	IFundTransferTransactionRepository fundTransferTransactionRepository) : IGetService<FundTransferInQuery, FundTransferResult>
{
	public async ValueTask<FundTransferResult> GetAsync(FundTransferInQuery query, CancellationToken cancellationToken = default)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.Amount);
		ArgumentOutOfRangeException.ThrowIfLessThan(query.Amount, 0.01m);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Amount, 10000000.00m);

		var exchangeRateFundResult = await gameCurrencyClient.ConvertGameProviderFundsAsync(
			gameProviderCode: query.GameProviderCode,
			currencyCode: query.CurrencyCode,
			amount: query.Amount,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var gameProviderAmount = exchangeRateFundResult.Amount;

		await fundsDistributedLock.LockingAsync(
			gameProviderCode: query.GameProviderCode,
			playerCode: query.PlayerCode)
			.ConfigureAwait(false);

		try
		{
			var playerBalance = await gameProvidersClient.PlayerBalanceAsync(
				gameProviderCode: query.GameProviderCode,
				playerCode: query.PlayerCode,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);

			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playerBalance.FreeBalance);
			ArgumentOutOfRangeException.ThrowIfLessThan(playerBalance.FreeBalance - gameProviderAmount, 0.0m);

			var fundProcessingResult = await service.GetAsync(
				query: new FundProcessingFundInAddQuery(
					GameProviderCode: query.GameProviderCode,
					PlayerCode: query.PlayerCode,
					Amount: query.Amount),
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			GameProviderFundInResult gameProviderFundInResult;
			try
			{
				gameProviderFundInResult = await gameProviderFundInService.GetAsync(
					query: new GameProviderFundInQuery(
						GameProviderCode: fundProcessingResult.GameProviderCode,
						BrandCode: fundProcessingResult.BrandCode,
						PlayerCode: fundProcessingResult.PlayerCode,
						CurrencyCode: query.CurrencyCode,
						GameTransactionId: fundProcessingResult.GameTransactionId,
						Amount: gameProviderAmount),
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);
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

			try
			{
				var transactionResponse = await cashServiceClient.GameProviderFundInAsync(
					walletId: query.WalletId,
					amount: query.Amount,
					description: $"【{query.GameProviderCode}】->【Default】",
					externalId: fundProcessingResult.CashTransactionId,
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
					GameRespTransactionId: gameProviderFundInResult.GameRespTransactionId);
			}
			catch (Exception ex)
			{
				await fundTransferTransactionRepository.UpdateByMainWalletFundFailedAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.FundTransactionId,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				logger.LogError(exception: ex, "The main wallet transfer failed.({gameProviderFundTransferTransactionId})", fundProcessingResult.FundTransactionId);

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
