namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundTransferInQuery(
	string GameProviderCode,
	string PlayerCode,
	decimal Amount);
