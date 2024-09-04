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
		try
		{
			var response = await gameProvidersClient.PlayerFundOutAsync(
				gameProviderCode: query.GameProviderCode,
				playerCode: query.PlayerCode,
				orderId: query.GameTransactionId,
				amount: query.Amount,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			ArgumentException.ThrowIfNullOrWhiteSpace(response.OrderId);

			logger.LogInformation("{logInformation}",
				new JsonObject
				{
					["LogOn"] = DateTime.Now.ToString("u"),
					["GameProviderId"] = query.GameProviderId,
					["GameProviderCode"] = query.GameProviderCode,
					["PlayerCode"] = query.PlayerCode,
					["WalletId"] = query.WalletId,
					["GameTransactionId"] = query.GameTransactionId,
					["OrderId"] = response.OrderId.ToString(),
					["ExternalOrderId"] = response.ExternalOrderId,
					["Amount"] = query.Amount,
					["Mesaage"] = "This game provider funds transfer was successful."
				}.ToJsonString());

			await messageSender.PublishAsync(
				subject: Common.GameFundTransferNatsConst.GameFundTransferTrackerRequest,
				data: new Common.Protos.GameFundTransferTrackerRequest
				{
					GameProviderId = query.GameProviderId,
					GameProviderCode = query.GameProviderCode,
					WalletId = query.WalletId,
					PlayerCode = query.PlayerCode
				},
				cancellationToken: cancellationToken).ConfigureAwait(false);

			return new GameProviderFundOutResult(
				response.OrderId,
				response.ExternalOrderId);
		}
		catch (ArgumentException ex)
		{
			logger.LogError(ex, "{logError}",
				new JsonObject
				{
					["LogOn"] = DateTime.Now.ToString("u"),
					["GameProviderId"] = query.GameProviderId,
					["GameProviderCode"] = query.GameProviderCode,
					["PlayerCode"] = query.PlayerCode,
					["WalletId"] = query.WalletId,
					["GameTransactionId"] = query.GameTransactionId,
					["Mesaage"] = "The transaction number provided by this game provider is empty."
				}.ToJsonString());

			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "{logError}",
				new JsonObject
				{
					["LogOn"] = DateTime.Now.ToString("u"),
					["GameProviderId"] = query.GameProviderId,
					["GameProviderCode"] = query.GameProviderCode,
					["PlayerCode"] = query.PlayerCode,
					["WalletId"] = query.WalletId,
					["GameTransactionId"] = query.GameTransactionId,
					["Mesaage"] = "This game provider call failed."
				}.ToJsonString());

			throw;
		}
	}
}
