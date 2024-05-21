using System.Text.Json.Nodes;

namespace Tgs.GameFundTransfer.Core;

public interface IGameProviderConfigurationRepository
{
	ValueTask<JsonNode> GetConfigurationDataAsync(Guid gameProviderConfigurationId, CancellationToken cancellationToken = default);
}
