using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.GameFund.Client;
using Tgs.GameFund.Common.Enums;
using Tgs.GameFund.Common.ViewModels;
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
		var fakeClient = Substitute.For<IGameFundClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.AWC_PGS,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var fundTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransactionId);

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

		var gameTransactionId = fundTransactionId.ToString("N");
		_ = fakeGameProviderTransactionIdUtensil.GetTxnIdAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundTransactionId),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameTransactionId);

		var gameFundTransferInfo = new GameFundTransferInfoViewModel(
			Id: fundTransactionId,
			GameProviderId: gameProviderInfo.GameProviderId,
			WalletId: playerInfo.WalletId,
			PlayerCode: playerInfo.PlayerCode,
			WalletFrom: WalletDef.MainWallet,
			WalletTo: gameProviderInfo.GameProviderCode,
			Source: GameFundTransferSource.FundOut,
			Status: GameFundTransferStatus.Processing,
			CashTransactionId: fundTransactionId.ToString(),
			CashRespTransactionId: null,
			GameTransactionId: gameTransactionId,
			Amount: query.Amount,
			CreatedOn: DateTimeOffset.UtcNow,
			UpdatedOn: DateTimeOffset.UtcNow);
		_ = fakeClient.GameFundTransferAddFundOutAsync(
			id: Arg.Is(fundTransactionId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			walletFrom: Arg.Is(WalletDef.MainWallet),
			walletTo: Arg.Is(gameProviderInfo.GameProviderCode),
			cashTransactionId: Arg.Is(fundTransactionId.ToString()),
			gameTransactionId: Arg.Is(gameTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameFundTransferInfo);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeClient,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(fundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundTransactionId.ToString(), actual.CashTransactionId);
		Assert.Equal(gameTransactionId.ToString(), actual.GameTransactionId);

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferBalanceCacheRequest
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
		var fakeClient = Substitute.For<IGameFundClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.Cards365Transfer,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var fundTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransactionId);

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

		var gameTransactionId = "D1716176296";
		_ = fakeGameProviderTransactionIdUtensil.GetCards365TxnIdAsync(
			Deposit: Arg.Is(true),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameTransactionId);

		var gameFundTransferInfo = new GameFundTransferInfoViewModel(
			Id: fundTransactionId,
			GameProviderId: gameProviderInfo.GameProviderId,
			WalletId: playerInfo.WalletId,
			PlayerCode: playerInfo.PlayerCode,
			WalletFrom: WalletDef.MainWallet,
			WalletTo: gameProviderInfo.GameProviderCode,
			Source: GameFundTransferSource.FundOut,
			Status: GameFundTransferStatus.Processing,
			CashTransactionId: fundTransactionId.ToString(),
			CashRespTransactionId: null,
			GameTransactionId: gameTransactionId,
			Amount: query.Amount,
			CreatedOn: DateTimeOffset.UtcNow,
			UpdatedOn: DateTimeOffset.UtcNow);
		_ = fakeClient.GameFundTransferAddFundOutAsync(
			id: Arg.Is(fundTransactionId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			walletFrom: Arg.Is(WalletDef.MainWallet),
			walletTo: Arg.Is(gameProviderInfo.GameProviderCode),
			cashTransactionId: Arg.Is(fundTransactionId.ToString()),
			gameTransactionId: Arg.Is(gameTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameFundTransferInfo);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeClient,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(fundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundTransactionId.ToString(), actual.CashTransactionId);
		Assert.Equal(gameTransactionId.ToString(), actual.GameTransactionId);

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferBalanceCacheRequest
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
		var fakeClient = Substitute.For<IGameFundClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.FaChai,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var fundTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransactionId);

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

		var gameTransactionId = "D1716176296";
		_ = fakeGameProviderTransactionIdUtensil.GetFaChaiTxnIdAsync(
			Deposit: Arg.Is(true),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameTransactionId);

		var gameFundTransferInfo = new GameFundTransferInfoViewModel(
			Id: fundTransactionId,
			GameProviderId: gameProviderInfo.GameProviderId,
			WalletId: playerInfo.WalletId,
			PlayerCode: playerInfo.PlayerCode,
			WalletFrom: WalletDef.MainWallet,
			WalletTo: gameProviderInfo.GameProviderCode,
			Source: GameFundTransferSource.FundOut,
			Status: GameFundTransferStatus.Processing,
			CashTransactionId: fundTransactionId.ToString(),
			CashRespTransactionId: null,
			GameTransactionId: gameTransactionId,
			Amount: query.Amount,
			CreatedOn: DateTimeOffset.UtcNow,
			UpdatedOn: DateTimeOffset.UtcNow);
		_ = fakeClient.GameFundTransferAddFundOutAsync(
			id: Arg.Is(fundTransactionId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			walletFrom: Arg.Is(WalletDef.MainWallet),
			walletTo: Arg.Is(gameProviderInfo.GameProviderCode),
			cashTransactionId: Arg.Is(fundTransactionId.ToString()),
			gameTransactionId: Arg.Is(gameTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameFundTransferInfo);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeClient,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(fundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundTransactionId.ToString(), actual.CashTransactionId);
		Assert.Equal(gameTransactionId.ToString(), actual.GameTransactionId);

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferBalanceCacheRequest
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
		var fakeClient = Substitute.For<IGameFundClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.GKXGaming,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var fundTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransactionId);

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

		var gameTransactionId = "60073202405200607050004ucmqplxb029";
		_ = fakeGameProviderTransactionIdUtensil.GetGKXGamingTxnIdAsync(
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameTransactionId);

		var gameFundTransferInfo = new GameFundTransferInfoViewModel(
			Id: fundTransactionId,
			GameProviderId: gameProviderInfo.GameProviderId,
			WalletId: playerInfo.WalletId,
			PlayerCode: playerInfo.PlayerCode,
			WalletFrom: WalletDef.MainWallet,
			WalletTo: gameProviderInfo.GameProviderCode,
			Source: GameFundTransferSource.FundOut,
			Status: GameFundTransferStatus.Processing,
			CashTransactionId: fundTransactionId.ToString(),
			CashRespTransactionId: null,
			GameTransactionId: gameTransactionId,
			Amount: query.Amount,
			CreatedOn: DateTimeOffset.UtcNow,
			UpdatedOn: DateTimeOffset.UtcNow);
		_ = fakeClient.GameFundTransferAddFundOutAsync(
			id: Arg.Is(fundTransactionId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			walletFrom: Arg.Is(WalletDef.MainWallet),
			walletTo: Arg.Is(gameProviderInfo.GameProviderCode),
			cashTransactionId: Arg.Is(fundTransactionId.ToString()),
			gameTransactionId: Arg.Is(gameTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameFundTransferInfo);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeClient,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(fundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundTransactionId.ToString(), actual.CashTransactionId);
		Assert.Equal(gameTransactionId.ToString(), actual.GameTransactionId);

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferBalanceCacheRequest
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
		var fakeClient = Substitute.For<IGameFundClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var fundTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransactionId);

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

		var gameTransactionId = "60073202405200607050004ucmqplxb029";
		_ = fakeGameProviderTransactionIdUtensil.GetKaiyuanTxnIdAsync(
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameTransactionId);

		var gameFundTransferInfo = new GameFundTransferInfoViewModel(
			Id: fundTransactionId,
			GameProviderId: gameProviderInfo.GameProviderId,
			WalletId: playerInfo.WalletId,
			PlayerCode: playerInfo.PlayerCode,
			WalletFrom: WalletDef.MainWallet,
			WalletTo: gameProviderInfo.GameProviderCode,
			Source: GameFundTransferSource.FundOut,
			Status: GameFundTransferStatus.Processing,
			CashTransactionId: fundTransactionId.ToString(),
			CashRespTransactionId: null,
			GameTransactionId: gameTransactionId,
			Amount: query.Amount,
			CreatedOn: DateTimeOffset.UtcNow,
			UpdatedOn: DateTimeOffset.UtcNow);
		_ = fakeClient.GameFundTransferAddFundOutAsync(
			id: Arg.Is(fundTransactionId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			walletFrom: Arg.Is(WalletDef.MainWallet),
			walletTo: Arg.Is(gameProviderInfo.GameProviderCode),
			cashTransactionId: Arg.Is(fundTransactionId.ToString()),
			gameTransactionId: Arg.Is(gameTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameFundTransferInfo);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeClient,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(fundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundTransactionId.ToString(), actual.CashTransactionId);
		Assert.Equal(gameTransactionId.ToString(), actual.GameTransactionId);

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferBalanceCacheRequest
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
		var fakeClient = Substitute.For<IGameFundClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.LEGaming,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var fundTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransactionId);

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

		var gameTransactionId = "60073202405200607050004ucmqplxb029";
		_ = fakeGameProviderTransactionIdUtensil.GetLEGamingTxnIdAsync(
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameTransactionId);

		var gameFundTransferInfo = new GameFundTransferInfoViewModel(
			Id: fundTransactionId,
			GameProviderId: gameProviderInfo.GameProviderId,
			WalletId: playerInfo.WalletId,
			PlayerCode: playerInfo.PlayerCode,
			WalletFrom: WalletDef.MainWallet,
			WalletTo: gameProviderInfo.GameProviderCode,
			Source: GameFundTransferSource.FundOut,
			Status: GameFundTransferStatus.Processing,
			CashTransactionId: fundTransactionId.ToString(),
			CashRespTransactionId: null,
			GameTransactionId: gameTransactionId,
			Amount: query.Amount,
			CreatedOn: DateTimeOffset.UtcNow,
			UpdatedOn: DateTimeOffset.UtcNow);
		_ = fakeClient.GameFundTransferAddFundOutAsync(
			id: Arg.Is(fundTransactionId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			walletFrom: Arg.Is(WalletDef.MainWallet),
			walletTo: Arg.Is(gameProviderInfo.GameProviderCode),
			cashTransactionId: Arg.Is(fundTransactionId.ToString()),
			gameTransactionId: Arg.Is(gameTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameFundTransferInfo);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeClient,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(fundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundTransactionId.ToString(), actual.CashTransactionId);
		Assert.Equal(gameTransactionId.ToString(), actual.GameTransactionId);

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferBalanceCacheRequest
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
		var fakeClient = Substitute.For<IGameFundClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.SBOTransfer,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var fundTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransactionId);

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

		var gameTransactionId = "D1716176296";
		_ = fakeGameProviderTransactionIdUtensil.GetSBOTxnIdAsync(
			Deposit: Arg.Is(true),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameTransactionId);

		var gameFundTransferInfo = new GameFundTransferInfoViewModel(
			Id: fundTransactionId,
			GameProviderId: gameProviderInfo.GameProviderId,
			WalletId: playerInfo.WalletId,
			PlayerCode: playerInfo.PlayerCode,
			WalletFrom: WalletDef.MainWallet,
			WalletTo: gameProviderInfo.GameProviderCode,
			Source: GameFundTransferSource.FundOut,
			Status: GameFundTransferStatus.Processing,
			CashTransactionId: fundTransactionId.ToString(),
			CashRespTransactionId: null,
			GameTransactionId: gameTransactionId,
			Amount: query.Amount,
			CreatedOn: DateTimeOffset.UtcNow,
			UpdatedOn: DateTimeOffset.UtcNow);
		_ = fakeClient.GameFundTransferAddFundOutAsync(
			id: Arg.Is(fundTransactionId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			walletFrom: Arg.Is(WalletDef.MainWallet),
			walletTo: Arg.Is(gameProviderInfo.GameProviderCode),
			cashTransactionId: Arg.Is(fundTransactionId.ToString()),
			gameTransactionId: Arg.Is(gameTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameFundTransferInfo);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeClient,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(fundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundTransactionId.ToString(), actual.CashTransactionId);
		Assert.Equal(gameTransactionId.ToString(), actual.GameTransactionId);

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferBalanceCacheRequest
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
		var fakeClient = Substitute.For<IGameFundClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.TPhere,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var fundTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransactionId);

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

		var gameTransactionId = "D1716176296";
		_ = fakeGameProviderTransactionIdUtensil.GetTPhereTxnIdAsync(
			Deposit: Arg.Is(true),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameTransactionId);

		var gameFundTransferInfo = new GameFundTransferInfoViewModel(
			Id: fundTransactionId,
			GameProviderId: gameProviderInfo.GameProviderId,
			WalletId: playerInfo.WalletId,
			PlayerCode: playerInfo.PlayerCode,
			WalletFrom: WalletDef.MainWallet,
			WalletTo: gameProviderInfo.GameProviderCode,
			Source: GameFundTransferSource.FundOut,
			Status: GameFundTransferStatus.Processing,
			CashTransactionId: fundTransactionId.ToString(),
			CashRespTransactionId: null,
			GameTransactionId: gameTransactionId,
			Amount: query.Amount,
			CreatedOn: DateTimeOffset.UtcNow,
			UpdatedOn: DateTimeOffset.UtcNow);
		_ = fakeClient.GameFundTransferAddFundOutAsync(
			id: Arg.Is(fundTransactionId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			walletFrom: Arg.Is(WalletDef.MainWallet),
			walletTo: Arg.Is(gameProviderInfo.GameProviderCode),
			cashTransactionId: Arg.Is(fundTransactionId.ToString()),
			gameTransactionId: Arg.Is(gameTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameFundTransferInfo);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeClient,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(fundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundTransactionId.ToString(), actual.CashTransactionId);
		Assert.Equal(gameTransactionId.ToString(), actual.GameTransactionId);

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferBalanceCacheRequest
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
		var fakeClient = Substitute.For<IGameFundClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.V8Poker,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var fundTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransactionId);

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

		var gameTransactionId = "60073202405200607050004ucmqplxb029";
		_ = fakeGameProviderTransactionIdUtensil.GetV8PokerTxnIdAsync(
			playerCode: Arg.Is(query.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameTransactionId);

		var gameFundTransferInfo = new GameFundTransferInfoViewModel(
			Id: fundTransactionId,
			GameProviderId: gameProviderInfo.GameProviderId,
			WalletId: playerInfo.WalletId,
			PlayerCode: playerInfo.PlayerCode,
			WalletFrom: WalletDef.MainWallet,
			WalletTo: gameProviderInfo.GameProviderCode,
			Source: GameFundTransferSource.FundOut,
			Status: GameFundTransferStatus.Processing,
			CashTransactionId: fundTransactionId.ToString(),
			CashRespTransactionId: null,
			GameTransactionId: gameTransactionId,
			Amount: query.Amount,
			CreatedOn: DateTimeOffset.UtcNow,
			UpdatedOn: DateTimeOffset.UtcNow);
		_ = fakeClient.GameFundTransferAddFundOutAsync(
			id: Arg.Is(fundTransactionId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			walletFrom: Arg.Is(WalletDef.MainWallet),
			walletTo: Arg.Is(gameProviderInfo.GameProviderCode),
			cashTransactionId: Arg.Is(fundTransactionId.ToString()),
			gameTransactionId: Arg.Is(gameTransactionId),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameFundTransferInfo);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeClient,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(fundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundTransactionId.ToString(), actual.CashTransactionId);
		Assert.Equal(gameTransactionId.ToString(), actual.GameTransactionId);

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferBalanceCacheRequest
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
			Arg.Is(new Common.Protos.GameFundTransferBalanceCacheRequest
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
		var fakeClient = Substitute.For<IGameFundClient>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var query = new FundProcessingFundOutAddQuery(
			GameProviderCode: GameProviderType.YeeBet,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

		var fundTransactionId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransactionId);

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

		var gameTransactionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		_ = fakeGameProviderTransactionIdUtensil.GetYeeBetTxnIdAsync(
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameTransactionId);

		var gameFundTransferInfo = new GameFundTransferInfoViewModel(
			Id: fundTransactionId,
			GameProviderId: gameProviderInfo.GameProviderId,
			WalletId: playerInfo.WalletId,
			PlayerCode: playerInfo.PlayerCode,
			WalletFrom: WalletDef.MainWallet,
			WalletTo: gameProviderInfo.GameProviderCode,
			Source: GameFundTransferSource.FundOut,
			Status: GameFundTransferStatus.Processing,
			CashTransactionId: fundTransactionId.ToString(),
			CashRespTransactionId: null,
			GameTransactionId: gameTransactionId.ToString(),
			Amount: query.Amount,
			CreatedOn: DateTimeOffset.UtcNow,
			UpdatedOn: DateTimeOffset.UtcNow);
		_ = fakeClient.GameFundTransferAddFundOutAsync(
			id: Arg.Is(fundTransactionId),
			gameProviderId: Arg.Is(gameProviderInfo.GameProviderId),
			walletId: Arg.Is(playerInfo.WalletId),
			playerCode: Arg.Is(playerInfo.PlayerCode),
			walletFrom: Arg.Is(WalletDef.MainWallet),
			walletTo: Arg.Is(gameProviderInfo.GameProviderCode),
			cashTransactionId: Arg.Is(fundTransactionId.ToString()),
			gameTransactionId: Arg.Is(gameTransactionId.ToString()),
			amount: Arg.Is(query.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameFundTransferInfo);

		var sut = new FundProcessingFundOutAddGetService(
			faceLogger,
			fakeSerialNumberGenerator,
			fakePlayerRepository,
			fakeGameProviderRepository,
			fakeGameProviderTransactionIdUtensil,
			fakeClient,
			fakeMessageSender);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(playerInfo.BrandId, actual.BrandId);
		Assert.Equal(playerInfo.BrandCode, actual.BrandCode);
		Assert.Equal(playerInfo.PlayerCode, actual.PlayerCode);
		Assert.Equal(playerInfo.Username, actual.Username);
		Assert.Equal(gameProviderInfo.GameProviderId, actual.GameProviderId);
		Assert.Equal(gameProviderInfo.GameProviderCode, actual.GameProviderCode);
		Assert.Equal(fundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundTransactionId.ToString(), actual.CashTransactionId);
		Assert.Equal(gameTransactionId.ToString(), actual.GameTransactionId);

		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
			Arg.Is(Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest),
			Arg.Is(new Common.Protos.GameFundTransferBalanceCacheRequest
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
