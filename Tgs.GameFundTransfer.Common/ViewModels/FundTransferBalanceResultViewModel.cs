namespace Tgs.GameFundTransfer.Common.ViewModels;
public record FundTransferBalanceResultViewModel(
	decimal Balance,
	decimal FreeBalance,
	decimal OutstandingBalance);
