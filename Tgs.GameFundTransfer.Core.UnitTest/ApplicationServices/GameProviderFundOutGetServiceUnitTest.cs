using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;

public class GameProviderFundOutGetServiceUnitTest
{
	[Fact]
	public async Task Normal_Process()
	{
		var faceLogger = NullLogger<GameProviderFundOutGetService>.Instance;
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new GameProviderFundOutQuery(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 BrandCode: "GPI",
			 PlayerCode: "4ucmqplxb029",
			 CurrencyCode: "CNY",
			 GameProviderTransactionId: "72412202405200607050004ucmqplxb029",
			 Amount: 1);

		var response = new PlayerFundResponse(
			OrderId: query.GameProviderTransactionId,
			GameProviderTransactionId: query.GameProviderTransactionId,
			ExternalGameProviderTransactionId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
		_ = fakeGameProvidersClient.PlayerFundOutAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			brandCode: Arg.Is(query.BrandCode),
			playerCode: Arg.Is(query.PlayerCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			orderId: Arg.Is(query.GameProviderTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(response);

		var sut = new GameProviderFundOutGetService(
			faceLogger,
			fakeGameProvidersClient);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(query.GameProviderTransactionId, actual.OrderId);
		Assert.Equal(query.GameProviderTransactionId, actual.GameProviderTransactionId);
	}

	[Fact]
	public async Task ArgumentException_ThrowIfNullOrWhiteSpace_OrderId()
	{
		var faceLogger = NullLogger<GameProviderFundOutGetService>.Instance;
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new GameProviderFundOutQuery(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 BrandCode: "GPI",
			 PlayerCode: "4ucmqplxb029",
			 CurrencyCode: "CNY",
			 GameProviderTransactionId: "72412202405200607050004ucmqplxb029",
			 Amount: 1);

		var response = new PlayerFundResponse(
			OrderId: string.Empty,
			GameProviderTransactionId: query.GameProviderTransactionId,
			ExternalGameProviderTransactionId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
		_ = fakeGameProvidersClient.PlayerFundOutAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			brandCode: Arg.Is(query.BrandCode),
			playerCode: Arg.Is(query.PlayerCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			orderId: Arg.Is(query.GameProviderTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(response);

		var sut = new GameProviderFundOutGetService(
			faceLogger,
			fakeGameProvidersClient);

		_ = await Assert.ThrowsAsync<ArgumentException>(() => sut.GetAsync(query).AsTask());
	}

	[Fact]
	public async Task ArgumentException_ThrowIfNullOrWhiteSpace_GameProviderTransactionId()
	{
		var faceLogger = NullLogger<GameProviderFundOutGetService>.Instance;
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new GameProviderFundOutQuery(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 BrandCode: "GPI",
			 PlayerCode: "4ucmqplxb029",
			 CurrencyCode: "CNY",
			 GameProviderTransactionId: "72412202405200607050004ucmqplxb029",
			 Amount: 1);

		var response = new PlayerFundResponse(
			OrderId: query.GameProviderTransactionId,
			GameProviderTransactionId: string.Empty,
			ExternalGameProviderTransactionId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
		_ = fakeGameProvidersClient.PlayerFundOutAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			brandCode: Arg.Is(query.BrandCode),
			playerCode: Arg.Is(query.PlayerCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			orderId: Arg.Is(query.GameProviderTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(response);

		var sut = new GameProviderFundOutGetService(
			faceLogger,
			fakeGameProvidersClient);

		_ = await Assert.ThrowsAsync<ArgumentException>(() => sut.GetAsync(query).AsTask());
	}
}
