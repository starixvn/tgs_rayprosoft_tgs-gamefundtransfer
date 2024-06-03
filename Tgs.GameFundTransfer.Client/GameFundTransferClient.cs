using Tgs.GameFundTransfer.Protos;

namespace Tgs.GameFundTransfer.Client;

internal class GameFundTransferClient(GameFundTransferService.GameFundTransferServiceClient client) : IGameFundTransferClient
{
	public async ValueTask<string> CheckingServiceAsync(string machineName, CancellationToken cancellationToken = default)
	{
		var request = new CheckingServiceRequest
		{
			MachineName = machineName,
		};

		var reply = await client.CheckingServiceAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return reply.MachineName;
	}

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

	public async ValueTask<GameFundTransferResultReply> FundInResultAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default)
	{
		var request = new GameFundTransferFundRequest
		{
			GameProviderCode = gameProviderCode,
			PlayerCode = playerCode,
			WalletId = walletId,
			CurrencyCode = currencyCode,
			Amount = amount,
		};

		return await client.FundInResultAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<GameFundTransferResultReply> FundInZeroResultAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, CancellationToken cancellationToken = default)
	{
		var request = new GameFundTransferFundZeroRequest
		{
			GameProviderCode = gameProviderCode,
			PlayerCode = playerCode,
			WalletId = walletId,
			CurrencyCode = currencyCode,
		};

		return await client.FundInZeroResultAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<bool> FundOutAsync(string gameProviderCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default)
	{
		var request = new GameFundTransferFundRequest
		{
			GameProviderCode = gameProviderCode,
			PlayerCode = playerCode,
			WalletId = walletId,
			CurrencyCode = currencyCode,
			Amount = amount,
		};

		var reply = await client.FundOutAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return reply.State;
	}
}
