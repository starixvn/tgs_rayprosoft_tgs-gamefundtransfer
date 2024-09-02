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
					data.GameProviderCode,
					data.PlayerCode
				},
				Message = "This receive message handler."
			}));

		return service.ExecuteAsync(
			command: new FundTransferTrackerCommand(
				GameProviderCode: data.GameProviderCode,
				PlayerCode: data.PlayerCode),
			cancellationToken: cancellationToken);
	}
}
