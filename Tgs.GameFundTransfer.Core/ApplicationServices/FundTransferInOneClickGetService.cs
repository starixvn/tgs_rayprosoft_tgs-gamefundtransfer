using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFund.Client;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundTransferInOneClickGetService(
	ILogger<FundTransferInGetService> logger,
	IPlayerRepository playerRepository,
	IGameCurrencyClient gameCurrencyClient,
	IFundsDistributedLock fundsDistributedLock,
	IGameProvidersClient gameProvidersClient,
	ICashServiceClient cashServiceClient,
	IGetService<FundProcessingFundInAddQuery, FundProcessingResult> service,
	IGetService<GameProviderFundInQuery, GameProviderFundInResult> gameProviderFundInService,
	IGameFundClient client) : IGetService<FundTransferInOneClickQuery, FundTransferResult>
{
	public async ValueTask<FundTransferResult> GetAsync(FundTransferInOneClickQuery query, CancellationToken cancellationToken = default)
	{
		var playerInfo = await playerRepository.GetAsync(
			playerCode: query.PlayerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

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

			var convertPlatformBalanceResult = await gameCurrencyClient.ConvertPlatformBalanceAsync(
				gameProviderCode: query.GameProviderCode,
				currencyCode: playerInfo.CurrencyCode,
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

			GameProviderFundInResult gameProviderFundInResult;
			try
			{
				gameProviderFundInResult = await gameProviderFundInService.GetAsync(
					query: new GameProviderFundInQuery(
						GameProviderId: fundProcessingResult.GameProviderId,
						GameProviderCode: fundProcessingResult.GameProviderCode,
						PlayerCode: fundProcessingResult.PlayerCode,
						WalletId: playerInfo.WalletId,
						GameTransactionId: fundProcessingResult.GameTransactionId,
						Amount: playerBalance.FreeBalance),
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				await client.GameFundTransferUpdateByGameWalletFailedAsync(
					id: fundProcessingResult.FundTransactionId,
					description: string.Empty,
					additionalData: new JsonObject
					{
						["LogOn"] = DateTime.Now.ToString("o"),
						["BrandId"] = fundProcessingResult.BrandId,
						["BrandCode"] = fundProcessingResult.BrandCode,
						["PlayerCode"] = fundProcessingResult.PlayerCode,
						["Username"] = fundProcessingResult.Username,
						["GameProviderId"] = fundProcessingResult.GameProviderId,
						["GameProviderCode"] = fundProcessingResult.GameProviderCode,
						["FundTransactionId"] = fundProcessingResult.FundTransactionId,
						["CashTransactionId"] = fundProcessingResult.CashTransactionId,
						["GameTransactionId"] = fundProcessingResult.GameTransactionId
					},
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				logger.LogError(ex, "{info}",
					new JsonObject
					{
						["LogOn"] = DateTime.Now.ToString("o"),
						["BrandId"] = fundProcessingResult.BrandId,
						["BrandCode"] = fundProcessingResult.BrandCode,
						["PlayerCode"] = fundProcessingResult.PlayerCode,
						["Username"] = fundProcessingResult.Username,
						["GameProviderId"] = fundProcessingResult.GameProviderId,
						["GameProviderCode"] = fundProcessingResult.GameProviderCode,
						["FundTransactionId"] = fundProcessingResult.FundTransactionId,
						["CashTransactionId"] = fundProcessingResult.CashTransactionId,
						["GameTransactionId"] = fundProcessingResult.GameTransactionId,
						["Mesaage"] = "The game wallet transfer failed."
					}.ToJsonString());
				throw;
			}

			try
			{
				var transactionResponse = await cashServiceClient.GameProviderFundInAsync(
					walletId: playerInfo.WalletId,
					amount: convertPlatformBalanceResult.FreeBalance,
					description: $"【{query.GameProviderCode}】->【Default】",
					externalId: fundProcessingResult.CashTransactionId,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				await client.GameFundTransferUpdateBySuccessAsync(
					id: fundProcessingResult.FundTransactionId,
					cashRespTransactionId: transactionResponse.TransactionId,
					gameRespTransactionId: gameProviderFundInResult.GameRespTransactionId,
					mainWalletBalance: transactionResponse.Balance,
					description: string.Empty,
					additionalData: new JsonObject
					{
						["LogOn"] = DateTime.Now.ToString("o"),
						["BrandId"] = fundProcessingResult.BrandId,
						["BrandCode"] = fundProcessingResult.BrandCode,
						["PlayerCode"] = fundProcessingResult.PlayerCode,
						["Username"] = fundProcessingResult.Username,
						["GameProviderId"] = fundProcessingResult.GameProviderId,
						["GameProviderCode"] = fundProcessingResult.GameProviderCode,
						["FundTransactionId"] = fundProcessingResult.FundTransactionId,
						["CashTransactionId"] = fundProcessingResult.CashTransactionId,
						["GameTransactionId"] = fundProcessingResult.GameTransactionId,
						["CashRespTransactionId"] = transactionResponse.TransactionId,
						["GameRespTransactionId"] = gameProviderFundInResult.GameRespTransactionId
					},
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
				await client.GameFundTransferUpdateByMainWalletFailedAsync(
					id: fundProcessingResult.FundTransactionId,
					description: string.Empty,
					additionalData: new JsonObject
					{
						["LogOn"] = DateTime.Now.ToString("o"),
						["BrandId"] = fundProcessingResult.BrandId,
						["BrandCode"] = fundProcessingResult.BrandCode,
						["PlayerCode"] = fundProcessingResult.PlayerCode,
						["Username"] = fundProcessingResult.Username,
						["GameProviderId"] = fundProcessingResult.GameProviderId,
						["GameProviderCode"] = fundProcessingResult.GameProviderCode,
						["FundTransactionId"] = fundProcessingResult.FundTransactionId,
						["CashTransactionId"] = fundProcessingResult.CashTransactionId,
						["GameTransactionId"] = fundProcessingResult.GameTransactionId,
						["GameRespTransactionId"] = gameProviderFundInResult.GameRespTransactionId
					},
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				logger.LogError(ex, "{info}",
					new JsonObject
					{
						["LogOn"] = DateTime.Now.ToString("o"),
						["BrandId"] = fundProcessingResult.BrandId,
						["BrandCode"] = fundProcessingResult.BrandCode,
						["PlayerCode"] = fundProcessingResult.PlayerCode,
						["Username"] = fundProcessingResult.Username,
						["GameProviderId"] = fundProcessingResult.GameProviderId,
						["GameProviderCode"] = fundProcessingResult.GameProviderCode,
						["FundTransactionId"] = fundProcessingResult.FundTransactionId,
						["CashTransactionId"] = fundProcessingResult.CashTransactionId,
						["GameTransactionId"] = fundProcessingResult.GameTransactionId,
						["GameRespTransactionId"] = gameProviderFundInResult.GameRespTransactionId,
						["Mesaage"] = "The main wallet transfer failed."
					}.ToJsonString());
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
