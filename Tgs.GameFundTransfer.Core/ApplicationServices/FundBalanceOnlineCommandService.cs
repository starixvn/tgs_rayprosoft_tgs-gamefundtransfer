using Microsoft.Extensions.Logging;
using Tgs.GamePlayerOnline.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundBalanceOnlineCommandService(
	ILogger<FundBalanceOnlineCommandService> logger,
	IGamePlayerOnlineClient gamePlayerOnlineClient) : ICommandService<FundBalanceOnlineCommand>
{
	public async ValueTask ExecuteAsync(FundBalanceOnlineCommand command, CancellationToken cancellationToken = default)
	{
		try
		{
			await gamePlayerOnlineClient.PlayerOnlineUpsertAsync(
				gameProviderCode: command.GameProviderCode,
				playerCode: command.PlayerCode,
				balance: command.Balance,
				freeBalance: command.FreeBalance,
				outstandingBalance: command.OutstandingBalance,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "{info}",
				new
				{
					LogOn = DateTime.Now.ToString("o"),
					command.GameProviderCode,
					command.PlayerCode,
					command.Balance,
					command.FreeBalance,
					command.OutstandingBalance,
				});
		}
	}
}
