using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class GameProviderFundOutGetService(
	ILogger<GameProviderFundOutGetService> logger,
	IGameProvidersClient gameProvidersClient,
	IMessageSender messageSender) : IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>
{
	public async ValueTask<GameProviderFundOutResult> GetAsync(GameProviderFundOutQuery query, CancellationToken cancellationToken = default)
	{
		var response = await gameProvidersClient.PlayerFundOutAsync(
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

		return new GameProviderFundOutResult(
			response.OrderId,
			response.ExternalOrderId);
	}
}
