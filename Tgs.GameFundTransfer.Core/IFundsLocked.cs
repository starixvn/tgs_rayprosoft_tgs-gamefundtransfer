namespace Tgs.GameFundTransfer.Core;

public interface IFundsLocked
{
    Task<bool> ExistAsync(string gameProviderCode, string playerCode);

    Task<bool> LockingAsync(string gameProviderCode, string playerCode);

    ValueTask UnlockAsync(string gameProviderCode, string playerCode);
}
