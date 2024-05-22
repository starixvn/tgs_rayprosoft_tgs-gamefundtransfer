using System.Threading;
using NSubstitute;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;

public class FundBalanceGetServiceUnitTest
{
	[Fact]
	public async Task Normal_Process()
	{
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeFundBalanceRepository = Substitute.For<IFundBalanceRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();

		var query = new FundBalanceQuery(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 PlayerCode: "4ucmqplxb029");

		var playerInfo = new Models.PlayerInfo(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY");
		_ = fakePlayerRepository.GetAsync(
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		var fundBalanceInfo = new Models.FundBalanceInfo(
			Balance: 100,
			FreeBalance: 90,
			OutstandingBalance: 10);
		_ = fakeFundBalanceRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundBalanceInfo);

		var result = new ConvertPlatformBalanceResult(
			ExchangeRate: 1,
			Balance: 100,
			FreeBalance: 90,
			OutstandingBalance: 10);
		_ = fakeGameCurrencyClient.ConvertPlatformBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(playerInfo.CurrencyCode),
			balance: Arg.Is(fundBalanceInfo.Balance),
			freeBalance: Arg.Is(fundBalanceInfo.FreeBalance),
			outstandingBalance: Arg.Is(fundBalanceInfo.OutstandingBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(result);

		var sut = new FundBalanceGetService(
			fakePlayerRepository,
			fakeFundBalanceRepository,
			fakeMessageSender,
			fakeGameCurrencyClient);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(result.Balance, actual.Balance);
		Assert.Equal(result.FreeBalance, actual.FreeBalance);
		Assert.Equal(result.OutstandingBalance, actual.OutstandingBalance);
	}

	[Fact]
	public async Task Platform_Currency_IDR_1000_Amount_Game_Provider_IDR()
	{
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeFundBalanceRepository = Substitute.For<IFundBalanceRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();

		var query = new FundBalanceQuery(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 PlayerCode: "4ucmqplxb029");

		var playerInfo = new Models.PlayerInfo(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			WalletId: Guid.NewGuid(),
			CurrencyCode: "IDR_1000");
		_ = fakePlayerRepository.GetAsync(
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		var fundBalanceInfo = new Models.FundBalanceInfo(
			Balance: 1000.0m,
			FreeBalance: 100.0m,
			OutstandingBalance: 900m);
		_ = fakeFundBalanceRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundBalanceInfo);

		var result = new ConvertPlatformBalanceResult(
			ExchangeRate: 0.001m,
			Balance: 1m,
			FreeBalance: 0.1m,
			OutstandingBalance: 0.9m);
		_ = fakeGameCurrencyClient.ConvertPlatformBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(playerInfo.CurrencyCode),
			balance: Arg.Is(fundBalanceInfo.Balance),
			freeBalance: Arg.Is(fundBalanceInfo.FreeBalance),
			outstandingBalance: Arg.Is(fundBalanceInfo.OutstandingBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(result);

		var sut = new FundBalanceGetService(
			fakePlayerRepository,
			fakeFundBalanceRepository,
			fakeMessageSender,
			fakeGameCurrencyClient);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(result.Balance, actual.Balance);
		Assert.Equal(result.FreeBalance, actual.FreeBalance);
		Assert.Equal(result.OutstandingBalance, actual.OutstandingBalance);
	}

	[Fact]
	public async Task Platform_Currency_IDR_Game_Provider_IDR_1000_Amount_1()
	{
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeFundBalanceRepository = Substitute.For<IFundBalanceRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();

		var query = new FundBalanceQuery(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 PlayerCode: "4ucmqplxb029");

		var playerInfo = new Models.PlayerInfo(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			WalletId: Guid.NewGuid(),
			CurrencyCode: "IDR");
		_ = fakePlayerRepository.GetAsync(
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		var fundBalanceInfo = new Models.FundBalanceInfo(
			Balance: 10.0m,
			FreeBalance: 1.0m,
			OutstandingBalance: 9.0m);
		_ = fakeFundBalanceRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundBalanceInfo);

		var result = new ConvertPlatformBalanceResult(
			ExchangeRate: 1000m,
			Balance: 10000m,
			FreeBalance: 100m,
			OutstandingBalance: 900m);
		_ = fakeGameCurrencyClient.ConvertPlatformBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(playerInfo.CurrencyCode),
			balance: Arg.Is(fundBalanceInfo.Balance),
			freeBalance: Arg.Is(fundBalanceInfo.FreeBalance),
			outstandingBalance: Arg.Is(fundBalanceInfo.OutstandingBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(result);

		var sut = new FundBalanceGetService(
			fakePlayerRepository,
			fakeFundBalanceRepository,
			fakeMessageSender,
			fakeGameCurrencyClient);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(result.Balance, actual.Balance);
		Assert.Equal(result.FreeBalance, actual.FreeBalance);
		Assert.Equal(result.OutstandingBalance, actual.OutstandingBalance);
	}
}
