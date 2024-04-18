namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundInZeroQuery(
	Guid GameProviderId,
	string GameProviderCode,
	string BrandCode,
	string PlayerCode,
	Guid WalletId,
	string CurrencyCode);
