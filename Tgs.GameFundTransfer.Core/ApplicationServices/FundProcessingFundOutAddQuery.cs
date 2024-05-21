namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record FundProcessingFundOutAddQuery(
	string GameProviderCode,
	string PlayerCode,
	decimal Amount);
