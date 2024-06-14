namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record GameProviderFundOutQuery(
	string GameProviderCode,
	string PlayerCode,
	string GameTransactionId,
	decimal Amount);
