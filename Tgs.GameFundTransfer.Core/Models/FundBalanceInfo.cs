namespace Tgs.GameFundTransfer.Core.Models;
public record FundBalanceInfo(
	decimal Balance,
	decimal FreeBalance,
	decimal OutstandingBalance);
