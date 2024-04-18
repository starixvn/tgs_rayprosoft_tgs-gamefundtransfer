namespace Tgs.GameFundTransfer.Core;

public record FundResult
{
	public StateCode State { get; init; }
	public string Message { get; init; }
	public Guid FundTransactionId { get; init; }
	public Guid TransactionId { get; init; }
	public string ExternalTransactionId { get; init; }
	public FundResult(
		StateCode state,
		string message)
	{
		State = state;
		Message = message;
		FundTransactionId = Guid.Empty;
		TransactionId = Guid.Empty;
		ExternalTransactionId = string.Empty;
	}

	public FundResult(
		StateCode state,
		string message,
		Guid fundTransactionId,
		Guid transactionId,
		string externalTransactionId
		)
	{
		State = state;
		Message = message;
		FundTransactionId = fundTransactionId;
		TransactionId = transactionId;
		ExternalTransactionId = externalTransactionId;
	}
}
