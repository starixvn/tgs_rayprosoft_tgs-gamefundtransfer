using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Tgs.GameFundTransfer.Core;

namespace Tgs.GameFundTransfer.Repositories.UnitTest;

public class PlayerCacheRepositoryUnitTest
{
	[Fact]
	public async Task Get_Is_Not_Cache()
	{
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeMemoryCache = Substitute.For<IMemoryCache>();
		var timeSpan = TimeSpan.FromSeconds(30);

		var playerCode = "4ucmqplxb029";

		var key = string.Concat(typeof(PlayerCacheRepository).Namespace!.Replace(".", "-").ToLower(), ":", "player", ":", "get", ":", playerCode);

		_ = fakeMemoryCache.TryGetValue(
			key: Arg.Is<object>(key),
			out _)
			.Returns(false);

		var playerInfo = new Core.Models.PlayerInfo(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: playerCode,
			Username: playerCode,
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CMY");
		_ = fakePlayerRepository.GetAsync(
			playerCode: Arg.Is(playerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		var sut = new PlayerCacheRepository(
			fakePlayerRepository,
			fakeMemoryCache,
			timeSpan);

		var actual = await sut.GetAsync(playerCode);

		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(playerInfo.WalletId, actual.WalletId);
		Assert.Equal(playerInfo.CurrencyCode, actual.CurrencyCode);
	}
}
