using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Core.Models;
using Tgs.GameFundTransfer.Entities;

namespace Tgs.GameFundTransfer.Repositories;

internal class PlayerRepository(
	TgsContext context) : IPlayerRepository
{
	public async ValueTask<PlayerInfo> GetAsync(string playerCode, CancellationToken cancellationToken = default)
		=> await context.Players
		.Where(p => p.PlayerCode == playerCode)
		.Join(context.Brands,
			player => player.BrandId,
			brand => brand.Id,
			(player, brand) => new PlayerInfo(
				brand.Id,
				brand.Code,
				player.PlayerCode,
				player.Username,
				player.DefaultWalletId,
				player.DefaultWalletCurrencyCode))
		.FirstOrDefaultAsync(
			cancellationToken: cancellationToken)
		.ConfigureAwait(false)
		?? throw new ArgumentNullException(
			playerCode,
			"This player does not exist.");
}
