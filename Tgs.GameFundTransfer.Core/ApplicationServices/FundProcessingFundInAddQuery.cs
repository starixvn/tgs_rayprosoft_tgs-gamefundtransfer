namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record FundProcessingFundInAddQuery(
	string GameProviderCode,
	string PlayerCode,
	decimal Amount);
