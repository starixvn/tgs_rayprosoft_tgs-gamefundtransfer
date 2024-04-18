using Tgs.GameFundTransfer.Protos;

namespace Tgs.GameFundTransfer.Client;

internal class GameFundTransferClient : IGameFundTransferClient
{
	private readonly GameFundTransferService.GameFundTransferServiceClient _client;

	public GameFundTransferClient(GameFundTransferService.GameFundTransferServiceClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}

	public async ValueTask<string> CheckingServiceAsync(string machineName, CancellationToken cancellationToken = default)
	{
		var request = new CheckingServiceRequest
		{
			MachineName = machineName,
		};

		var reply = await _client.CheckingServiceAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return reply.MachineName;
	}

	public async ValueTask<bool> FundInAsync(Guid gameProviderId, string gameProviderCode, string brandCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default)
	{
		var request = new GameFundTransferFundRequest
		{
			GameProviderId = gameProviderId,
			GameProviderCode = gameProviderCode,
			BrandCode = brandCode,
			PlayerCode = playerCode,
			WalletId = walletId,
			CurrencyCode = currencyCode,
			Amount = amount,
		};

		var reply = await _client.FundInAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return reply.State;
	}

	public async ValueTask<GameFundTransferResultReply> FundInResultAsync(Guid gameProviderId, string gameProviderCode, string brandCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default)
	{
		var request = new GameFundTransferFundRequest
		{
			GameProviderId = gameProviderId,
			GameProviderCode = gameProviderCode,
			BrandCode = brandCode,
			PlayerCode = playerCode,
			WalletId = walletId,
			CurrencyCode = currencyCode,
			Amount = amount,
		};

		return await _client.FundInResultAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<GameFundTransferResultReply> FundInZeroResultAsync(Guid gameProviderId, string gameProviderCode, string brandCode, string playerCode, Guid walletId, string currencyCode, CancellationToken cancellationToken = default)
	{
		var request = new GameFundTransferFundZeroRequest
		{
			GameProviderId = gameProviderId,
			GameProviderCode = gameProviderCode,
			BrandCode = brandCode,
			PlayerCode = playerCode,
			WalletId = walletId,
			CurrencyCode = currencyCode,
		};

		return await _client.FundInZeroResultAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<bool> FundOutAsync(Guid gameProviderId, string gameProviderCode, string brandCode, string playerCode, Guid walletId, string currencyCode, decimal amount, CancellationToken cancellationToken = default)
	{
		var request = new GameFundTransferFundRequest
		{
			GameProviderId = gameProviderId,
			GameProviderCode = gameProviderCode,
			BrandCode = brandCode,
			PlayerCode = playerCode,
			WalletId = walletId,
			CurrencyCode = currencyCode,
			Amount = amount,
		};

		var reply = await _client.FundOutAsync(
			request: request,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return reply.State;
	}
}
