using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class GameProviderFundInGetService(
	ILogger<GameProviderFundInGetService> logger,
	IGameProvidersClient gameProvidersClient) : IGetService<GameProviderFundInQuery, GameProviderFundInResult>
{
	public async ValueTask<GameProviderFundInResult> GetAsync(GameProviderFundInQuery query, CancellationToken cancellationToken = default)
	{
		var response = await gameProvidersClient.PlayerFundInAsync(
			gameProviderCode: query.GameProviderCode,
			playerCode: query.PlayerCode,
			orderId: query.GameTransactionId,
			amount: query.Amount,
			cancellationToken: cancellationToken)
		.ConfigureAwait(false);

		ArgumentException.ThrowIfNullOrWhiteSpace(response.OrderId);

		logger.LogInformation("{info}",
		new JsonObject
		{
			["LogOn"] = DateTime.Now.ToString("o"),
			["GameProviderCode"] = query.GameProviderCode,
			["PlayerCode"] = query.PlayerCode,
			["OrderId"] = response.OrderId.ToString(),
			["ExternalOrderId"] = response.ExternalOrderId,
			["Mesaage"] = "This game provider funds transfer was successful.",
		}.ToJsonString());

		return new GameProviderFundInResult(
			response.OrderId,
			response.ExternalOrderId);
	}
}
