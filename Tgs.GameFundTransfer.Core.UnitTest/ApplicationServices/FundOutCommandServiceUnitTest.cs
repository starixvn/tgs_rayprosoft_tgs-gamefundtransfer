using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;

public class FundOutCommandServiceUnitTest
{
	[Theory]
	[InlineData(0.0)]
	[InlineData(-1.0)]
	public async Task ArgumentOutOfRangeException_ThrowIfNegativeOrZero(decimal amount)
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: amount);

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.ExecuteAsync(query).AsTask());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(0)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Any<Guid>(),
			transactionId: Arg.Any<Guid>(),
			balance: Arg.Any<decimal>());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			gameProviderCode: Arg.Any<string>(),
			playerCode: Arg.Any<string>());
	}

	[Theory]
	[InlineData(10000000.01)]
	public async Task ArgumentOutOfRangeException_ThrowIfGreaterThan(decimal amount)
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: amount);

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.ExecuteAsync(query).AsTask());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(0)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Any<Guid>(),
			transactionId: Arg.Any<Guid>(),
			balance: Arg.Any<decimal>());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			gameProviderCode: Arg.Any<string>(),
			playerCode: Arg.Any<string>());
	}

	[Fact]
	public async Task ArgumentOutOfRangeException_ThrowIfGreaterThan_Platform_Currency_IDR_Amount_1m_FundIn_Game_Provider_IDR_1000()
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "IDR",
			Amount: 1m);

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1000,
			Amount: 0.001m);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.ExecuteAsync(query).AsTask());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(0)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Any<Guid>(),
			transactionId: Arg.Any<Guid>(),
			balance: Arg.Any<decimal>());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			gameProviderCode: Arg.Any<string>(),
			playerCode: Arg.Any<string>());
	}

	[Fact]
	public async Task Fund_Out()
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var balance = 10.0m;
		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId))
			.Returns(ValueTask.FromResult(balance));

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundOutAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};
		_ = fakeCashServiceClient.GameProviderFundOutAsync(
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(query.Amount),
			description: Arg.Is($"【Default】->【{query.GameProviderCode}】"),
			externalId: Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeGameProviderFundOutService
			.Received(1)
			.GetAsync(
			query: new GameProviderFundOutQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			transactionId: Arg.Is(transactionResponse.TransactionId),
			balance: Arg.Is(transactionResponse.Balance));

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Fund_Transfer_Lock_Failed()
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(gameProviderBalance);

		var balance = 9m;
		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId))
			.Returns(balance);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(false));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.ExecuteAsync(query).AsTask());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(0)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Any<Guid>(),
			transactionId: Arg.Any<Guid>(),
			balance: Arg.Any<decimal>());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			gameProviderCode: Arg.Any<string>(),
			playerCode: Arg.Any<string>());
	}

	[Fact]
	public async Task Insufficient_Balance_In_Main_Wallet()
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(gameProviderBalance);

		var balance = 9m;
		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId))
			.Returns(balance);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.ExecuteAsync(query).AsTask());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(0)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Any<Guid>(),
			transactionId: Arg.Any<Guid>(),
			balance: Arg.Any<decimal>());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			gameProviderCode: Arg.Any<string>(),
			playerCode: Arg.Any<string>());
	}

	[Fact]
	public async Task Main_Wallet_Balance_Is_Below_0_And_The_Refund_Fails()
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			amount: Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(gameProviderBalance);

		var balance = 10.0m;
		_ = fakeCashServiceClient.GetBalanceAsync(
			walletId: Arg.Is(query.WalletId))
			.Returns(balance);

		_ = fakeFundsDistributedLock.LockingAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundOutAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = -10
		};
		_ = fakeCashServiceClient.GameProviderFundOutAsync(
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(query.Amount),
			description: Arg.Is($"【Default】->【{query.GameProviderCode}】"),
			externalId: Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		_ = fakeCashServiceClient.GameProviderFundRefundAsync(
			 walletId: Arg.Is(query.WalletId),
			 amount: Arg.Is(query.Amount),
			 description: Arg.Is(transactionResponse.TransactionId.ToString()),
			 externalId: Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromException<TransactionResponse>(new HttpRequestException()));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(0)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Any<Guid>(),
			transactionId: Arg.Any<Guid>(),
			balance: Arg.Any<decimal>());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateByMainWalletFundRefundFailedAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			description: Arg.Any<string>());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Main_Wallet_Balance_Is_Below_0_And_The_Refund_Successful()
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			amount: Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var balance = 10.0m;
		_ = fakeCashServiceClient.GetBalanceAsync(
			walletId: Arg.Is(query.WalletId))
			.Returns(ValueTask.FromResult(balance));

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundOutAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = -10
		};
		_ = fakeCashServiceClient.GameProviderFundOutAsync(
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(query.Amount),
			description: Arg.Is($"【Default】->【{query.GameProviderCode}】"),
			externalId: Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var fundRefundResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 0
		};
		_ = fakeCashServiceClient.GameProviderFundRefundAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is(transactionResponse.TransactionId.ToString()),
			 Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()))
			.Returns(ValueTask.FromResult(fundRefundResponse));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateByMainWalletFundRefundSuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			transactionId: Arg.Is(fundRefundResponse.TransactionId),
			balance: Arg.Is(fundRefundResponse.Balance),
			description: Arg.Any<string>());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(0)
			.UpdateByMainWalletFundRefundFailedAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			description: Arg.Any<string>());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_Amount_1000m_FundOut_Game_Provider_IDR_1000()
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.AWC_PGS,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "IDR",
			Amount: 1000m);

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1000,
			Amount: 1);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var balance = 1000m;
		_ = fakeCashServiceClient.GetBalanceAsync(
			walletId: Arg.Is(query.WalletId))
			.Returns(ValueTask.FromResult(balance));

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundOutAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 0m
		};
		_ = fakeCashServiceClient.GameProviderFundOutAsync(
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(query.Amount),
			description: Arg.Is($"【Default】->【{query.GameProviderCode}】"),
			externalId: Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeGameProviderFundOutService
			.Received(1)
			.GetAsync(
			query: new GameProviderFundOutQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			transactionId: Arg.Is(transactionResponse.TransactionId),
			balance: Arg.Is(transactionResponse.Balance));

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_Amount_10m_FundIn_Game_Provider_IDR_1000()
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.AWC_PGS,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "IDR",
			Amount: 10m);

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1000,
			Amount: 0.01m);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(query.CurrencyCode),
			amount: Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var balance = 10m;
		_ = fakeCashServiceClient.GetBalanceAsync(
			walletId: Arg.Is(query.WalletId))
			.Returns(ValueTask.FromResult(balance));

		_ = fakeFundsDistributedLock.LockingAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundOutAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 0m
		};
		_ = fakeCashServiceClient.GameProviderFundOutAsync(
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(query.Amount),
			description: Arg.Is($"【Default】->【{query.GameProviderCode}】"),
			externalId: Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeGameProviderFundOutService
			.Received(1)
			.GetAsync(
			query: new GameProviderFundOutQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			transactionId: Arg.Is(transactionResponse.TransactionId),
			balance: Arg.Is(transactionResponse.Balance));

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task This_player_is_main_wallet_balance_is_less_than_zero()
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var balance = 0.0m;
		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId))
			.Returns(ValueTask.FromResult(balance));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.ExecuteAsync(query).AsTask());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(0)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Any<Guid>(),
			transactionId: Arg.Any<Guid>(),
			balance: Arg.Any<decimal>());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			gameProviderCode: Arg.Any<string>(),
			playerCode: Arg.Any<string>());
	}
}
