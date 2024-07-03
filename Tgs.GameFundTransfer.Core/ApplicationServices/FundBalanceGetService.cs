using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Tgs.GameCurrency.Client;
using Tgs.GamePlayerOnline.Client;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundBalanceGetService(
	ILogger<FundBalanceGetService> logger,
	IPlayerRepository playerRepository,
	IFundBalanceRepository fundBalanceRepository,
	IMessageSender messageSender,
	IGameCurrencyClient gameCurrencyClient,
	IGamePlayerOnlineClient gamePlayerOnlineClient) : IGetService<FundBalanceQuery, FundBalanceResult>
{
	public async ValueTask<FundBalanceResult> GetAsync(FundBalanceQuery query, CancellationToken cancellationToken = default)
	{
		var playerInfo = await playerRepository.GetAsync(
			playerCode: query.PlayerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var fundBalanceInfo = await fundBalanceRepository.GetAsync(
			gameProviderCode: query.GameProviderCode,
			playerCode: playerInfo.PlayerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		try
		{
			await messageSender.PublishAsync(
				subject: Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest,
				data: new Common.Protos.GameFundTransferCacheRequest
				{
					Action = Common.Protos.Action.Cache,
					GameProviderCode = query.GameProviderCode,
					PlayerCode = playerInfo.PlayerCode,
					Balance = fundBalanceInfo.Balance,
					FreeBalance = fundBalanceInfo.FreeBalance,
					OutstandingBalance = fundBalanceInfo.OutstandingBalance
				},
				cancellationToken: cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "{info}",
				new
				{
					LogOn = DateTime.Now.ToString("o"),
					playerInfo.BrandId,
					playerInfo.BrandCode,
					playerInfo.PlayerCode,
					playerInfo.Username,
					query.GameProviderCode,
					fundBalanceInfo.Balance,
					fundBalanceInfo.FreeBalance,
					fundBalanceInfo.OutstandingBalance
				});
			throw;
		}

		ConvertPlatformBalanceResult balanceResult = new ConvertPlatformBalanceResult(
			ExchangeRate: 1,
			Balance: 0.0m,
			FreeBalance: 0.0m,
			OutstandingBalance: 0.0m);

		if (fundBalanceInfo.Balance != 0)
		{
			balanceResult = await gameCurrencyClient.ConvertPlatformBalanceAsync(
				gameProviderCode: query.GameProviderCode,
				currencyCode: playerInfo.CurrencyCode,
				balance: fundBalanceInfo.Balance,
				freeBalance: fundBalanceInfo.FreeBalance,
				outstandingBalance: fundBalanceInfo.OutstandingBalance,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		}

		try
		{
			await gamePlayerOnlineClient.PlayerOnlineUpsertAsync(
				gameProviderCode: query.GameProviderCode,
				playerCode: query.PlayerCode,
				balance: balanceResult.Balance,
				freeBalance: balanceResult.FreeBalance,
				outstandingBalance: balanceResult.OutstandingBalance,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "{info}",
				new
				{
					LogOn = DateTime.Now.ToString("o"),
					playerInfo.BrandId,
					playerInfo.BrandCode,
					playerInfo.PlayerCode,
					playerInfo.Username,
					query.GameProviderCode,
					balanceResult.Balance,
					balanceResult.FreeBalance,
					balanceResult.OutstandingBalance
				});
			throw;
		}

		return new FundBalanceResult(
			Balance: balanceResult.Balance,
			FreeBalance: balanceResult.FreeBalance,
			OutstandingBalance: balanceResult.OutstandingBalance);
	}
}
