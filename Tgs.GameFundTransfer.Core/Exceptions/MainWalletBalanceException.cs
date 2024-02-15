namespace Tgs.GameFundTransfer.Core.Exceptions;

public class MainWalletBalanceException(string playerCode, decimal freeBalance) : Exception($"This player({playerCode}) is main wallet balance is less than zero.({freeBalance})")
{
}
