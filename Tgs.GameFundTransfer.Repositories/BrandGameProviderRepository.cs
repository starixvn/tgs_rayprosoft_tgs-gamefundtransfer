using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Entities;

namespace Tgs.GameFundTransfer.Repositories;

internal class BrandGameProviderRepository(
	TgsContext context) : IBrandGameProviderRepository
{
	public async ValueTask<Guid> GetGameProviderConfigurationIdAsync(Guid brandId, Guid gameProviderId, CancellationToken cancellationToken = default)
	=> await context.BrandGameProviders
		.Where(bgp => bgp.BrandId == brandId && bgp.GameProviderId == gameProviderId)
		.Select(bgp => (Guid?)bgp.GameProviderConfigurationId)
		.FirstOrDefaultAsync(
		cancellationToken: cancellationToken)
		.ConfigureAwait(false)
		?? throw new ArgumentNullException(
			paramName: new JsonObject
			{
				["BrandId"] = brandId.ToString(),
				["GameProviderId"] = gameProviderId.ToString(),
			}.ToJsonString(),
			message: "This branded game provider configuration does not exist.");
}
