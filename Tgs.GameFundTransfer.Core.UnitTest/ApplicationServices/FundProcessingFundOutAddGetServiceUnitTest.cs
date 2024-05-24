using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.GameFundTransfer.Common.Enums;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Domains;
using Tgs.SerialNumberGenerator;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;

public class FundProcessingFundOutAddGetServiceUnitTest
{
	[Fact]
	public async Task Normal_Process()
	{
		var faceLogger = NullLogger<FundProcessingFundOutAddGetService>.Instance;
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGameProviderTransactionIdUtensil = Substitute.For<IGameProviderTransactionIdUtensil>();
		var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.AWC_PGS,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundTransferTransactionId);

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

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var gameProviderTransactionId = gameProviderFundTransferTransactionId.ToString("N");
		_ = fakeGameProviderTransactionIdUtensil.GetTxnIdAsync(
			gameProviderFundTransferTransactionId: Arg.Is(gameProviderFundTransferTransactionId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderTransactionId);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeFundTransferTransactionRepository,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(gameProviderFundTransferTransactionId, actual.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderFundTransferTransactionId.ToString(), actual.ExternalTransactionId);
		Assert.Equal(gameProviderTransactionId, actual.GameProviderTransactionId);

		_ = fakeFundTransferTransactionRepository
			.Received(1)
			.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			fundSource: Arg.Is(FundSource.FundOut),
			fromWallet: Arg.Is(WalletDef.MainWallet),
			toWallet: Arg.Is(gameProviderInfo.GameProviderCode),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Cards365Transfer()
	{
		var faceLogger = NullLogger<FundProcessingFundOutAddGetService>.Instance;
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGameProviderTransactionIdUtensil = Substitute.For<IGameProviderTransactionIdUtensil>();
		var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.Cards365Transfer,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundTransferTransactionId);

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

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var gameProviderTransactionId = "D1716176296";
		_ = fakeGameProviderTransactionIdUtensil.GetCards365TxnIdAsync(
			Deposit: Arg.Is(true),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderTransactionId);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeFundTransferTransactionRepository,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(gameProviderFundTransferTransactionId, actual.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderFundTransferTransactionId.ToString(), actual.ExternalTransactionId);
		Assert.Equal(gameProviderTransactionId, actual.GameProviderTransactionId);

		_ = fakeFundTransferTransactionRepository
			.Received(1)
			.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			fundSource: Arg.Is(FundSource.FundOut),
			fromWallet: Arg.Is(WalletDef.MainWallet),
			toWallet: Arg.Is(gameProviderInfo.GameProviderCode),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task FaChai()
	{
		var faceLogger = NullLogger<FundProcessingFundOutAddGetService>.Instance;
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGameProviderTransactionIdUtensil = Substitute.For<IGameProviderTransactionIdUtensil>();
		var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.FaChai,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundTransferTransactionId);

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

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var gameProviderTransactionId = "D1716176296";
		_ = fakeGameProviderTransactionIdUtensil.GetFaChaiTxnIdAsync(
			Deposit: Arg.Is(true),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderTransactionId);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeFundTransferTransactionRepository,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(gameProviderFundTransferTransactionId, actual.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderFundTransferTransactionId.ToString(), actual.ExternalTransactionId);
		Assert.Equal(gameProviderTransactionId, actual.GameProviderTransactionId);

		_ = fakeFundTransferTransactionRepository
			.Received(1)
			.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			fundSource: Arg.Is(FundSource.FundOut),
			fromWallet: Arg.Is(WalletDef.MainWallet),
			toWallet: Arg.Is(gameProviderInfo.GameProviderCode),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GKXGaming()
	{
		var faceLogger = NullLogger<FundProcessingFundOutAddGetService>.Instance;
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGameProviderTransactionIdUtensil = Substitute.For<IGameProviderTransactionIdUtensil>();
		var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.GKXGaming,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundTransferTransactionId);

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

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var gameProviderTransactionId = "60073202405200607050004ucmqplxb029";
		_ = fakeGameProviderTransactionIdUtensil.GetGKXGamingTxnIdAsync(
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderTransactionId);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeFundTransferTransactionRepository,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(gameProviderFundTransferTransactionId, actual.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderFundTransferTransactionId.ToString(), actual.ExternalTransactionId);
		Assert.Equal(gameProviderTransactionId, actual.GameProviderTransactionId);

		_ = fakeFundTransferTransactionRepository
			.Received(1)
			.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			fundSource: Arg.Is(FundSource.FundOut),
			fromWallet: Arg.Is(WalletDef.MainWallet),
			toWallet: Arg.Is(gameProviderInfo.GameProviderCode),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Kaiyuan()
	{
		var faceLogger = NullLogger<FundProcessingFundOutAddGetService>.Instance;
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGameProviderTransactionIdUtensil = Substitute.For<IGameProviderTransactionIdUtensil>();
		var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundTransferTransactionId);

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

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var gameProviderTransactionId = "60073202405200607050004ucmqplxb029";
		_ = fakeGameProviderTransactionIdUtensil.GetKaiyuanTxnIdAsync(
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderTransactionId);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeFundTransferTransactionRepository,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(gameProviderFundTransferTransactionId, actual.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderFundTransferTransactionId.ToString(), actual.ExternalTransactionId);
		Assert.Equal(gameProviderTransactionId, actual.GameProviderTransactionId);

		_ = fakeFundTransferTransactionRepository
			.Received(1)
			.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			fundSource: Arg.Is(FundSource.FundOut),
			fromWallet: Arg.Is(WalletDef.MainWallet),
			toWallet: Arg.Is(gameProviderInfo.GameProviderCode),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task LEGaming()
	{
		var faceLogger = NullLogger<FundProcessingFundOutAddGetService>.Instance;
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGameProviderTransactionIdUtensil = Substitute.For<IGameProviderTransactionIdUtensil>();
		var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.LEGaming,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundTransferTransactionId);

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

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var gameProviderTransactionId = "60073202405200607050004ucmqplxb029";
		_ = fakeGameProviderTransactionIdUtensil.GetLEGamingTxnIdAsync(
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderTransactionId);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeFundTransferTransactionRepository,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(gameProviderFundTransferTransactionId, actual.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderFundTransferTransactionId.ToString(), actual.ExternalTransactionId);
		Assert.Equal(gameProviderTransactionId, actual.GameProviderTransactionId);

		_ = fakeFundTransferTransactionRepository
			.Received(1)
			.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			fundSource: Arg.Is(FundSource.FundOut),
			fromWallet: Arg.Is(WalletDef.MainWallet),
			toWallet: Arg.Is(gameProviderInfo.GameProviderCode),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task SBOTransfer()
	{
		var faceLogger = NullLogger<FundProcessingFundOutAddGetService>.Instance;
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGameProviderTransactionIdUtensil = Substitute.For<IGameProviderTransactionIdUtensil>();
		var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.SBOTransfer,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundTransferTransactionId);

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

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var gameProviderTransactionId = "D1716176296";
		_ = fakeGameProviderTransactionIdUtensil.GetSBOTxnIdAsync(
			Deposit: Arg.Is(true),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderTransactionId);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeFundTransferTransactionRepository,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(gameProviderFundTransferTransactionId, actual.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderFundTransferTransactionId.ToString(), actual.ExternalTransactionId);
		Assert.Equal(gameProviderTransactionId, actual.GameProviderTransactionId);

		_ = fakeFundTransferTransactionRepository
			.Received(1)
			.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			fundSource: Arg.Is(FundSource.FundOut),
			fromWallet: Arg.Is(WalletDef.MainWallet),
			toWallet: Arg.Is(gameProviderInfo.GameProviderCode),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task TPhere()
	{
		var faceLogger = NullLogger<FundProcessingFundOutAddGetService>.Instance;
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGameProviderTransactionIdUtensil = Substitute.For<IGameProviderTransactionIdUtensil>();
		var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.TPhere,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundTransferTransactionId);

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

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var gameProviderTransactionId = "D1716176296";
		_ = fakeGameProviderTransactionIdUtensil.GetTPhereTxnIdAsync(
			Deposit: Arg.Is(true),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderTransactionId);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeFundTransferTransactionRepository,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(gameProviderFundTransferTransactionId, actual.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderFundTransferTransactionId.ToString(), actual.ExternalTransactionId);
		Assert.Equal(gameProviderTransactionId, actual.GameProviderTransactionId);

		_ = fakeFundTransferTransactionRepository
			.Received(1)
			.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			fundSource: Arg.Is(FundSource.FundOut),
			fromWallet: Arg.Is(WalletDef.MainWallet),
			toWallet: Arg.Is(gameProviderInfo.GameProviderCode),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task V8Poker()
	{
		var faceLogger = NullLogger<FundProcessingFundOutAddGetService>.Instance;
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGameProviderTransactionIdUtensil = Substitute.For<IGameProviderTransactionIdUtensil>();
		var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.V8Poker,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundTransferTransactionId);

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

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var gameProviderTransactionId = "60073202405200607050004ucmqplxb029";
		_ = fakeGameProviderTransactionIdUtensil.GetV8PokerTxnIdAsync(
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderTransactionId);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeFundTransferTransactionRepository,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(gameProviderFundTransferTransactionId, actual.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderFundTransferTransactionId.ToString(), actual.ExternalTransactionId);
		Assert.Equal(gameProviderTransactionId, actual.GameProviderTransactionId);

		_ = fakeFundTransferTransactionRepository
			.Received(1)
			.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			fundSource: Arg.Is(FundSource.FundOut),
			fromWallet: Arg.Is(WalletDef.MainWallet),
			toWallet: Arg.Is(gameProviderInfo.GameProviderCode),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			}),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task YeeBet()
	{
		var faceLogger = NullLogger<FundProcessingFundOutAddGetService>.Instance;
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGameProviderTransactionIdUtensil = Substitute.For<IGameProviderTransactionIdUtensil>();
		var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.YeeBet,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundTransferTransactionId);

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

		var gameProviderInfo = new Models.GameProviderInfo(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			TransferWallet: true);
		_ = fakeGameProviderRepository.GetAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderInfo);

		var gameProviderTransactionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		_ = fakeGameProviderTransactionIdUtensil.GetYeeBetTxnIdAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderTransactionId);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeFundTransferTransactionRepository,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(gameProviderFundTransferTransactionId, actual.GameProviderFundTransferTransactionId);
		Assert.Equal(gameProviderFundTransferTransactionId.ToString(), actual.ExternalTransactionId);
		Assert.Equal(gameProviderTransactionId.ToString(), actual.GameProviderTransactionId);

		_ = fakeFundTransferTransactionRepository
			.Received(1)
			.AddProcessingAsync(
			gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			gameProviderTransactionId: Arg.Is(gameProviderTransactionId.ToString()),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			fundSource: Arg.Is(FundSource.FundOut),
			fromWallet: Arg.Is(WalletDef.MainWallet),
			toWallet: Arg.Is(gameProviderInfo.GameProviderCode),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			}),
			cancellationToken: Arg.Any<CancellationToken>());
	}
}
