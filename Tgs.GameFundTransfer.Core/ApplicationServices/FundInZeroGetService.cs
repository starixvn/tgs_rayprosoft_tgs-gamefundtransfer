using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.Extensions;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundInZeroGetService(
	ILogger<FundInGetService> logger,
	IGameCurrencyClient gameCurrencyClient,
	IFundsDistributedLock fundsDistributedLock,
	IGameProvidersClient gameProvidersClient,
	ICashServiceClient cashServiceClient,
	IGetService<FundProcessingFundInAddQuery, FundProcessingResult> service,
	IGetService<GameProviderFundInQuery, GameProviderFundInResult> gameProviderFundInService,
	IFundTransferTransactionRepository fundTransferTransactionRepository) : IGetService<FundInZeroQuery, FundResult>
{
	public async ValueTask<FundResult> GetAsync(FundInZeroQuery query, CancellationToken cancellationToken = default)
	{
		if (!await fundsDistributedLock.LockingAsync(
			gameProviderCode: query.GameProviderCode,
			playerCode: query.PlayerCode)
			.ConfigureAwait(false))

		{
			logger.LogWarning("LogOn:{logOn} Mesaage:Player ({playerCode}) funds transfer game provider ({gameProviderCode}) has been locked.",
				DateTime.Now.ToString("o"),
				query.PlayerCode,
				query.GameProviderCode);

			return new FundResult(
				state: StateCode.TransactionLock,
				message: $"This player ({query.PlayerCode}) transfer game provider({query.GameProviderCode}) is locked.");
		}

		try
		{
			var playerBalance = await gameProvidersClient.PlayerBalanceAsync(
				gameProviderCode: query.GameProviderCode,
				playerCode: query.PlayerCode,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playerBalance.FreeBalance);

			var convertPlatformBalanceResult = await gameCurrencyClient.ConvertPlatformBalanceAsync(
				gameProviderCode: query.GameProviderCode,
				currencyCode: query.CurrencyCode,
				balance: playerBalance.Balance,
				freeBalance: playerBalance.FreeBalance,
				outstandingBalance: playerBalance.OutstandingBalance,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			var fundProcessingResult = await service.GetAsync(
				query: new FundProcessingFundInAddQuery(
					GameProviderCode: query.GameProviderCode,
					PlayerCode: query.PlayerCode,
					Amount: convertPlatformBalanceResult.FreeBalance),
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			var gameProviderTransactionId = string.Empty;
			try
			{
				var gameProviderFundInResult = await gameProviderFundInService.GetAsync(
					query: new GameProviderFundInQuery(
						GameProviderCode: fundProcessingResult.GameProviderCode,
						BrandCode: fundProcessingResult.BrandCode,
						PlayerCode: fundProcessingResult.PlayerCode,
						CurrencyCode: query.CurrencyCode,
						GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
						Amount: playerBalance.FreeBalance),
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				gameProviderTransactionId = gameProviderFundInResult.GameProviderTransactionId;
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
					amount: convertPlatformBalanceResult.Balance,
					description: $"【{query.GameProviderCode}】->【Default】",
					externalId: fundProcessingResult.GameProviderFundTransferTransactionId.ToString(),
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				await fundTransferTransactionRepository.UpdateBySuccessAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
					transactionId: transactionResponse.TransactionId,
					gameProviderTransactionId: gameProviderTransactionId,
					balance: transactionResponse.Balance,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				return new FundResult(
					state: StateCode.Success,
					message: StateCode.Success.GetDescription(),
					fundTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
					transactionId: transactionResponse.TransactionId,
					externalTransactionId: gameProviderTransactionId);
			}
			catch (Exception ex)
			{
				await fundTransferTransactionRepository.UpdateByMainWalletFundFailedAsync(
					gameProviderFundTransferTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
					gameProviderTransactionId: gameProviderTransactionId,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				logger.LogError(exception: ex, "The main wallet transfer failed.({gameProviderFundTransferTransactionId})", fundProcessingResult.GameProviderFundTransferTransactionId);

				return new FundResult(
					state: StateCode.Exception,
					message: $"The main wallet transfer failed.({fundProcessingResult.GameProviderFundTransferTransactionId})",
					fundTransactionId: fundProcessingResult.GameProviderFundTransferTransactionId,
					transactionId: Guid.Empty,
					externalTransactionId: gameProviderTransactionId);
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
