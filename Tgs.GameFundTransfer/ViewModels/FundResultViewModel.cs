using Tgs.GameFundTransfer.Core;

namespace Tgs.GameFundTransfer.ViewModels;

public record FundResultViewModel(
	StateCode State,
	string Message,
	Guid FundTransactionId,
	Guid TransactionId,
	string ExternalTransactionId);
