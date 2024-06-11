using System.Text.Json;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Messages;

public class GameFundTransferBalanceCacheMessageHandler(
	ILogger<GameFundTransferBalanceCacheMessageHandler> logger,
	IFundBalanceCacheDispatcher dispatcher) : IMessageHandler<Common.Protos.GameFundTransferCacheRequest>
{
	public ValueTask HandleAsync(string subject, Common.Protos.GameFundTransferCacheRequest data, IEnumerable<MessageHeaderValue>? headerValues, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("LogOn:{logOn} Subject:{subject} Mesaage:This message has been received.({data})",
			DateTime.Now.ToString("o"),
			subject,
			data);

		logger.LogInformation("{logInformation}",
			JsonSerializer.Serialize(new
			{
				LogOn = DateTime.Now.ToString("u"),
				Subject = subject,
				Data = new
				{
					data.Action,
					data.GameProviderCode,
					data.PlayerCode,
					Balance = data.Balance.ToDecimal().GetValueOrDefault(0.0m),
					FreeBalance = data.FreeBalance.ToDecimal().GetValueOrDefault(0.0m),
					OutstandingBalance = data.OutstandingBalance.ToDecimal().GetValueOrDefault(0.0m)
				},
				Message = "This receive message handler."
			}));

		return dispatcher.PushCommandAsync(
			command: new FundBalanceCacheCommand(
				Action: data.Action,
				GameProviderCode: data.GameProviderCode,
				PlayerCode: data.PlayerCode,
				Balance: data.Balance.ToDecimal().GetValueOrDefault(0.0m),
				FreeBalance: data.FreeBalance.ToDecimal().GetValueOrDefault(0.0m),
				OutstandingBalance: data.OutstandingBalance.ToDecimal().GetValueOrDefault(0.0m)),
			cancellationToken: cancellationToken);
	}
}
