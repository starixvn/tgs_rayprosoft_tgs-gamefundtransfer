namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundTransferInQuery(
	string GameProviderCode,
	string PlayerCode,
	Guid WalletId,
	string CurrencyCode,
	decimal Amount);
