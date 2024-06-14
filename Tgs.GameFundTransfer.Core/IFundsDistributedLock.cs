namespace Tgs.GameFundTransfer.Core;

public interface IFundsDistributedLock
{
	ValueTask LockingAsync(string gameProviderCode, string playerCode);

	ValueTask UnlockAsync(string gameProviderCode, string playerCode);
}
