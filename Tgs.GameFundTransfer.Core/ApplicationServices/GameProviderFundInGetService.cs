using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class GameProviderFundInGetService(
	ILogger<GameProviderFundInGetService> logger,
	IGameProvidersClient gameProvidersClient,
	IMessageSender messageSender) : IGetService<GameProviderFundInQuery, GameProviderFundInResult>
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

		await messageSender.PublishAsync(
			subject: Common.GameFundTransferNatsConst.GameFundTransferTrackerRequest,
			data: new Common.Protos.GameFundTransferTrackerRequest
			{
				GameProviderCode = query.GameProviderCode,
				PlayerCode = query.PlayerCode
			},
			cancellationToken: cancellationToken).ConfigureAwait(false);

		return new GameProviderFundInResult(
			response.OrderId,
			response.ExternalOrderId);
	}
}
