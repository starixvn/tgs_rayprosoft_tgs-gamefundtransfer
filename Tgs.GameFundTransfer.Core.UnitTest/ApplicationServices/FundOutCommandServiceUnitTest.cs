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

public class FundOutCommandServiceUnitTest
{
	[Fact]
	public async Task Amount_Can_Only_Be_Positive()
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: -1.0m);

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<AmountFormatException>(() => sut.ExecuteAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
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
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var balance = 10.0m;

		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)
			).Returns(new ConvertGameProviderFundsResult(
				ExchangeRate: 1,
				Amount: query.Amount));

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

		var fundTransferRunning = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundOutAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransferRunning);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};
		_ = fakeCashServiceClient.GameProviderFundOutAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【Default】->【{fundTransferRunning.GameProviderCode}】"),
			 Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		_ = fakeGameProvidersClient.PlayerFundOutAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(fundTransferRunning.BrandCode),
			Arg.Is(query.PlayerCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(fundTransferRunning.GameProviderTransactionId),
			Arg.Is(query.Amount)).Returns(ValueTask.FromResult(fundTransferRunning.GameProviderTransactionId));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId),
			Arg.Is(transactionResponse.TransactionId),
			Arg.Is(fundTransferRunning.GameProviderTransactionId),
			Arg.Is(transactionResponse.Balance));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
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
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var balance = 10.0m;

		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)
			).Returns(new ConvertGameProviderFundsResult(
				ExchangeRate: 1,
				Amount: query.Amount));

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(false));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

		var fundTransferRunning = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundOutAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransferRunning);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};
		_ = fakeCashServiceClient.GameProviderFundOutAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【Default】->【{fundTransferRunning.GameProviderCode}】"),
			 Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		_ = fakeGameProvidersClient.PlayerFundOutAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(fundTransferRunning.BrandCode),
			Arg.Is(query.PlayerCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(fundTransferRunning.GameProviderTransactionId),
			Arg.Is(query.Amount)).Returns(ValueTask.FromResult(fundTransferRunning.GameProviderTransactionId));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId),
			Arg.Is(transactionResponse.TransactionId),
			Arg.Is(fundTransferRunning.GameProviderTransactionId),
			Arg.Is(transactionResponse.Balance));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
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
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var balance = 9m;
		var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};

		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)
			).Returns(new ConvertGameProviderFundsResult(
				ExchangeRate: 1,
				Amount: query.Amount));

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<MainWalletBalanceInsufficientException>(() => sut.ExecuteAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
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
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var balance = 10.0m;

		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)
			).Returns(new ConvertGameProviderFundsResult(
				ExchangeRate: 1,
				Amount: query.Amount));

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

		var fundTransferRunning = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundOutAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransferRunning);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = -10
		};
		_ = fakeCashServiceClient.GameProviderFundOutAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【Default】->【{fundTransferRunning.GameProviderCode}】"),
			 Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeCashServiceClient
			.Received(1)
			.GameProviderFundRefundAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is(transactionResponse.TransactionId.ToString()),
			 Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId.ToString()));

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateByMainWalletFundRefundFailedAsync(
				Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId),
				Arg.Any<string>());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
				Arg.Is(query.GameProviderCode),
				Arg.Is(query.PlayerCode));
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
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var balance = 10.0m;

		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)
			).Returns(new ConvertGameProviderFundsResult(
				ExchangeRate: 1,
				Amount: query.Amount));

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

		var fundTransferRunning = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundOutAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransferRunning);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = -10
		};
		_ = fakeCashServiceClient.GameProviderFundOutAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【Default】->【{fundTransferRunning.GameProviderCode}】"),
			 Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId.ToString()))
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
			 Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(fundRefundResponse));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeGameProviderFundTransferTransactionRepository.Received(1)
			.UpdateByMainWalletFundRefundSuccessAsync(
				Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId),
				Arg.Is(fundRefundResponse.TransactionId),
				Arg.Is(fundRefundResponse.Balance),
				Arg.Any<string>());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
				Arg.Is(query.GameProviderCode),
				Arg.Is(query.PlayerCode));
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
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.AWC_PGS,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "IDR",
			Amount: 1000m);

		var balance = 1000m;

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1000,
			Amount: 1);

		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)
			).Returns(exchangeRateFundResult);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId))
			.Returns(ValueTask.FromResult(balance));

		var fundTransferRunning = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.AWC_PGS,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundOutAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransferRunning);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 0m
		};
		_ = fakeCashServiceClient.GameProviderFundOutAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【Default】->【{fundTransferRunning.GameProviderCode}】"),
			 Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		_ = fakeGameProvidersClient.PlayerFundOutAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(fundTransferRunning.BrandCode),
			Arg.Is(query.PlayerCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(fundTransferRunning.GameProviderTransactionId),
			Arg.Is(exchangeRateFundResult.Amount))
			.Returns(ValueTask.FromResult(fundTransferRunning.GameProviderTransactionId));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId),
			Arg.Is(transactionResponse.TransactionId),
			Arg.Is(fundTransferRunning.GameProviderTransactionId),
			Arg.Is(transactionResponse.Balance));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_Amount_1m_FundOut_Game_Provider_IDR_1000()
	{
		var faceLogger = NullLogger<FundOutCommandService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.AWC_PGS,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "IDR",
			Amount: 1m);

		var balance = 1m;

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1000,
			Amount: 0.001m);

		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)
			).Returns(exchangeRateFundResult);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

		var fundTransferRunning = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundOutAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransferRunning);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 0m
		};
		_ = fakeCashServiceClient.GameProviderFundOutAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【Default】->【{query.GameProviderCode}】"),
			 Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		_ = fakeGameProvidersClient.PlayerFundOutAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(fundTransferRunning.BrandCode),
			Arg.Is(query.PlayerCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(fundTransferRunning.GameProviderTransactionId),
			Arg.Is(exchangeRateFundResult.Amount)).Returns(ValueTask.FromResult(fundTransferRunning.GameProviderTransactionId));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			Arg.Is(fundTransferRunning.GameProviderFundTransferTransactionId),
			Arg.Is(transactionResponse.TransactionId),
			Arg.Is(fundTransferRunning.GameProviderTransactionId),
			Arg.Is(transactionResponse.Balance));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundTransferTransactionRepository);

		await sut.ExecuteAsync(query);

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
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
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundOutCommand(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: 10m);

		var balance = 0.0m;

		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)
			).Returns(new ConvertGameProviderFundsResult(
				ExchangeRate: 1,
				Amount: query.Amount));

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 0m,
			FreeBalance: 0m,
			OutstandingBalance: 0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

		var sut = new FundOutCommandService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<MainWalletBalanceException>(() => sut.ExecuteAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}
}
