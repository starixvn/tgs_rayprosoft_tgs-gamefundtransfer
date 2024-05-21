namespace Tgs.GameFundTransfer.Core;

public interface IGameProviderTransactionIdUtensil
{
	ValueTask<string> GetCards365TxnIdAsync(bool Deposit, CancellationToken cancellationToken = default);

	ValueTask<string> GetFaChaiTxnIdAsync(bool Deposit, CancellationToken cancellationToken = default);

	ValueTask<string> GetGKXGamingTxnIdAsync(string playerCode, CancellationToken cancellationToken = default);

	ValueTask<string> GetKaiyuanTxnIdAsync(string playerCode, CancellationToken cancellationToken = default);

	ValueTask<string> GetLEGamingTxnIdAsync(string playerCode, CancellationToken cancellationToken = default);

	ValueTask<string> GetSBOTxnIdAsync(bool Deposit, CancellationToken cancellationToken = default);

	ValueTask<string> GetTPhereTxnIdAsync(bool Deposit, CancellationToken cancellationToken = default);

	ValueTask<string> GetTxnIdAsync(Guid gameProviderFundTransferTransactionId, CancellationToken cancellationToken = default);

	ValueTask<string> GetV8PokerTxnIdAsync(string playerCode, CancellationToken cancellationToken = default);

	ValueTask<long> GetYeeBetTxnIdAsync(CancellationToken cancellationToken = default);
}
