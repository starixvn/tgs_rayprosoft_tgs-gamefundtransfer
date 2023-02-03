using System.ComponentModel;

namespace Tgs.GameFundTransfer.Core;

public enum StateCode
{
    [Description("Success.")]
    Success = 0,

    [Description("An unknown error occurred.")]
    Exception = 404,

    [Description("This transaction is locked.")]
    TransactionLock = 423,
    [Description("The transaction number provided by this game provider is empty.")]
    ExternalTransactionIdEmpty =424,

    [Description("This balance is insufficient.")]
    InsufficientBalance = 500,
}
