using System.Text.Json.Nodes;
using NSubstitute;
using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Core.UnitTest;

public class GameProviderTransactionIdUtensilUnitTest
{
	[Theory]
	[InlineData(true, 1716176296, "D1716176296")]
	[InlineData(false, 1716176296, "W1716176296")]
	public async Task Get_Cards365_TxnId(bool deposit, long id, string expected)
	{
		var fakeTimeProvider = Substitute.For<TimeProvider>();
		var fakeSnowflakeWorkUtensil = Substitute.For<ISnowflakeWorkUtensil>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeBrandGameProviderRepository = Substitute.For<IBrandGameProviderRepository>();
		var fakeGameProviderConfigurationRepository = Substitute.For<IGameProviderConfigurationRepository>();

		_ = fakeSnowflakeWorkUtensil.GetId()
			.Returns(id);

		var sut = new GameProviderTransactionIdUtensil(
			fakeTimeProvider,
			fakeSnowflakeWorkUtensil,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeBrandGameProviderRepository,
			fakeGameProviderConfigurationRepository);

		var actual = await sut.GetCards365TxnIdAsync(deposit);

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData(true, 1716176296, "D1716176296")]
	[InlineData(false, 1716176296, "W1716176296")]
	public async Task Get_FaChai_TxnId(bool deposit, long id, string expected)
	{
		var fakeTimeProvider = Substitute.For<TimeProvider>();
		var fakeSnowflakeWorkUtensil = Substitute.For<ISnowflakeWorkUtensil>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeBrandGameProviderRepository = Substitute.For<IBrandGameProviderRepository>();
		var fakeGameProviderConfigurationRepository = Substitute.For<IGameProviderConfigurationRepository>();

		_ = fakeSnowflakeWorkUtensil.GetId()
			.Returns(id);

		var sut = new GameProviderTransactionIdUtensil(
			fakeTimeProvider,
			fakeSnowflakeWorkUtensil,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeBrandGameProviderRepository,
			fakeGameProviderConfigurationRepository);

		var actual = await sut.GetFaChaiTxnIdAsync(deposit);

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("4ucmqplxb029", "2024-05-20T06:07:05", "60073", "60073202405200607050004ucmqplxb029")]
	public async Task Get_GKXGaming_TxnId(string playerCode, string utcNow, string agentId, string expected)
	{
		var fakeTimeProvider = Substitute.For<TimeProvider>();
		var fakeSnowflakeWorkUtensil = Substitute.For<ISnowflakeWorkUtensil>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeBrandGameProviderRepository = Substitute.For<IBrandGameProviderRepository>();
		var fakeGameProviderConfigurationRepository = Substitute.For<IGameProviderConfigurationRepository>();

		_ = fakeTimeProvider.GetUtcNow()
			.Returns(DateTimeOffset.Parse(utcNow));

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.GKXGaming,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(GameProviderType.GKXGaming),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var playerInfo = new Models.PlayerInfo(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: playerCode,
			Username: playerCode,
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CMY");
		_ = fakePlayerRepository.GetAsync(
			playerCode: Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		var gameProviderConfigurationId = Guid.NewGuid();
		_ = fakeBrandGameProviderRepository.GetGameProviderConfigurationIdAsync(
			brandId: Arg.Is(playerInfo.BrandId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderConfigurationId);
		var jsonObject = new JsonObject
		{
			["AgentId"] = agentId
		};
		_ = fakeGameProviderConfigurationRepository.GetConfigurationDataAsync(
			gameProviderConfigurationId: Arg.Is(gameProviderConfigurationId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(jsonObject);

		var sut = new GameProviderTransactionIdUtensil(
			fakeTimeProvider,
			fakeSnowflakeWorkUtensil,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeBrandGameProviderRepository,
			fakeGameProviderConfigurationRepository);

		var actual = await sut.GetGKXGamingTxnIdAsync(playerCode);

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("4ucmqplxb029", "2024-05-20T06:07:05", "72412", "72412202405200607050004ucmqplxb029")]
	public async Task Get_Kaiyuan_TxnId(string playerCode, string utcNow, string agentId, string expected)
	{
		var fakeTimeProvider = Substitute.For<TimeProvider>();
		var fakeSnowflakeWorkUtensil = Substitute.For<ISnowflakeWorkUtensil>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeBrandGameProviderRepository = Substitute.For<IBrandGameProviderRepository>();
		var fakeGameProviderConfigurationRepository = Substitute.For<IGameProviderConfigurationRepository>();

		_ = fakeTimeProvider.GetUtcNow()
			.Returns(DateTimeOffset.Parse(utcNow));

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(GameProviderType.Kaiyuan),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var playerInfo = new Models.PlayerInfo(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: playerCode,
			Username: playerCode,
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CMY");
		_ = fakePlayerRepository.GetAsync(
			playerCode: Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		var gameProviderConfigurationId = Guid.NewGuid();
		_ = fakeBrandGameProviderRepository.GetGameProviderConfigurationIdAsync(
			brandId: Arg.Is(playerInfo.BrandId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderConfigurationId);
		var jsonObject = new JsonObject
		{
			["AgentId"] = agentId
		};
		_ = fakeGameProviderConfigurationRepository.GetConfigurationDataAsync(
			gameProviderConfigurationId: Arg.Is(gameProviderConfigurationId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(jsonObject);

		var sut = new GameProviderTransactionIdUtensil(
			fakeTimeProvider,
			fakeSnowflakeWorkUtensil,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeBrandGameProviderRepository,
			fakeGameProviderConfigurationRepository);

		var actual = await sut.GetKaiyuanTxnIdAsync(playerCode);

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("4ucmqplxb029", "2024-05-20T06:07:05", "200995", "200995202405200607050004ucmqplxb029")]
	public async Task Get_LEGaming_TxnId(string playerCode, string utcNow, string agentId, string expected)
	{
		var fakeTimeProvider = Substitute.For<TimeProvider>();
		var fakeSnowflakeWorkUtensil = Substitute.For<ISnowflakeWorkUtensil>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeBrandGameProviderRepository = Substitute.For<IBrandGameProviderRepository>();
		var fakeGameProviderConfigurationRepository = Substitute.For<IGameProviderConfigurationRepository>();

		_ = fakeTimeProvider.GetUtcNow()
			.Returns(DateTimeOffset.Parse(utcNow));

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.LEGaming,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(GameProviderType.LEGaming),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var playerInfo = new Models.PlayerInfo(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: playerCode,
			Username: playerCode,
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CMY");
		_ = fakePlayerRepository.GetAsync(
			playerCode: Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		var gameProviderConfigurationId = Guid.NewGuid();
		_ = fakeBrandGameProviderRepository.GetGameProviderConfigurationIdAsync(
			brandId: Arg.Is(playerInfo.BrandId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderConfigurationId);
		var jsonObject = new JsonObject
		{
			["AgentId"] = agentId
		};
		_ = fakeGameProviderConfigurationRepository.GetConfigurationDataAsync(
			gameProviderConfigurationId: Arg.Is(gameProviderConfigurationId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(jsonObject);

		var sut = new GameProviderTransactionIdUtensil(
			fakeTimeProvider,
			fakeSnowflakeWorkUtensil,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeBrandGameProviderRepository,
			fakeGameProviderConfigurationRepository);

		var actual = await sut.GetLEGamingTxnIdAsync(playerCode);

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData(true, 1716176296, "D1716176296")]
	[InlineData(false, 1716176296, "W1716176296")]
	public async Task Get_SBO_TxnId(bool deposit, long id, string expected)
	{
		var fakeTimeProvider = Substitute.For<TimeProvider>();
		var fakeSnowflakeWorkUtensil = Substitute.For<ISnowflakeWorkUtensil>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeBrandGameProviderRepository = Substitute.For<IBrandGameProviderRepository>();
		var fakeGameProviderConfigurationRepository = Substitute.For<IGameProviderConfigurationRepository>();

		_ = fakeSnowflakeWorkUtensil.GetId()
			.Returns(id);

		var sut = new GameProviderTransactionIdUtensil(
			fakeTimeProvider,
			fakeSnowflakeWorkUtensil,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeBrandGameProviderRepository,
			fakeGameProviderConfigurationRepository);

		var actual = await sut.GetSBOTxnIdAsync(deposit);

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData(true, 1716176296, "D1716176296")]
	[InlineData(false, 1716176296, "W1716176296")]
	public async Task Get_TPhere_TxnId(bool deposit, long id, string expected)
	{
		var fakeTimeProvider = Substitute.For<TimeProvider>();
		var fakeSnowflakeWorkUtensil = Substitute.For<ISnowflakeWorkUtensil>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeBrandGameProviderRepository = Substitute.For<IBrandGameProviderRepository>();
		var fakeGameProviderConfigurationRepository = Substitute.For<IGameProviderConfigurationRepository>();

		_ = fakeSnowflakeWorkUtensil.GetId()
			.Returns(id);

		var sut = new GameProviderTransactionIdUtensil(
			fakeTimeProvider,
			fakeSnowflakeWorkUtensil,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeBrandGameProviderRepository,
			fakeGameProviderConfigurationRepository);

		var actual = await sut.GetTPhereTxnIdAsync(deposit);

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("018f9427-13ae-0000-0001-07e8f58f2e59", "018f942713ae0000000107e8f58f2e59")]
	public async Task Get_TxnId(string txnId, string expected)
	{
		var fakeTimeProvider = Substitute.For<TimeProvider>();
		var fakeSnowflakeWorkUtensil = Substitute.For<ISnowflakeWorkUtensil>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeBrandGameProviderRepository = Substitute.For<IBrandGameProviderRepository>();
		var fakeGameProviderConfigurationRepository = Substitute.For<IGameProviderConfigurationRepository>();

		var sut = new GameProviderTransactionIdUtensil(
			fakeTimeProvider,
			fakeSnowflakeWorkUtensil,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeBrandGameProviderRepository,
			fakeGameProviderConfigurationRepository);

		var actual = await sut.GetTxnIdAsync(Guid.Parse(txnId));

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("4ucmqplxb029", "2024-05-20T06:07:05", "71033", "71033202405200607050004ucmqplxb029")]
	public async Task Get_V8Poker_TxnId(string playerCode, string utcNow, string agentId, string expected)
	{
		var fakeTimeProvider = Substitute.For<TimeProvider>();
		var fakeSnowflakeWorkUtensil = Substitute.For<ISnowflakeWorkUtensil>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeBrandGameProviderRepository = Substitute.For<IBrandGameProviderRepository>();
		var fakeGameProviderConfigurationRepository = Substitute.For<IGameProviderConfigurationRepository>();

		_ = fakeTimeProvider.GetUtcNow()
			.Returns(DateTimeOffset.Parse(utcNow));

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.V8Poker,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(GameProviderType.V8Poker),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var playerInfo = new Models.PlayerInfo(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: playerCode,
			Username: playerCode,
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CMY");
		_ = fakePlayerRepository.GetAsync(
			playerCode: Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		var gameProviderConfigurationId = Guid.NewGuid();
		_ = fakeBrandGameProviderRepository.GetGameProviderConfigurationIdAsync(
			brandId: Arg.Is(playerInfo.BrandId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderConfigurationId);
		var jsonObject = new JsonObject
		{
			["AgentId"] = agentId
		};
		_ = fakeGameProviderConfigurationRepository.GetConfigurationDataAsync(
			gameProviderConfigurationId: Arg.Is(gameProviderConfigurationId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(jsonObject);

		var sut = new GameProviderTransactionIdUtensil(
			fakeTimeProvider,
			fakeSnowflakeWorkUtensil,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeBrandGameProviderRepository,
			fakeGameProviderConfigurationRepository);

		var actual = await sut.GetV8PokerTxnIdAsync(playerCode);

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData(1716176296)]
	public async Task Get_YeeBet_TxnId(long id)
	{
		var fakeTimeProvider = Substitute.For<TimeProvider>();
		var fakeSnowflakeWorkUtensil = Substitute.For<ISnowflakeWorkUtensil>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeBrandGameProviderRepository = Substitute.For<IBrandGameProviderRepository>();
		var fakeGameProviderConfigurationRepository = Substitute.For<IGameProviderConfigurationRepository>();

		_ = fakeSnowflakeWorkUtensil.GetId()
			.Returns(id);

		var sut = new GameProviderTransactionIdUtensil(
			fakeTimeProvider,
			fakeSnowflakeWorkUtensil,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeBrandGameProviderRepository,
			fakeGameProviderConfigurationRepository);

		var actual = await sut.GetYeeBetTxnIdAsync();

		Assert.Equal(id, actual);
	}
}
