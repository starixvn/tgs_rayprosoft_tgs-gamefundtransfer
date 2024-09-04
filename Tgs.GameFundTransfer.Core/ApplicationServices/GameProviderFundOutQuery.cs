namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record GameProviderFundOutQuery(
	Guid GameProviderId,
	string GameProviderCode,
	string PlayerCode,
	Guid WalletId,
	string GameTransactionId,
	decimal Amount);
