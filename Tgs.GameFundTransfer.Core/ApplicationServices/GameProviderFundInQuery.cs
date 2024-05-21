namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record GameProviderFundInQuery(
	string GameProviderCode,
	string BrandCode,
	string PlayerCode,
	string CurrencyCode,
	string GameProviderTransactionId,
	decimal Amount);
