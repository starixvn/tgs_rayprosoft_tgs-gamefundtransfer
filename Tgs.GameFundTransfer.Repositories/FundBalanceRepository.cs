using Microsoft.Extensions.Logging;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Core.Models;
using Tgs.GameProviders.Client;

namespace Tgs.GameFundTransfer.Repositories;

internal class FundBalanceRepository(
	ILogger<FundBalanceRepository> logger,
	IGameProvidersClient client) : IFundBalanceRepository
{
	public ValueTask<bool> ExistsAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public async ValueTask<FundBalanceInfo> GetAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default)
	{
		try
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
		catch (Exception ex)
		{
			logger.LogError(ex, "LogOn:{logOn} Mesaage:This player's({playerCode}) balance query from the game provider({gameProviderCode}) failed.",
				DateTime.Now.ToString("o"),
				playerCode,
				gameProviderCode);
			throw;
		}
	}

	public ValueTask<bool> ReleaseAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public ValueTask SettingsAsync(string gameProviderCode, string playerCode, FundBalanceInfo source, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
