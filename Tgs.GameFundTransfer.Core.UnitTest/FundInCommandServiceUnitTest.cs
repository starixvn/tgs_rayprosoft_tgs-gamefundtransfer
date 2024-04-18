using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.Core.Exceptions;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Core.UnitTest;

[TestClass]
public class FundInCommandServiceUnitTest
{
	[TestMethod]
	public async Task Amount_Can_Only_Be_Positive()

	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeFundsLocked = Substitute.For<IFundsDistributedLock>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new FundInCommand(
			Guid.NewGuid(),
			GameProviderType.Kaiyuan,
			"GPI",
			"4ucmqplxb029",
			Guid.NewGuid(),
			"CNY",
			-1.0m
			);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};

		var sut = new FundInCommandService(
			faceLogger,
			fakeCashServiceClient,
			fakeFundsLocked,
			fakeGameProviderFundTransferTransactionRepository,
			fakeGameCurrencyClient,
			fakeGameProvidersClient);

		_ = await Assert.ThrowsExceptionAsync<AmountFormatException>(async () =>
		await sut.ExecuteAsync(query).ConfigureAwait(false))
			.ConfigureAwait(false);

		_ = fakeFundsLocked
			.Received(0)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[TestMethod]
	public async Task Fund_In()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeFundsLocked = Substitute.For<IFundsDistributedLock>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new FundInCommand(
			Guid.NewGuid(),
			GameProviderType.Kaiyuan,
			"GPI",
			"4ucmqplxb029",
			Guid.NewGuid(),
			"CNY",
			10m
			);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
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

		_ = fakeFundsLocked.LockingAsync(
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

		_ = fakeGameProviderFundTransferTransactionRepository
			.AddFundInAsync(
			gameProviderId: Arg.Is(query.GameProviderId),
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(query.Amount))
			.Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

		_ = fakeGameProvidersClient
			.PlayerFundInAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.BrandCode),
			Arg.Is(query.PlayerCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
			Arg.Is(query.Amount))
			.Returns(ValueTask.FromResult(gameProviderTransactionId));

		_ = fakeCashServiceClient
			.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(gameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository.
			UpdateBySuccessAsync(
			 Arg.Is(gameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(gameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var fakeService = new FundInCommandService(
			faceLogger,
			fakeCashServiceClient,
			fakeFundsLocked,
			fakeGameProviderFundTransferTransactionRepository,
			fakeGameCurrencyClient,
			fakeGameProvidersClient);

		await fakeService.ExecuteAsync(query).ConfigureAwait(false);

		_ = fakeFundsLocked
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[TestMethod]
	public async Task Fund_Transfer_Lock_Failed()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeFundsLocked = Substitute.For<IFundsDistributedLock>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new FundInCommand(
			Guid.NewGuid(),
			GameProviderType.Kaiyuan,
			"GPI",
			"4ucmqplxb029",
			Guid.NewGuid(),
			"CNY",
			10m
			);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};

		_ = fakeGameCurrencyClient
			.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)
			)
			.Returns(new ConvertGameProviderFundsResult(
				ExchangeRate: 1,
				Amount: query.Amount));

		_ = fakeFundsLocked
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(false));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 100m,
			FreeBalance: 10m,
			OutstandingBalance: 90m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeGameProviderFundTransferTransactionRepository
			.AddFundInAsync(
			gameProviderId: Arg.Is(query.GameProviderId),
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(query.Amount))
			.Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

		_ = fakeGameProvidersClient
			.PlayerFundInAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.BrandCode),
			Arg.Is(query.PlayerCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
			Arg.Is(query.Amount))
			.Returns(ValueTask.FromResult(gameProviderTransactionId));

		_ = fakeCashServiceClient
			.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(gameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository
			.UpdateBySuccessAsync(
			 Arg.Is(gameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(gameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var fakeService = new FundInCommandService(
			faceLogger,
			fakeCashServiceClient,
			fakeFundsLocked,
			fakeGameProviderFundTransferTransactionRepository,
			fakeGameCurrencyClient,
			fakeGameProvidersClient);

		await fakeService.ExecuteAsync(query).ConfigureAwait(false);

		_ = fakeFundsLocked
			.Received(0)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[TestMethod]
	public async Task Insufficient_Balance_Of_Game_Provider()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeFundsLocked = Substitute.For<IFundsDistributedLock>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new FundInCommand(
			Guid.NewGuid(),
			GameProviderType.Kaiyuan,
			"GPI",
			"4ucmqplxb029",
			Guid.NewGuid(),
			"CNY",
			10m
			);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};

		_ = fakeGameCurrencyClient
			.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount))
			.Returns(new ConvertGameProviderFundsResult(
				ExchangeRate: 1,
				Amount: query.Amount));

		_ = fakeFundsLocked.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 10m,
			FreeBalance: 9m,
			OutstandingBalance: 1m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var fakeService = new FundInCommandService(
			faceLogger,
			fakeCashServiceClient,
			fakeFundsLocked,
			fakeGameProviderFundTransferTransactionRepository,
			fakeGameCurrencyClient,
			fakeGameProvidersClient);

		_ = await Assert.ThrowsExceptionAsync<FreeBalanceInsufficientException>(() => fakeService.ExecuteAsync(query).AsTask()).ConfigureAwait(false);

		_ = fakeFundsLocked
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[TestMethod]
	public async Task Platform_Currency_IDR_1000_Amount_0001m_FundIn_Game_Provider_IDR()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeFundsLocked = Substitute.For<IFundsDistributedLock>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new FundInCommand(
			Guid.NewGuid(),
			GameProviderType.Kaiyuan,
			"GPI",
			"4ucmqplxb029",
			Guid.NewGuid(),
			"IDR_1000",
			0.001m
			);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 0.001m,
			Amount: 1m);

		_ = fakeGameCurrencyClient
			.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		_ = fakeFundsLocked
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 100m,
			FreeBalance: 1m,
			OutstandingBalance: 99m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeGameProviderFundTransferTransactionRepository
			.AddFundInAsync(
			gameProviderId: Arg.Is(query.GameProviderId),
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(query.Amount))
			.Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

		_ = fakeGameProvidersClient
			.PlayerFundInAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.BrandCode),
			Arg.Is(query.PlayerCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
			Arg.Is(exchangeRateFundResult.Amount))
			.Returns(ValueTask.FromResult(gameProviderTransactionId));

		_ = fakeCashServiceClient
			.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(gameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository
			.UpdateBySuccessAsync(
			 Arg.Is(gameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(gameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var fakeService = new FundInCommandService(
			faceLogger,
			fakeCashServiceClient,
			fakeFundsLocked,
			fakeGameProviderFundTransferTransactionRepository,
			fakeGameCurrencyClient,
			fakeGameProvidersClient);

		await fakeService.ExecuteAsync(query).ConfigureAwait(false);

		_ = fakeFundsLocked
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[TestMethod]
	public async Task Platform_Currency_IDR_1000_Amount_1m_FundIn_Game_Provider_IDR()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeFundsLocked = Substitute.For<IFundsDistributedLock>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new FundInCommand(
			Guid.NewGuid(),
			GameProviderType.Kaiyuan,
			"GPI",
			"4ucmqplxb029",
			Guid.NewGuid(),
			"IDR_1000",
			1m
			);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 0.001m,
			Amount: 1000m);

		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)).Returns(exchangeRateFundResult);

		_ = fakeFundsLocked.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1999m,
			FreeBalance: 1000m,
			OutstandingBalance: 999m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeGameProviderFundTransferTransactionRepository
			.AddFundInAsync(
			gameProviderId: Arg.Is(query.GameProviderId),
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(query.Amount))
			.Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

		_ = fakeGameProvidersClient
			.PlayerFundInAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.BrandCode),
			Arg.Is(query.PlayerCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
			Arg.Is(exchangeRateFundResult.Amount))
			.Returns(gameProviderTransactionId);

		_ = fakeCashServiceClient
			.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(gameProviderFundTransferTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository
			.UpdateBySuccessAsync(
			 Arg.Is(gameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(gameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var fakeService = new FundInCommandService(
			faceLogger,
			fakeCashServiceClient,
			fakeFundsLocked,
			fakeGameProviderFundTransferTransactionRepository,
			fakeGameCurrencyClient,
			fakeGameProvidersClient);

		await fakeService.ExecuteAsync(query).ConfigureAwait(false);

		_ = fakeFundsLocked
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[TestMethod]
	public async Task Platform_Currency_IDR_Amount_1000m_FundIn_Game_Provider_IDR_1000()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeFundsLocked = Substitute.For<IFundsDistributedLock>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new FundInCommand(
			Guid.NewGuid(),
			GameProviderType.AWC_PGS,
			"GPI",
			"4ucmqplxb029",
			Guid.NewGuid(),
			"IDR",
			1000m
			);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1000,
			Amount: 1);

		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)).Returns(exchangeRateFundResult);

		_ = fakeFundsLocked.LockingAsync(
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

		_ = fakeGameProviderFundTransferTransactionRepository.AddFundInAsync(
			gameProviderId: Arg.Is(query.GameProviderId),
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(query.Amount)).Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

		_ = fakeGameProvidersClient.PlayerFundInAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.BrandCode),
			Arg.Is(query.PlayerCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
			Arg.Is(exchangeRateFundResult.Amount)).Returns(ValueTask.FromResult(gameProviderTransactionId));

		_ = fakeCashServiceClient.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(gameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			 Arg.Is(gameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(gameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var fakeService = new FundInCommandService(
			faceLogger,
			fakeCashServiceClient,
			fakeFundsLocked,
			fakeGameProviderFundTransferTransactionRepository,
			fakeGameCurrencyClient,
			fakeGameProvidersClient);

		await fakeService.ExecuteAsync(query).ConfigureAwait(false);

		_ = fakeFundsLocked
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[TestMethod]
	public async Task Platform_Currency_IDR_Amount_1m_FundIn_Game_Provider_IDR_1000()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeFundsLocked = Substitute.For<IFundsDistributedLock>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new FundInCommand(
			Guid.NewGuid(),
			GameProviderType.AWC_PGS,
			"GPI",
			"4ucmqplxb029",
			Guid.NewGuid(),
			"IDR",
			1m
			);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1000,
			Amount: 0.001m);

		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(query.Amount)).Returns(exchangeRateFundResult);

		_ = fakeFundsLocked.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1m,
			FreeBalance: 0.001m,
			OutstandingBalance: 0.999m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		_ = fakeGameProviderFundTransferTransactionRepository.AddFundInAsync(
			gameProviderId: Arg.Is(query.GameProviderId),
			gameProviderCode: Arg.Is(query.GameProviderCode),
			playerCode: Arg.Is(query.PlayerCode),
			walletId: Arg.Is(query.WalletId),
			amount: Arg.Is(query.Amount)).Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

		_ = fakeGameProvidersClient.PlayerFundInAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.BrandCode),
			Arg.Is(query.PlayerCode),
			Arg.Is(query.CurrencyCode),
			Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
			Arg.Is(exchangeRateFundResult.Amount)).Returns(ValueTask.FromResult(gameProviderTransactionId));

		_ = fakeCashServiceClient.GameProviderFundInAsync(
			 Arg.Is(query.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			 Arg.Is(gameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(transactionResponse));

		await fakeGameProviderFundTransferTransactionRepository.UpdateBySuccessAsync(
			 Arg.Is(gameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(gameProviderTransactionId),
			 Arg.Is(transactionResponse.Balance));

		var fakeService = new FundInCommandService(
			faceLogger,
			fakeCashServiceClient,
			fakeFundsLocked,
			fakeGameProviderFundTransferTransactionRepository,
			fakeGameCurrencyClient,
			fakeGameProvidersClient);

		await fakeService.ExecuteAsync(query).ConfigureAwait(false);

		_ = fakeFundsLocked
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[TestMethod]
	public async Task This_player_gets_a_game_provider_free_balance_less_than_zero()
	{
		var faceLogger = NullLogger<FundInCommandService>.Instance;
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeFundsLocked = Substitute.For<IFundsDistributedLock>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

		var query = new FundInCommand(
			Guid.NewGuid(),
			GameProviderType.Kaiyuan,
			"GPI",
			"4ucmqplxb029",
			Guid.NewGuid(),
			"CNY",
			10m
			);

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
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

		_ = fakeFundsLocked.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1720.00m,
			FreeBalance: -1720.00m,
			OutstandingBalance: 0.0m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var sut = new FundInCommandService(
			faceLogger,
			fakeCashServiceClient,
			fakeFundsLocked,
			fakeGameProviderFundTransferTransactionRepository,
			fakeGameCurrencyClient,
			fakeGameProvidersClient);

		_ = await Assert.ThrowsExceptionAsync<FreeBalanceException>(async () =>
		await sut.ExecuteAsync(query).ConfigureAwait(false))
			.ConfigureAwait(false);

		_ = fakeFundsLocked
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}
}
