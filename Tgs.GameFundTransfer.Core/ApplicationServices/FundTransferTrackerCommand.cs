namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundTransferTrackerCommand(
	string GameProviderCode,
	string PlayerCode,
	Guid WalletId);
