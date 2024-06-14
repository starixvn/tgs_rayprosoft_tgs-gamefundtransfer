namespace Tgs.GameFundTransfer.Core.ApplicationServices;

public record FundTransferResult(
	Guid FundTransactionId,
	string CashTransactionId,
	Guid CashRespTransactionId,
	string GameTransactionId,
	string GameRespTransactionId);
