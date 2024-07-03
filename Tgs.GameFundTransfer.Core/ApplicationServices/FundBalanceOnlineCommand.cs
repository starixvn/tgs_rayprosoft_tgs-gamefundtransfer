namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record FundBalanceOnlineCommand(
	string GameProviderCode,
	string PlayerCode,
	decimal Balance,
	decimal FreeBalance,
	decimal OutstandingBalance);
