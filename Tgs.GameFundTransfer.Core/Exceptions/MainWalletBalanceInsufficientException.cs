namespace Tgs.GameFundTransfer.Core.Exceptions;

public class MainWalletBalanceInsufficientException(string playerCode, string gpCode, decimal amount, decimal freeBalance) : Exception($"This player is ({playerCode}) main wallet has insufficient free balance for the transfer amount ({amount}) to the game provider ({gpCode}). ({freeBalance})")
{
}
