using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tgs.GameFundTransfer.Core.Exceptions;

public class FreeBalanceInsufficientException(string playerCode, string gpCode, decimal amount, decimal freeBalance) : Exception($"This player ({playerCode}) game provider ({gpCode}) transferred to the main wallet transfer amount ({amount}) free balance is insufficient. ({freeBalance})")
{
}