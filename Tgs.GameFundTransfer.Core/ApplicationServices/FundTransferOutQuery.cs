namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundTransferOutQuery(
	string GameProviderCode,
	string PlayerCode,
	decimal Amount);
