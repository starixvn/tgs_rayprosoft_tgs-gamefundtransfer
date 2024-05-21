namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record FundBalanceCacheCommand(
	Common.Protos.Action Action,
	string GameProviderCode,
	string PlayerCode,
	decimal Balance,
	decimal FreeBalance,
	decimal OutstandingBalance);
