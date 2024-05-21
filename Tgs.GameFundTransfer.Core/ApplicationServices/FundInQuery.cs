namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundInQuery(
	string GameProviderCode,
	string PlayerCode,
	Guid WalletId,
	string CurrencyCode,
	decimal Amount);
