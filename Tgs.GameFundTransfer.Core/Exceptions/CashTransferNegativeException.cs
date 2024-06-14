namespace Tgs.GameFundTransfer.Core.Exceptions;

public class CashTransferNegativeException(string cashTransactionId, Guid cashRespTransactionId, string playerCode, decimal balance) : Exception(
	$"This CashTransactionIdentifier({cashTransactionId}) conducts the player({playerCode}) main wallet transaction resulting in Balance({balance}) being negative and CashResponseTransactionIdentifier({cashRespTransactionId}).");
