namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundTransferTrackerCommand(
	Guid GameProviderId,
	string GameProviderCode,
	Guid WalletId,
	string PlayerCode);
