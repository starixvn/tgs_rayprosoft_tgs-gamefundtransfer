namespace Tgs.GameFundTransfer.Core.Exceptions;

public class FreeBalanceException(string playerCode, string gpCode, decimal freeBalance) : Exception($"This player({playerCode}) gets a game provider({gpCode}) free balance less than zero.({freeBalance})")
{
}