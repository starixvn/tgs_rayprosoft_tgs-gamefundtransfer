using Microsoft.Extensions.Logging;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundTransferTrackerCommandService(
	ILogger<FundTransferTrackerCommandService> logger) : ICommandService<FundTransferTrackerCommand>
{
	public ValueTask ExecuteAsync(FundTransferTrackerCommand command, CancellationToken cancellationToken = default)
		=> throw new NotImplementedException();
}
