namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundTransferInOneClickQuery(
	string GameProviderCode,
	string PlayerCode);
