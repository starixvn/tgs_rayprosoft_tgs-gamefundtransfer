using NSubstitute;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;

public class GameProviderFundInGetServiceUnitTest
{
	[Fact]
	public async Task Normal_Process()
	{
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new GameProviderFundInQuery(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 BrandCode: "GPI",
			 PlayerCode: "4ucmqplxb029",
			 CurrencyCode: "CNY",
			 GameProviderTransactionId: "72412202405200607050004ucmqplxb029",
			 Amount: 1);

		_ = fakeGameProvidersClient.PlayerFundInAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			brandCode: Arg.Is(query.BrandCode),
			playerCode: Arg.Is(query.PlayerCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			orderId: Arg.Is(query.GameProviderTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(query.GameProviderTransactionId);

		var sut = new GameProviderFundInGetService(
			fakeGameProvidersClient);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(query.GameProviderTransactionId, actual.GameProviderTransactionId);
	}

	[Fact]
	public async Task ArgumentException()
	{
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new GameProviderFundInQuery(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 BrandCode: "GPI",
			 PlayerCode: "4ucmqplxb029",
			 CurrencyCode: "CNY",
			 GameProviderTransactionId: "72412202405200607050004ucmqplxb029",
			 Amount: 1);

		_ = fakeGameProvidersClient.PlayerFundInAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			brandCode: Arg.Is(query.BrandCode),
			playerCode: Arg.Is(query.PlayerCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			orderId: Arg.Is(query.GameProviderTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(string.Empty);

		var sut = new GameProviderFundInGetService(
			fakeGameProvidersClient);

		_ = await Assert.ThrowsAsync<ArgumentException>(() => sut.GetAsync(query).AsTask());
	}
}
