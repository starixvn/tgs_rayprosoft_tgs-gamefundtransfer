using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Entities;

namespace Tgs.GameFundTransfer.Repositories;

internal class GameProviderConfigurationRepository(
	TgsContext context) : IGameProviderConfigurationRepository
{
	public async ValueTask<JsonNode> GetConfigurationDataAsync(Guid gameProviderConfigurationId, CancellationToken cancellationToken = default)
	{
		var configurationData = await context.GameProviderConfigs
			.Where(gpc => gpc.Id == gameProviderConfigurationId)
			.Select(gpc => gpc.ConfigurationData)
			.FirstOrDefaultAsync(cancellationToken)
			.ConfigureAwait(false)
			?? throw new ArgumentNullException(gameProviderConfigurationId.ToString(), $"This game provider configuration does not exist.");

		return JsonNode.Parse(configurationData)
			?? throw new ArgumentNullException(gameProviderConfigurationId.ToString(), $"This game provider profile is empty.");
	}
}
