using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Tgs.GameFundTransfer.Core;

namespace Tgs.GameFundTransfer.Repositories.UnitTest;

public class BrandGameProviderCacheRepositoryUnitTest
{
	[Fact]
	public async Task Get_Is_Not_Cache()
	{
		var fakeBrandGameProviderRepository = Substitute.For<IBrandGameProviderRepository>();
		var fakeMemoryCache = Substitute.For<IMemoryCache>();
		var timeSpan = TimeSpan.FromSeconds(30);

		var brandId = Guid.NewGuid();
		var gameProviderId = Guid.NewGuid();

		var key = string.Concat(typeof(BrandGameProviderCacheRepository).Namespace!.Replace(".", "-").ToLower(), ":", "brandgameprovider", ":", "get", ":", "gameproviderconfigurationid", ":", brandId, ":", gameProviderId);

		_ = fakeMemoryCache.TryGetValue(
			key: Arg.Is<object>(key),
			out _)
			.Returns(false);

		var gameProviderConfigurationId = Guid.NewGuid();
		_ = fakeBrandGameProviderRepository.GetGameProviderConfigurationIdAsync(
			brandId: Arg.Is(brandId),
			gameProviderId: Arg.Is(gameProviderId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderConfigurationId);

		var sut = new BrandGameProviderCacheRepository(
			fakeBrandGameProviderRepository,
			fakeMemoryCache,
			timeSpan);

		var actual = await sut.GetGameProviderConfigurationIdAsync(brandId, gameProviderId);

		Assert.Equal(gameProviderConfigurationId, actual);
	}
}
