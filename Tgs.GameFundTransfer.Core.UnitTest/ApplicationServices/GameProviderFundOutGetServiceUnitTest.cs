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
			 PlayerCode: "4ucmqplxb029",
			 GameProviderTransactionId: "72412202405200607050004ucmqplxb029",
			 Amount: 1);

		var response = new PlayerFundResponse(
			OrderId: query.GameProviderTransactionId,
			ExternalOrderId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
		_ = fakeGameProvidersClient.PlayerFundOutAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
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
		Assert.Equal(response.ExternalOrderId, actual.ExternalOrderId);
	}

	[Fact]
	public async Task ArgumentException_ThrowIfNullOrWhiteSpace_OrderId()
	{
		var faceLogger = NullLogger<GameProviderFundOutGetService>.Instance;
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new GameProviderFundOutQuery(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 PlayerCode: "4ucmqplxb029",
			 GameProviderTransactionId: "72412202405200607050004ucmqplxb029",
			 Amount: 1);

		var response = new PlayerFundResponse(
			OrderId: string.Empty,
			ExternalOrderId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
		_ = fakeGameProvidersClient.PlayerFundOutAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
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
