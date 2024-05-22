using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundInCommandService(
	ILogger<FundInCommandService> logger,
	IGameCurrencyClient gameCurrencyClient,
	IFundsDistributedLock fundsDistributedLock,
	IGameProvidersClient gameProvidersClient,
	ICashServiceClient cashServiceClient,
	IGetService<FundProcessingFundInAddQuery, FundProcessingResult> service,
	IGetService<GameProviderFundInQuery, GameProviderFundInResult> gameProviderFundInService,
	IFundTransferTransactionRepository fundTransferTransactionRepository) : ICommandService<FundInCommand>
{
	public async ValueTask ExecuteAsync(FundInCommand query, CancellationToken cancellationToken = default)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.Amount);
		ArgumentOutOfRangeException.ThrowIfLessThan(query.Amount, 0.01m);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Amount, 9999999.99m);

		var exchangeRateFundResult = await gameCurrencyClient.ConvertGameProviderFundsAsync(
			query.GameProviderCode,
			query.CurrencyCode,
			query.Amount,
			cancellationToken)
			.ConfigureAwait(false);

		var gameProviderAmount = exchangeRateFundResult.Amount;

		if (!await fundsDistributedLock.LockingAsync(
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

			try
			{
				_ = await gameProviderFundInService.GetAsync(
					query: new GameProviderFundInQuery(
						GameProviderCode: fundProcessingResult.GameProviderCode,
						BrandCode: fundProcessingResult.BrandCode,
						PlayerCode: fundProcessingResult.PlayerCode,
						CurrencyCode: query.CurrencyCode,
						GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
						Amount: gameProviderAmount),
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);
			}
			catch (ArgumentException ex)
			{
				await fundTransferTransactionRepository.UpdateByGameProviderFundFailedAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				logger.LogError(ex, "LogOn:{logOn} Mesaage:The transaction number provided by this game provider ({gameProviderCode}) is empty.({gameProviderFundTransferTransactionId})",
					DateTime.Now.ToString("o"),
					fundProcessingResult.GameProviderCode,
					fundProcessingResult.GameProviderFundTransferTransactionId);

				throw;
			}
			catch (Exception ex)
			{
				await fundTransferTransactionRepository.UpdateByGameProviderFundFailedAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				logger.LogError(ex, "LogOn:{logOn} Mesaage:This game provider({gameProviderCode}) call failed.({gameProviderFundTransferTransactionId})",
					DateTime.Now.ToString("o"),
					fundProcessingResult.GameProviderCode,
					fundProcessingResult.GameProviderFundTransferTransactionId);

				throw;
			}

			try
			{
				var transactionResponse = await cashServiceClient.GameProviderFundInAsync(
					walletId: query.WalletId,
					amount: query.Amount,
					description: $"【{fundProcessingResult.GameProviderCode}】->【Default】",
					externalId: fundProcessingResult.GameProviderFundTransferTransactionId.ToString(),
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				await fundTransferTransactionRepository.UpdateBySuccessAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
					transactionId: transactionResponse.TransactionId,
					balance: transactionResponse.Balance,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				await fundTransferTransactionRepository.UpdateByMainWalletFundFailedAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);
				logger.LogError(exception: ex, "The main wallet transfer failed.({gameProviderFundTransferTransactionId})", fundProcessingResult.GameProviderFundTransferTransactionId);
			}
		}
		finally
		{
			await fundsDistributedLock.UnlockAsync(
				  gameProviderCode: query.GameProviderCode,
				  playerCode: query.PlayerCode).ConfigureAwait(false);
		}
	}
}
