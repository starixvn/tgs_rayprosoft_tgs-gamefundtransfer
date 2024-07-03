using System.Text.Json;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Messages;

public class GameFundTransferBalanceOnlineMessageHandler(
	ILogger<GameFundTransferBalanceOnlineMessageHandler> logger,
	ICommandService<FundBalanceOnlineCommand> service) : IMessageHandler<Common.Protos.GameFundTransferBalanceOnlineRequest>
{
	public ValueTask HandleAsync(string subject, Common.Protos.GameFundTransferBalanceOnlineRequest data, IEnumerable<MessageHeaderValue>? headerValues, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("{logInformation}",
			JsonSerializer.Serialize(new
			{
				LogOn = DateTime.Now.ToString("u"),
				Subject = subject,
				Data = new
				{
					data.GameProviderCode,
					data.PlayerCode,
					Balance = data.Balance.ToDecimal().GetValueOrDefault(0.0m),
					FreeBalance = data.FreeBalance.ToDecimal().GetValueOrDefault(0.0m),
					OutstandingBalance = data.OutstandingBalance.ToDecimal().GetValueOrDefault(0.0m)
				},
				Message = "This receive message handler."
			}));

		return service.ExecuteAsync(
			command: new FundBalanceOnlineCommand(
				GameProviderCode: data.GameProviderCode,
				PlayerCode: data.PlayerCode,
				Balance: data.Balance.ToDecimal().GetValueOrDefault(0.0m),
				FreeBalance: data.FreeBalance.ToDecimal().GetValueOrDefault(0.0m),
				OutstandingBalance: data.OutstandingBalance.ToDecimal().GetValueOrDefault(0.0m)),
			cancellationToken: cancellationToken);
	}
}
