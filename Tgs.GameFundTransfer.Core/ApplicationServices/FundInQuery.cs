namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundInQuery(
	Guid GameProviderId,
	string GameProviderCode,
	string BrandCode,
	string PlayerCode,
	Guid WalletId,
	string CurrencyCode,
	decimal Amount);
