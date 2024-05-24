using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;

public class FundInZeroGetServiceUnitTest
{
	[Fact]
	public async Task Fund_In()
	{
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInZeroQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY");

		_ = fakeFundsDistributedLock.LockingAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 100.0m,
			FreeBalance: 10.0m,
			OutstandingBalance: 90m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var convertPlatformBalanceResult = new ConvertPlatformBalanceResult(
			ExchangeRate: 1,
			Balance: gameProviderBalance.Balance,
			FreeBalance: gameProviderBalance.FreeBalance,
			OutstandingBalance: gameProviderBalance.OutstandingBalance);
		_ = fakeGameCurrencyClient.ConvertPlatformBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			balance: Arg.Is(gameProviderBalance.Balance),
			freeBalance: Arg.Is(gameProviderBalance.FreeBalance),
			outstandingBalance: Arg.Is(gameProviderBalance.OutstandingBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(convertPlatformBalanceResult);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString("N"),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: convertPlatformBalanceResult.FreeBalance)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(convertPlatformBalanceResult.Balance),
			description: Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			externalId: Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundInZeroGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeRepository);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(StateCode.Success, actual.State);

		_ = fakeGameProviderFundInService
			.Received(1)
			.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				BrandCode: fundProcessingResult.BrandCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				CurrencyCode: query.CurrencyCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: gameProviderBalance.FreeBalance),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeRepository
			.Received(1)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			transactionId: Arg.Is(transactionResponse.TransactionId),
			balance: Arg.Is(transactionResponse.Balance),
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
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInZeroQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY");

		_ = fakeFundsDistributedLock.LockingAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(false));

		var sut = new FundInZeroGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeRepository);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(StateCode.TransactionLock, actual.State);

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_1000_FundIn_Game_Provider_IDR_Amount_1()
	{
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInZeroQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "IDR_1000");

		_ = fakeFundsDistributedLock.LockingAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1000.0m,
			FreeBalance: 100.0m,
			OutstandingBalance: 900m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var convertPlatformBalanceResult = new ConvertPlatformBalanceResult(
			ExchangeRate: 0.001m,
			Balance: 1,
			FreeBalance: 0.1m,
			OutstandingBalance: 0.9m);
		_ = fakeGameCurrencyClient.ConvertPlatformBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			balance: Arg.Is(gameProviderBalance.Balance),
			freeBalance: Arg.Is(gameProviderBalance.FreeBalance),
			outstandingBalance: Arg.Is(gameProviderBalance.OutstandingBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(convertPlatformBalanceResult);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString("N"),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: convertPlatformBalanceResult.FreeBalance)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1000
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(convertPlatformBalanceResult.Balance),
			description: Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			externalId: Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundInZeroGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeRepository);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(StateCode.Success, actual.State);

		_ = fakeGameProviderFundInService
			.Received(1)
			.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				BrandCode: fundProcessingResult.BrandCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				CurrencyCode: query.CurrencyCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: gameProviderBalance.FreeBalance),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeRepository
			.Received(1)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			transactionId: Arg.Is(transactionResponse.TransactionId),
			balance: Arg.Is(transactionResponse.Balance),
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
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInZeroQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "IDR");

		_ = fakeFundsDistributedLock
			.LockingAsync(
				Arg.Is(query.GameProviderCode),
				Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 10.0m,
			FreeBalance: 1.0m,
			OutstandingBalance: 9.0m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var convertPlatformBalanceResult = new ConvertPlatformBalanceResult(
			ExchangeRate: 1000m,
			Balance: 10000,
			FreeBalance: 100m,
			OutstandingBalance: 900m);
		_ = fakeGameCurrencyClient.ConvertPlatformBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			balance: Arg.Is(gameProviderBalance.Balance),
			freeBalance: Arg.Is(gameProviderBalance.FreeBalance),
			outstandingBalance: Arg.Is(gameProviderBalance.OutstandingBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(convertPlatformBalanceResult);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString("N"),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: convertPlatformBalanceResult.FreeBalance)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1000
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(convertPlatformBalanceResult.Balance),
			description: Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			externalId: Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundInZeroGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeRepository);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(StateCode.Success, actual.State);

		_ = fakeGameProviderFundInService
			.Received(1)
			.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				BrandCode: fundProcessingResult.BrandCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				CurrencyCode: query.CurrencyCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: gameProviderBalance.FreeBalance),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeRepository
			.Received(1)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			transactionId: Arg.Is(transactionResponse.TransactionId),
			balance: Arg.Is(transactionResponse.Balance),
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
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInZeroQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY");

		_ = fakeFundsDistributedLock.LockingAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 10m,
			FreeBalance: 0m,
			OutstandingBalance: 10m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var convertPlatformBalanceResult = new ConvertPlatformBalanceResult(
			ExchangeRate: 1,
			Balance: 10.0m,
			FreeBalance: 0m,
			OutstandingBalance: 10.0m);
		_ = fakeGameCurrencyClient.ConvertPlatformBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			balance: Arg.Is(gameProviderBalance.Balance),
			freeBalance: Arg.Is(gameProviderBalance.FreeBalance),
			outstandingBalance: Arg.Is(gameProviderBalance.OutstandingBalance),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(convertPlatformBalanceResult);

		var sut = new FundInZeroGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.GetAsync(query).AsTask());

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
