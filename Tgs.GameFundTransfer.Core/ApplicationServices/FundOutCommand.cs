namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundOutCommand(
	string GameProviderCode,
	string PlayerCode,
	Guid WalletId,
	string CurrencyCode,
	decimal Amount);
