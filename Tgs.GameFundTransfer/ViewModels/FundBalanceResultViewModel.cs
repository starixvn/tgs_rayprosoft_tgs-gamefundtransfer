namespace Tgs.GameFundTransfer.ViewModels;

public record FundBalanceResultViewModel(
	decimal Balance,
	decimal FreeBalance,
	decimal OutstandingBalance);
