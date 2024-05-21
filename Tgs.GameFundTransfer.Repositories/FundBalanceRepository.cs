using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Core.Models;
using Tgs.GameProviders.Client;

namespace Tgs.GameFundTransfer.Repositories;

internal class FundBalanceRepository(
	IGameProvidersClient client) : IFundBalanceRepository
{
	public ValueTask<bool> ExistsAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public async ValueTask<FundBalanceInfo> GetAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default)
	{
		var response = await client.PlayerBalanceAsync(
			gameProviderCode: gameProviderCode,
			playerCode: playerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return response.Balance == 0 ?
			new FundBalanceInfo(
				Balance: 0.0m,
				FreeBalance: 0.0m,
				OutstandingBalance: 0.0m)
			: new FundBalanceInfo(
				Balance: response.Balance,
				FreeBalance: response.FreeBalance,
				OutstandingBalance: response.OutstandingBalance);
	}

	public ValueTask<bool> ReleaseAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public ValueTask SettingsAsync(string gameProviderCode, string playerCode, FundBalanceInfo source, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
