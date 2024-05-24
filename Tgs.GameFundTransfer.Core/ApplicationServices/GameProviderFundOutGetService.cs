using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class GameProviderFundOutGetService(
	ILogger<GameProviderFundOutGetService> logger,
	IGameProvidersClient gameProvidersClient) : IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>
{
	public async ValueTask<GameProviderFundOutResult> GetAsync(GameProviderFundOutQuery query, CancellationToken cancellationToken = default)
	{
		var response = await gameProvidersClient.PlayerFundOutAsync(
			gameProviderCode: query.GameProviderCode,
			brandCode: query.BrandCode,
			playerCode: query.PlayerCode,
			currencyCode: query.CurrencyCode,
			orderId: query.GameProviderTransactionId,
			amount: query.Amount,
			cancellationToken: cancellationToken)
		.ConfigureAwait(false);

		ArgumentException.ThrowIfNullOrWhiteSpace(response.OrderId);
		ArgumentException.ThrowIfNullOrWhiteSpace(response.GameProviderTransactionId);

		logger.LogInformation("{info}",
		new JsonObject
		{
			["LogOn"] = DateTime.Now.ToString("o"),
			["GameProviderCode"] = query.GameProviderCode,
			["PlayerCode"] = query.PlayerCode,
			["OrderId"] = response.OrderId.ToString(),
			["GameProviderTransactionId"] = response.GameProviderTransactionId,
			["ExternalGameProviderTransactionId"] = response.ExternalGameProviderTransactionId,
			["Mesaage"] = "This game provider funds transfer was successful.",
		});

		return new GameProviderFundOutResult(
			response.OrderId,
			response.GameProviderTransactionId);
	}
}
