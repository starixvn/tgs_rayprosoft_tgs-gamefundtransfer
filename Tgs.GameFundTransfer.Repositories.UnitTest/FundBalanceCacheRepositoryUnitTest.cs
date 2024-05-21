using System.Text.Json;
using NSubstitute;
using StackExchange.Redis;
using Tgs.GameFundTransfer.Core;
using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Repositories.UnitTest;

public class FundBalanceCacheRepositoryUnitTest
{
	[Fact]
	public async Task Get_Is_Not_Cache()
	{
		var fakeFundBalanceRepository = Substitute.For<IFundBalanceRepository>();
		var fakeDatabase = Substitute.For<IDatabase>();
		var timeSpan = TimeSpan.FromSeconds(30);

		var gameProviderCode = GameProviderType.Kaiyuan;
		var playerCode = "4ucmqplxb029";

		var key = string.Concat(typeof(FundBalanceCacheRepository).Namespace!.Replace(".", "-").ToLower(), ":", "balance", ":", gameProviderCode, ":", playerCode);

		_ = fakeDatabase.KeyExistsAsync(
			key: Arg.Is<RedisKey>(key))
			.Returns(Task.FromResult(false));

		var fundBalanceInfo = new Core.Models.FundBalanceInfo(
			Balance: 100,
			FreeBalance: 90,
			OutstandingBalance: 10);
		_ = fakeFundBalanceRepository.GetAsync(
			gameProviderCode: Arg.Is(gameProviderCode),
			playerCode: Arg.Is(playerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundBalanceInfo);

		var sut = new FundBalanceCacheRepository(
			fakeFundBalanceRepository,
			fakeDatabase,
			timeSpan);

		var actual = await sut.GetAsync(gameProviderCode, playerCode);

		Assert.NotNull(actual);
		Assert.Equal(fundBalanceInfo.Balance, actual.Balance);
		Assert.Equal(fundBalanceInfo.FreeBalance, actual.FreeBalance);
		Assert.Equal(fundBalanceInfo.OutstandingBalance, actual.OutstandingBalance);
	}

	[Fact]
	public async Task Get_Is_Cache()
	{
		var fakeFundBalanceRepository = Substitute.For<IFundBalanceRepository>();
		var fakeDatabase = Substitute.For<IDatabase>();
		var timeSpan = TimeSpan.FromSeconds(30);

		var gameProviderCode = GameProviderType.Kaiyuan;
		var playerCode = "4ucmqplxb029";

		var key = string.Concat(typeof(FundBalanceCacheRepository).Namespace!.Replace(".", "-").ToLower(), ":", "balance", ":", gameProviderCode, ":", playerCode);

		_ = fakeDatabase.KeyExistsAsync(
			key: Arg.Is<RedisKey>(key))
			.Returns(Task.FromResult(true));

		var fundBalanceInfo = new Core.Models.FundBalanceInfo(
			Balance: 100,
			FreeBalance: 90,
			OutstandingBalance: 10);
		_ = fakeDatabase.StringGetAsync(
			key: Arg.Is<RedisKey>(key))
			.Returns((RedisValue)JsonSerializer.Serialize(fundBalanceInfo));

		var sut = new FundBalanceCacheRepository(
			fakeFundBalanceRepository,
			fakeDatabase,
			timeSpan);

		var actual = await sut.GetAsync(gameProviderCode, playerCode);

		Assert.NotNull(actual);
		Assert.Equal(fundBalanceInfo.Balance, actual.Balance);
		Assert.Equal(fundBalanceInfo.FreeBalance, actual.FreeBalance);
		Assert.Equal(fundBalanceInfo.OutstandingBalance, actual.OutstandingBalance);

		_ = fakeFundBalanceRepository
		   .Received(0)
		   .GetAsync(
		   gameProviderCode: Arg.Is(gameProviderCode),
		   playerCode: Arg.Is(playerCode),
		   cancellationToken: Arg.Any<CancellationToken>());
	}
}
