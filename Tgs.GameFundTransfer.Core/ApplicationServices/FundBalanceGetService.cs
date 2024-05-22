using Microsoft.Extensions.Logging;
using Tgs.GameCurrency.Client;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundBalanceGetService(
	IPlayerRepository playerRepository,
	IFundBalanceRepository fundBalanceRepository,
	IMessageSender messageSender,
	IGameCurrencyClient gameCurrencyClient) : IGetService<FundBalanceQuery, FundBalanceResult>
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

		if (fundBalanceInfo.Balance == 0)
		{
			return new FundBalanceResult(
				Balance: 0.0m,
				FreeBalance: 0.0m,
				OutstandingBalance: 0.0m);
		}

		var gameFundTransferCacheRequest = new Common.Protos.GameFundTransferCacheRequest
		{
			Action = Common.Protos.Action.Cache,
			GameProviderCode = query.GameProviderCode,
			PlayerCode = playerInfo.PlayerCode,
			Balance = fundBalanceInfo.Balance,
			FreeBalance = fundBalanceInfo.FreeBalance,
			OutstandingBalance = fundBalanceInfo.OutstandingBalance
		};
		await messageSender.PublishAsync(
			subject: Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest,
			data: gameFundTransferCacheRequest,
			cancellationToken: cancellationToken).ConfigureAwait(false);

		var result = await gameCurrencyClient.ConvertPlatformBalanceAsync(
			query.GameProviderCode,
			playerInfo.CurrencyCode,
			fundBalanceInfo.Balance,
			fundBalanceInfo.FreeBalance,
			fundBalanceInfo.OutstandingBalance,
			cancellationToken)
			.ConfigureAwait(false);

		return new FundBalanceResult(
			Balance: result.Balance,
			FreeBalance: result.FreeBalance,
			OutstandingBalance: result.OutstandingBalance);
	}
}
