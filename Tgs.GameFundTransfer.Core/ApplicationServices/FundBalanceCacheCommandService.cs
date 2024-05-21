using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundBalanceCacheCommandService(
	IFundBalanceRepository repository) : ICommandService<FundBalanceCacheCommand>
{
	public async ValueTask ExecuteAsync(FundBalanceCacheCommand command, CancellationToken cancellationToken = default)
	{
		if (command.Action == Common.Protos.Action.Cache)
		{
			if (!await repository.ExistsAsync(
				gameProviderCode: command.GameProviderCode,
				playerCode: command.PlayerCode,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false))
			{
				await repository.SettingsAsync(
					gameProviderCode: command.GameProviderCode,
					playerCode: command.PlayerCode,
					source: new Models.FundBalanceInfo(
						Balance: command.Balance,
						FreeBalance: command.FreeBalance,
						OutstandingBalance: command.OutstandingBalance),
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);
			}
		}
		else
		{
			_ = await repository.ReleaseAsync(
				gameProviderCode: command.GameProviderCode,
				playerCode: command.PlayerCode,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
