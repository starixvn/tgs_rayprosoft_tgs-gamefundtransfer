using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Tgs.GameFundTransfer.Core;
using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Repositories.UnitTest;

public class GameProviderCacheRepositoryUnitTest
{
	[Fact]
	public async Task Get_Is_Not_Cache()
	{
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeMemoryCache = Substitute.For<IMemoryCache>();
		var timeSpan = TimeSpan.FromSeconds(30);

		var gameProviderCode = GameProviderType.Kaiyuan;

		var key = string.Concat(typeof(GameProviderCacheRepository).Namespace!.Replace(".", "-").ToLower(), ":", "gameprovider", ":", "get", ":", gameProviderCode);

		_ = fakeMemoryCache.TryGetValue(
			key: Arg.Is<object>(key),
			out _)
			.Returns(false);

		var gameProviderInfo = new Core.Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: gameProviderCode,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(gameProviderCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var sut = new GameProviderCacheRepository(
			fakeGameProviderRepository,
			fakeMemoryCache,
			timeSpan);

		var actual = await sut.GetAsync(gameProviderCode);

		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(gameProviderInfo.TransferWallet, actual.TransferWallet);
	}
}
