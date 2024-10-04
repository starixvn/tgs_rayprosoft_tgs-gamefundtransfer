using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFund.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.Core.Exceptions;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;

public class FundTransferInOneClickGetServiceUnitTest
{
	[Fact]
	public async Task Fund_In()
	{
		var faceLogger = NullLogger<FundTransferInGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeClient = Substitute.For<IGameFundClient>();

		var query = new FundTransferInOneClickQuery(
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

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 100.0m,
			FreeBalance: 10.0m,
			OutstandingBalance: 90m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var convertPlatformBalanceResult = new GameCurrency.Common.ConvertPlatformBalanceResult(
			ExchangeRate: 1,
			Balance: gameProviderBalance.Balance,
			FreeBalance: gameProviderBalance.FreeBalance,
			OutstandingBalance: gameProviderBalance.OutstandingBalance);
		_ = fakeGameCurrencyClient.ConvertPlatformBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(playerInfo.CurrencyCode),
			balance: Arg.Is(gameProviderBalance.Balance),
			freeBalance: Arg.Is(gameProviderBalance.FreeBalance),
			outstandingBalance: Arg.Is(gameProviderBalance.OutstandingBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(convertPlatformBalanceResult);

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			FundTransactionId: Guid.NewGuid(),
			CashTransactionId: fundTransactionId.ToString("N"),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: convertPlatformBalanceResult.FreeBalance)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var gameProviderFundInResult = new GameProviderFundInResult(
			GameTransactionId: fundProcessingResult.GameTransactionId,
			GameRespTransactionId: Guid.NewGuid().ToString("N"));
		_ = fakeGameProviderFundInService.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderId: fundProcessingResult.GameProviderId,
				GameProviderCode: fundProcessingResult.GameProviderCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				WalletId: playerInfo.WalletId,
				GameTransactionId: fundProcessingResult.GameTransactionId,
				Amount: gameProviderBalance.FreeBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundInResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			walletId: Arg.Is(playerInfo.WalletId),
			amount: Arg.Is(convertPlatformBalanceResult.FreeBalance),
			description: Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			externalId: Arg.Is(fundProcessingResult.CashTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundTransferInOneClickGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeClient);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(fundProcessingResult.FundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundProcessingResult.CashTransactionId, actual.CashTransactionId);
		Assert.Equal(transactionResponse.TransactionId, actual.CashRespTransactionId);
		Assert.Equal(fundProcessingResult.GameTransactionId, actual.GameTransactionId);
		Assert.Equal(gameProviderFundInResult.GameRespTransactionId, actual.GameRespTransactionId);

		_ = fakeFundsDistributedLock
			.Received(1)
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

		_ = fakeClient
			.Received(1)
			.GameFundTransferUpdateBySuccessAsync(
			id: Arg.Is(fundProcessingResult.FundTransactionId),
			cashRespTransactionId: Arg.Is(transactionResponse.TransactionId),
			gameRespTransactionId: Arg.Is(gameProviderFundInResult.GameRespTransactionId),
			mainWalletBalance: Arg.Is(transactionResponse.Balance),
			description: Arg.Any<string>(),
			additionalData: Arg.Any<JsonNode>(),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Fund_Transfer_Lock_Failed()
	{
		var faceLogger = NullLogger<FundTransferInGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeClient = Substitute.For<IGameFundClient>();

		var query = new FundTransferInOneClickQuery(
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

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromException(new FundTransferException(query.GameProviderCode, query.PlayerCode)));

		var sut = new FundTransferInOneClickGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeClient);

		_ = await Assert.ThrowsAsync<FundTransferException>(() => sut.GetAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_1000_FundIn_Game_Provider_IDR_Amount_1()
	{
		var faceLogger = NullLogger<FundTransferInGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeClient = Substitute.For<IGameFundClient>();

		var query = new FundTransferInOneClickQuery(
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

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1000.0m,
			FreeBalance: 100.0m,
			OutstandingBalance: 900m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var convertPlatformBalanceResult = new GameCurrency.Common.ConvertPlatformBalanceResult(
			ExchangeRate: 0.001m,
			Balance: 1,
			FreeBalance: 0.1m,
			OutstandingBalance: 0.9m);
		_ = fakeGameCurrencyClient.ConvertPlatformBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(playerInfo.CurrencyCode),
			balance: Arg.Is(gameProviderBalance.Balance),
			freeBalance: Arg.Is(gameProviderBalance.FreeBalance),
			outstandingBalance: Arg.Is(gameProviderBalance.OutstandingBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(convertPlatformBalanceResult);

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			FundTransactionId: Guid.NewGuid(),
			CashTransactionId: fundTransactionId.ToString("N"),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: convertPlatformBalanceResult.FreeBalance)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var gameProviderFundInResult = new GameProviderFundInResult(
			GameTransactionId: fundProcessingResult.GameTransactionId,
			GameRespTransactionId: Guid.NewGuid().ToString("N"));
		_ = fakeGameProviderFundInService.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderId: fundProcessingResult.GameProviderId,
				GameProviderCode: fundProcessingResult.GameProviderCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				WalletId: playerInfo.WalletId,
				GameTransactionId: fundProcessingResult.GameTransactionId,
				Amount: gameProviderBalance.FreeBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundInResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1000
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			walletId: Arg.Is(playerInfo.WalletId),
			amount: Arg.Is(convertPlatformBalanceResult.FreeBalance),
			description: Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			externalId: Arg.Is(fundProcessingResult.CashTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundTransferInOneClickGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeClient);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(fundProcessingResult.FundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundProcessingResult.CashTransactionId, actual.CashTransactionId);
		Assert.Equal(transactionResponse.TransactionId, actual.CashRespTransactionId);
		Assert.Equal(fundProcessingResult.GameTransactionId, actual.GameTransactionId);
		Assert.Equal(gameProviderFundInResult.GameRespTransactionId, actual.GameRespTransactionId);

		_ = fakeFundsDistributedLock
			.Received(1)
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

		_ = fakeClient
			.Received(1)
			.GameFundTransferUpdateBySuccessAsync(
			id: Arg.Is(fundProcessingResult.FundTransactionId),
			cashRespTransactionId: Arg.Is(transactionResponse.TransactionId),
			gameRespTransactionId: Arg.Is(gameProviderFundInResult.GameRespTransactionId),
			mainWalletBalance: Arg.Is(transactionResponse.Balance),
			description: Arg.Any<string>(),
			additionalData: Arg.Any<JsonNode>(),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_FundIn_Game_Provider_IDR_1000_Amount_1()
	{
		var faceLogger = NullLogger<FundTransferInGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeClient = Substitute.For<IGameFundClient>();

		var query = new FundTransferInOneClickQuery(
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

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 10.0m,
			FreeBalance: 1.0m,
			OutstandingBalance: 9.0m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var convertPlatformBalanceResult = new GameCurrency.Common.ConvertPlatformBalanceResult(
			ExchangeRate: 1000m,
			Balance: 10000,
			FreeBalance: 100m,
			OutstandingBalance: 900m);
		_ = fakeGameCurrencyClient.ConvertPlatformBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(playerInfo.CurrencyCode),
			balance: Arg.Is(gameProviderBalance.Balance),
			freeBalance: Arg.Is(gameProviderBalance.FreeBalance),
			outstandingBalance: Arg.Is(gameProviderBalance.OutstandingBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(convertPlatformBalanceResult);

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			FundTransactionId: fundTransactionId,
			CashTransactionId: fundTransactionId.ToString("N"),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: convertPlatformBalanceResult.FreeBalance)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var gameProviderFundInResult = new GameProviderFundInResult(
			GameTransactionId: fundProcessingResult.GameTransactionId,
			GameRespTransactionId: Guid.NewGuid().ToString("N"));
		_ = fakeGameProviderFundInService.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderId: fundProcessingResult.GameProviderId,
				GameProviderCode: fundProcessingResult.GameProviderCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				WalletId: playerInfo.WalletId,
				GameTransactionId: fundProcessingResult.GameTransactionId,
				Amount: gameProviderBalance.FreeBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundInResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1000
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			walletId: Arg.Is(playerInfo.WalletId),
			amount: Arg.Is(convertPlatformBalanceResult.FreeBalance),
			description: Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			externalId: Arg.Is(fundProcessingResult.CashTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundTransferInOneClickGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeClient);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(fundProcessingResult.FundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundProcessingResult.CashTransactionId, actual.CashTransactionId);
		Assert.Equal(transactionResponse.TransactionId, actual.CashRespTransactionId);
		Assert.Equal(fundProcessingResult.GameTransactionId, actual.GameTransactionId);
		Assert.Equal(gameProviderFundInResult.GameRespTransactionId, actual.GameRespTransactionId);

		_ = fakeFundsDistributedLock
			.Received(1)
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

		_ = fakeClient
			.Received(1)
			.GameFundTransferUpdateBySuccessAsync(
			id: Arg.Is(fundProcessingResult.FundTransactionId),
			cashRespTransactionId: Arg.Is(transactionResponse.TransactionId),
			gameRespTransactionId: Arg.Is(gameProviderFundInResult.GameRespTransactionId),
			mainWalletBalance: Arg.Is(transactionResponse.Balance),
			description: Arg.Any<string>(),
			additionalData: Arg.Any<JsonNode>(),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task This_Player_Has_Acquired_A_Game_Provider_Less_Than_Or_Equal_To_0()
	{
		var faceLogger = NullLogger<FundTransferInGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeClient = Substitute.For<IGameFundClient>();

		var query = new FundTransferInOneClickQuery(
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

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 10m,
			FreeBalance: 0m,
			OutstandingBalance: 10m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var convertPlatformBalanceResult = new GameCurrency.Common.ConvertPlatformBalanceResult(
			ExchangeRate: 1,
			Balance: 10.0m,
			FreeBalance: 0m,
			OutstandingBalance: 10.0m);
		_ = fakeGameCurrencyClient.ConvertPlatformBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(playerInfo.CurrencyCode),
			balance: Arg.Is(gameProviderBalance.Balance),
			freeBalance: Arg.Is(gameProviderBalance.FreeBalance),
			outstandingBalance: Arg.Is(gameProviderBalance.OutstandingBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(convertPlatformBalanceResult);

		var sut = new FundTransferInOneClickGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeClient);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.GetAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(1)
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

		_ = fakeService
			.Received(0)
			.GetAsync(
			query: Arg.Any<FundProcessingFundInAddQuery>(),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}
}
