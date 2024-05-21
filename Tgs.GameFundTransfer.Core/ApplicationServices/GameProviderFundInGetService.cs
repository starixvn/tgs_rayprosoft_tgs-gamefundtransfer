using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class GameProviderFundInGetService(
	IGameProvidersClient gameProvidersClient) : IGetService<GameProviderFundInQuery, GameProviderFundInResult>
{
	public async ValueTask<GameProviderFundInResult> GetAsync(GameProviderFundInQuery query, CancellationToken cancellationToken = default)
	{
		var gameProviderTransactionId = await gameProvidersClient.PlayerFundInAsync(
			gameProviderCode: query.GameProviderCode,
			brandCode: query.BrandCode,
			playerCode: query.PlayerCode,
			currencyCode: query.CurrencyCode,
			orderId: query.GameProviderTransactionId,
			amount: query.Amount,
			cancellationToken: cancellationToken)
		.ConfigureAwait(false);

		ArgumentException.ThrowIfNullOrWhiteSpace(gameProviderTransactionId);

		return new GameProviderFundInResult(gameProviderTransactionId);
	}
}
