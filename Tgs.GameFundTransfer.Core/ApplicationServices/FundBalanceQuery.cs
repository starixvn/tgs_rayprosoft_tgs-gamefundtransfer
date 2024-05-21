namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record FundBalanceQuery(
	string GameProviderCode,
	string PlayerCode);
