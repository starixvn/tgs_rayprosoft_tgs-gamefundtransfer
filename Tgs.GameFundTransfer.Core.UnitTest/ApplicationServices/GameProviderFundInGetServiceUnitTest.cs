using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;

public class GameProviderFundInGetServiceUnitTest
{
	[Fact]
	public async Task Normal_Process()
	{
		var faceLogger = NullLogger<GameProviderFundInGetService>.Instance;
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new GameProviderFundInQuery(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 BrandCode: "GPI",
			 PlayerCode: "4ucmqplxb029",
			 CurrencyCode: "CNY",
			 GameTransactionId: "72412202405200607050004ucmqplxb029",
			 Amount: 1);

		var response = new PlayerFundResponse(
			OrderId: query.GameTransactionId,
			ExternalOrderId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
		_ = fakeGameProvidersClient.PlayerFundInAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
			orderId: Arg.Is(query.GameTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(response);

		var sut = new GameProviderFundInGetService(
			faceLogger,
			fakeGameProvidersClient,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(query.GameTransactionId, actual.GameTransactionId);
		Assert.Equal(response.ExternalOrderId, actual.GameRespTransactionId);

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			subject: Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferTrackerRequest),
			data: Arg.Is(new Common.Protos.GameFundTransferTrackerRequest
			{
				GameProviderCode = query.GameProviderCode,
				PlayerCode = query.PlayerCode,
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ArgumentException_ThrowIfNullOrWhiteSpace_OrderId()
	{
		var faceLogger = NullLogger<GameProviderFundInGetService>.Instance;
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new GameProviderFundInQuery(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 BrandCode: "GPI",
			 PlayerCode: "4ucmqplxb029",
			 CurrencyCode: "CNY",
			 GameTransactionId: "72412202405200607050004ucmqplxb029",
			 Amount: 1);

		var response = new PlayerFundResponse(
			OrderId: string.Empty,
			ExternalOrderId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
		_ = fakeGameProvidersClient.PlayerFundInAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
			orderId: Arg.Is(query.GameTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(response);

		var sut = new GameProviderFundInGetService(
			faceLogger,
			fakeGameProvidersClient,
			fakeMessageSender);

		_ = await Assert.ThrowsAsync<ArgumentException>(() => sut.GetAsync(query).AsTask());

		_ = fakeMessageSender
			.Received(0)
			.PublishAsync(
			subject: Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferTrackerRequest),
			data: Arg.Is(new Common.Protos.GameFundTransferTrackerRequest
			{
				GameProviderCode = query.GameProviderCode,
				PlayerCode = query.PlayerCode,
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}
}
