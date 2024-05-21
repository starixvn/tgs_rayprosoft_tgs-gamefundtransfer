namespace Tgs.GameFundTransfer.Core.Models;
public record GameProviderInfo(
	Guid GameProviderId,
	string GameProviderCode,
	bool TransferWallet);
