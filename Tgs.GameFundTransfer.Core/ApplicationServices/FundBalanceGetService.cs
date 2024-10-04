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

		await messageSender.PublishAsync(
			subject: Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest,
			data: new Common.Protos.GameFundTransferBalanceCacheRequest
			{
				Action = Common.Protos.Action.Cache,
				GameProviderCode = query.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = fundBalanceInfo.Balance,
				FreeBalance = fundBalanceInfo.FreeBalance,
				OutstandingBalance = fundBalanceInfo.OutstandingBalance
			},
			cancellationToken: cancellationToken).ConfigureAwait(false);

		var balanceResult = new GameCurrency.Common.ConvertPlatformBalanceResult(
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

		await messageSender.PublishAsync(
			subject: Common.GameFundTransferNatsConst.GameFundTransferBalanceOnlineRequest,
			data: new Common.Protos.GameFundTransferBalanceOnlineRequest
			{
				GameProviderCode = query.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = balanceResult.Balance,
				FreeBalance = balanceResult.FreeBalance,
				OutstandingBalance = balanceResult.OutstandingBalance
			},
			cancellationToken: cancellationToken).ConfigureAwait(false);

		return new FundBalanceResult(
			Balance: balanceResult.Balance,
			FreeBalance: balanceResult.FreeBalance,
			OutstandingBalance: balanceResult.OutstandingBalance);
	}
}
