using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Tgs.GameFundTransfer.Core.Models;
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

		return new GameProviderFundInResult(
			response.OrderId,
			response.GameProviderTransactionId);
	}
}
