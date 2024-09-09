using System.Text.Json;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Messages;

public class GameFundTransferTrackerMessageHandler(
	ILogger<GameFundTransferTrackerMessageHandler> logger,
	 ICommandService<FundTransferTrackerCommand> service) : IMessageHandler<Common.Protos.GameFundTransferTrackerRequest>
{
	public ValueTask HandleAsync(string subject, Common.Protos.GameFundTransferTrackerRequest data, IEnumerable<MessageHeaderValue>? headerValues, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("{logInformation}",
			JsonSerializer.Serialize(new
			{
				LogOn = DateTime.Now.ToString("u"),
				Subject = subject,
				Data = new
				{
					GameProviderId = data.GameProviderId.ToGuid(),
					data.GameProviderCode,
					WalletId = data.WalletId.ToGuid(),
					data.PlayerCode
				},
				Message = "This receive message handler."
			}));

		return service.ExecuteAsync(
			command: new FundTransferTrackerCommand(
				GameProviderId: data.GameProviderId.ToGuid(),
				GameProviderCode: data.GameProviderCode,
				WalletId: data.WalletId.ToGuid(),
				PlayerCode: data.PlayerCode),
			cancellationToken: cancellationToken);
	}
}
