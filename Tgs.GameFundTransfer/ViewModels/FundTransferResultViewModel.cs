namespace Tgs.GameFundTransfer.ViewModels;

public record FundTransferResultViewModel(
	Guid FundTransactionId,
	string CashTransactionId,
	Guid CashRespTransactionId,
	string GameTransactionId,
	string GameRespTransactionId);
