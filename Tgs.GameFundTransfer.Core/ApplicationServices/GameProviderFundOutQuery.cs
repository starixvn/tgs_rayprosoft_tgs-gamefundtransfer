namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record GameProviderFundOutQuery(
	string GameProviderCode,
	string PlayerCode,
	Guid WalletId,
	string GameTransactionId,
	decimal Amount);
