namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record GameProviderFundInQuery(
	string GameProviderCode,
	string PlayerCode,
	Guid WalletId,
	string GameTransactionId,
	decimal Amount);
