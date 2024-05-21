using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.Core.Exceptions;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;

public class FundInCommandServiceUnitTest
{
	[Fact]
	public async Task Amount_Can_Only_Be_Positive()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: -1.0m);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};

		var sut = new FundInCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<AmountFormatException>(() => sut.ExecuteAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Fund_In()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInCommand(
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
			Arg.Is(query.Amount)
			).Returns(exchangeRateFundResult);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 100m,
			FreeBalance: 10m,
			OutstandingBalance: 90m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		_ = fakeGameProviderFundInService.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				BrandCode: fundProcessingResult.BrandCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				CurrencyCode: query.CurrencyCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(new GameProviderFundInResult(fundProcessingResult.GameProviderTransactionId));

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(fundProcessingResult.GameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var sut = new FundInCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Fund_Transfer_Lock_Failed()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInCommand(
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

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(false));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 100m,
			FreeBalance: 10m,
			OutstandingBalance: 90m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		_ = fakeGameProviderFundInService.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				BrandCode: fundProcessingResult.BrandCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				CurrencyCode: query.CurrencyCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(new GameProviderFundInResult(fundProcessingResult.GameProviderTransactionId));

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(fundProcessingResult.GameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var sut = new FundInCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Insufficient_Balance_Of_Game_Provider()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInCommand(
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

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 10m,
			FreeBalance: 9m,
			OutstandingBalance: 1m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var sut = new FundInCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<FreeBalanceInsufficientException>(() => sut.ExecuteAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_1000_Amount_0001m_FundIn_Game_Provider_IDR()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "IDR_1000",
			Amount: 0.001m);

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 0.001m,
			Amount: 1m);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 100m,
			FreeBalance: 1m,
			OutstandingBalance: 99m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		_ = fakeGameProviderFundInService.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				BrandCode: fundProcessingResult.BrandCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				CurrencyCode: query.CurrencyCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(new GameProviderFundInResult(fundProcessingResult.GameProviderTransactionId));

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(fundProcessingResult.GameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var sut = new FundInCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_1000_Amount_1m_FundIn_Game_Provider_IDR()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "IDR_1000",
			Amount: 1m);

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 0.001m,
			Amount: 1000m);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1999m,
			FreeBalance: 1000m,
			OutstandingBalance: 999m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		_ = fakeGameProviderFundInService.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				BrandCode: fundProcessingResult.BrandCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				CurrencyCode: query.CurrencyCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(new GameProviderFundInResult(fundProcessingResult.GameProviderTransactionId));

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(fundProcessingResult.GameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var sut = new FundInCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_Amount_1000m_FundIn_Game_Provider_IDR_1000()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInCommand(
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

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 100m,
			FreeBalance: 1m,
			OutstandingBalance: 99m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.AWC_PGS,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		_ = fakeGameProviderFundInService.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				BrandCode: fundProcessingResult.BrandCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				CurrencyCode: query.CurrencyCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(new GameProviderFundInResult(fundProcessingResult.GameProviderTransactionId));

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(fundProcessingResult.GameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var sut = new FundInCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_Amount_1m_FundIn_Game_Provider_IDR_1000()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInCommand(
			GameProviderCode: GameProviderType.AWC_PGS,
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
			Arg.Is(query.Amount)).Returns(exchangeRateFundResult);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1m,
			FreeBalance: 0.001m,
			OutstandingBalance: 0.999m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.AWC_PGS,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		_ = fakeGameProviderFundInService.GetAsync(
			query: new GameProviderFundInQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				BrandCode: fundProcessingResult.BrandCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				CurrencyCode: query.CurrencyCode,
				GameProviderTransactionId: fundProcessingResult.GameProviderTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(new GameProviderFundInResult(fundProcessingResult.GameProviderTransactionId));

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(fundProcessingResult.GameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var sut = new FundInCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task This_player_gets_a_game_provider_free_balance_less_than_zero()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInCommand(
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

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1720.00m,
			FreeBalance: -1720.00m,
			OutstandingBalance: 0.0m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var sut = new FundInCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<FreeBalanceException>(async () =>
		await sut.ExecuteAsync(query).ConfigureAwait(false));

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}
}
