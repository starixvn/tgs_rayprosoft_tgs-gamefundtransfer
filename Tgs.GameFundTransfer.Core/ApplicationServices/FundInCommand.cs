namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundInCommand(
	string GameProviderCode,
	string PlayerCode,
	Guid WalletId,
	string CurrencyCode,
	decimal Amount);
