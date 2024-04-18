namespace Tgs.GameFundTransfer.Core;

public interface IFundsDistributedLock
{
	Task<bool> LockingAsync(string gameProviderCode, string playerCode);

	Task UnlockAsync(string gameProviderCode, string playerCode);
}
