using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFund.Client;
using Tgs.GameFundTransfer.Core.Exceptions;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundTransferOutGetService(
	ILogger<FundTransferOutGetService> logger,
	IPlayerRepository playerRepository,
	IGameCurrencyClient gameCurrencyClient,
	IFundsDistributedLock fundsDistributedLock,
	IGameProvidersClient gameProvidersClient,
	ICashServiceClient cashServiceClient,
	IGetService<FundProcessingFundOutAddQuery, FundProcessingResult> service,
	IGetService<GameProviderFundOutQuery, GameProviderFundOutResult> gameProviderFundOutService,
	IGameFundClient client) : IGetService<FundTransferOutQuery, FundTransferResult>
{
	public async ValueTask<FundTransferResult> GetAsync(FundTransferOutQuery query, CancellationToken cancellationToken = default)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.Amount);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Amount, 10000000.00m);

		var playerInfo = await playerRepository.GetAsync(
			playerCode: query.PlayerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var exchangeRateFundResult = await gameCurrencyClient.ConvertGameProviderFundsAsync(
			gameProviderCode: query.GameProviderCode,
			currencyCode: playerInfo.CurrencyCode,
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
			walletId: playerInfo.WalletId,
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
				   walletId: playerInfo.WalletId,
				   amount: query.Amount,
				   description: $"【Default】->【{query.GameProviderCode}】",
				   externalId: fundProcessingResult.CashTransactionId,
				   cancellationToken: cancellationToken)
				   .ConfigureAwait(false);
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
						["Mesaage"] = "The main wallet transfer failed."
					}.ToJsonString());
				throw;
			}

			if (transactionResponse.Balance < 0)
			{
				var description = $"This CashTransactionIdentifier({fundProcessingResult.CashTransactionId}) conducts the player({fundProcessingResult.PlayerCode}) main wallet transaction resulting in Balance({transactionResponse.Balance}) being negative and CashResponseTransactionIdentifier({transactionResponse.TransactionId}).";

				try
				{
					var fundRefundResponse = await cashServiceClient.GameProviderFundRefundAsync(
						walletId: playerInfo.WalletId,
						amount: query.Amount,
						description: transactionResponse.TransactionId.ToString(),
						externalId: fundProcessingResult.CashTransactionId,
						cancellationToken: cancellationToken)
						.ConfigureAwait(false);

					await client.GameFundTransferUpdateByMainWalletRefundSuccessAsync(
						id: fundProcessingResult.FundTransactionId,
						cashRespTransactionId: fundRefundResponse.TransactionId,
						mainWalletBalance: transactionResponse.Balance,
						description: description,
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
							["CashRespTransactionId"] = fundRefundResponse.TransactionId
						},
						cancellationToken: cancellationToken)
						.ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					logger.LogError(exception: ex, "This transaction id({transactionId}) main wallet fund refund failed.", transactionResponse.TransactionId);

					await client.GameFundTransferUpdateByMainWalletRefundFailedAsync(
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
							["Mesaage"] = "This main wallet fund refund failed."
						},
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
						GameProviderId: fundProcessingResult.GameProviderId,
						GameProviderCode: fundProcessingResult.GameProviderCode,
						PlayerCode: fundProcessingResult.PlayerCode,
						WalletId: playerInfo.WalletId,
						GameTransactionId: fundProcessingResult.GameTransactionId,
						Amount: gameProviderAmount),
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				await client.GameFundTransferUpdateBySuccessAsync(
					id: fundProcessingResult.FundTransactionId,
					cashRespTransactionId: transactionResponse.TransactionId,
					gameRespTransactionId: gameProviderFundOutResult.GameRespTransactionId,
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
						["GameRespTransactionId"] = gameProviderFundOutResult.GameRespTransactionId
					},
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				return new FundTransferResult(
					FundTransactionId: fundProcessingResult.FundTransactionId,
					CashTransactionId: fundProcessingResult.CashTransactionId,
					CashRespTransactionId: transactionResponse.TransactionId,
					GameTransactionId: fundProcessingResult.GameTransactionId,
					GameRespTransactionId: gameProviderFundOutResult.GameRespTransactionId);
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
						["GameTransactionId"] = fundProcessingResult.GameTransactionId,
						["CashRespTransactionId"] = transactionResponse.TransactionId
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
						["CashRespTransactionId"] = transactionResponse.TransactionId,
						["Mesaage"] = "The game wallet transfer failed."
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
