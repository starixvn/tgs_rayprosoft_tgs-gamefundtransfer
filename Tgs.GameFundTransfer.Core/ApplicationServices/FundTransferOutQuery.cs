namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundTransferOutQuery(
	string GameProviderCode,
	string PlayerCode,
	Guid WalletId,
	string CurrencyCode,
	decimal Amount);
