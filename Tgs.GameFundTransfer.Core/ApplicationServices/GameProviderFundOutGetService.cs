using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class GameProviderFundOutGetService(
	IGameProvidersClient gameProvidersClient) : IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>
{
	public async ValueTask<GameProviderFundOutResult> GetAsync(GameProviderFundOutQuery query, CancellationToken cancellationToken = default)
	{
		var gameProviderTransactionId = await gameProvidersClient.PlayerFundOutAsync(
			gameProviderCode: query.GameProviderCode,
			brandCode: query.BrandCode,
			playerCode: query.PlayerCode,
			currencyCode: query.CurrencyCode,
			orderId: query.GameProviderTransactionId,
			amount: query.Amount,
			cancellationToken: cancellationToken)
		.ConfigureAwait(false);

		ArgumentException.ThrowIfNullOrWhiteSpace(gameProviderTransactionId);

		return new GameProviderFundOutResult(gameProviderTransactionId);
	}
}
