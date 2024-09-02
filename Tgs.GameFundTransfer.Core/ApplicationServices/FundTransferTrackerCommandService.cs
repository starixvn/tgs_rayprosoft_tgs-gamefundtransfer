using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundTransferTrackerCommandService(
	ILogger<FundTransferTrackerCommandService> logger) : ICommandService<FundTransferTrackerCommand>
{
	public ValueTask ExecuteAsync(FundTransferTrackerCommand command, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("{logInformation}",
			JsonSerializer.Serialize(new
			{
				LogOn = DateTime.Now.ToString("u"),
				Data = new
				{
					command.GameProviderCode,
					command.PlayerCode
				},
				Message = "This receive message handler."
			}));

		return ValueTask.CompletedTask;
	}
}
