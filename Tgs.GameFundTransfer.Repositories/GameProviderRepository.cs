using Microsoft.EntityFrameworkCore;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Entities;

namespace Tgs.GameFundTransfer.Repositories;

internal class GameProviderRepository(
	TgsContext context) : IGameProviderRepository
{
	public async ValueTask<Core.Models.GameProviderInfo> GetAsync(string gameProviderCode, CancellationToken cancellationToken = default)
	{
		gameProviderCode = gameProviderCode.ToUpper();
		return await context.GameProviders
			.Where(gp => gp.Code == gameProviderCode)
			.Select(gp => new Core.Models.GameProviderInfo(
				gp.Id,
				gp.Code,
				gp.TransferWallet))
			.FirstOrDefaultAsync(
			cancellationToken: cancellationToken)
			.ConfigureAwait(false)
			?? throw new ArgumentNullException(
			gameProviderCode,
			"This game provider identification does not exist.");
	}

	public IAsyncEnumerable<string> QueryAsync(CancellationToken cancellationToken = default)
		=> context.GameProviders
		.Where(gp => gp.Status != 1 && gp.TransferWallet)
		.Select(gp => gp.Code)
		.ToAsyncEnumerable();
}
