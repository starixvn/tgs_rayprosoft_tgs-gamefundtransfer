using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Tgs.GameFundTransfer.Core;

namespace Tgs.GameFundTransfer.Repositories.UnitTest;

public class GameProviderConfigurationCacheRepositoryUnitTest
{
	[Fact]
	public async Task Get_Is_Not_Cache()
	{
		var fakeGameProviderConfigurationRepository = Substitute.For<IGameProviderConfigurationRepository>();
		var fakeMemoryCache = Substitute.For<IMemoryCache>();
		var timeSpan = TimeSpan.FromSeconds(30);

		var gameProviderConfigurationId = Guid.NewGuid();

		var key = string.Concat(typeof(GameProviderConfigurationCacheRepository).Namespace!.Replace(".", "-").ToLower(), ":", "gameproviderconfiguration", ":", "get", ":", "configurationdata", ":", gameProviderConfigurationId);

		_ = fakeMemoryCache.TryGetValue(
			key: Arg.Is<object>(key),
			out _)
		.Returns(false);

		var jsonObject = new JsonObject
		{
			["AgentId"] = "123456"
		};
		_ = fakeGameProviderConfigurationRepository.GetConfigurationDataAsync(
			gameProviderConfigurationId: Arg.Is(gameProviderConfigurationId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(jsonObject);

		var sut = new GameProviderConfigurationCacheRepository(
			fakeGameProviderConfigurationRepository,
			fakeMemoryCache,
			timeSpan);

		var actual = await sut.GetConfigurationDataAsync(gameProviderConfigurationId);

		Assert.Equal(jsonObject.ToJsonString(), actual.ToJsonString());
	}
}
