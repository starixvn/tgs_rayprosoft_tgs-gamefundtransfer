using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Messages;

public class GameFundTransferBalanceCacheMessageHandler(
	ILogger<GameFundTransferBalanceCacheMessageHandler> logger,
	ICommandService<FundBalanceCacheCommand> service) : IMessageHandler<Common.Protos.GameFundTransferCacheRequest>
{
	public ValueTask HandleAsync(string subject, Common.Protos.GameFundTransferCacheRequest data, IEnumerable<MessageHeaderValue>? headerValues, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("LogOn:{logOn} Subject:{subject} Mesaage:This message has been received.({data})",
			DateTime.Now.ToString("o"),
			subject,
			data);

		return service.ExecuteAsync(
			command: new FundBalanceCacheCommand(
				Action: data.Action,
				GameProviderCode: data.GameProviderCode,
				PlayerCode: data.PlayerCode,
				Balance: data.Balance,
				FreeBalance: data.FreeBalance,
				OutstandingBalance: data.OutstandingBalance),
			cancellationToken: cancellationToken);
	}
}
