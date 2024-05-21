namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundInZeroQuery(
	string GameProviderCode,
	string PlayerCode,
	Guid WalletId,
	string CurrencyCode);
