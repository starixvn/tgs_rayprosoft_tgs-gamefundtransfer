namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record FundBalanceResult(
	decimal Balance,
	decimal FreeBalance,
	decimal OutstandingBalance);
