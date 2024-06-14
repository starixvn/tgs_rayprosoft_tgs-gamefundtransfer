using Tgs.GameFundTransfer.Protos;

namespace Tgs.GameFundTransfer.Client;

internal class GameFundTransferClient(Protos.GameFundTransfer.GameFundTransferClient client) : IGameFundTransferClient
{
	public async ValueTask<FundBalanceReply> FundBalanceAsync(string gameProviderCode, string playerCode, CancellationToken cancellationToken = default)
	{
		var request = new FundBalanceRequest
		{
			GameProviderCode = gameProviderCode,
			PlayerCode = playerCode,
		};

		return await client.FundBalanceAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<FundTransferReply> FundTransferInAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default)
	{
		var request = new FundTransferRequest
		{
			GameProviderCode = gameProviderCode,
			PlayerCode = playerCode,
			WalletId = walletId,
			CurrencyCode = currencyCode,
			Amount = amount,
		};

		return await client.FundTransferInAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<FundTransferReply> FundTransferInOneClickAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, CancellationToken cancellationToken = default)
	{
		var request = new FundTransferInOneClickRequest
		{
			GameProviderCode = gameProviderCode,
			PlayerCode = playerCode,
			WalletId = walletId,
			CurrencyCode = currencyCode,
		};

		return await client.FundTransferInOneClickAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<FundTransferReply> FundTransferOutAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default)
	{
		var request = new FundTransferRequest
		{
			GameProviderCode = gameProviderCode,
			PlayerCode = playerCode,
			WalletId = walletId,
			CurrencyCode = currencyCode,
			Amount = amount,
		};

		return await client.FundTransferOutAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}
}
