using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;

public class FundInGetServiceUnitTest
{
	[Theory]
	[InlineData(0.0)]
	[InlineData(-1.0)]
	public async Task ArgumentOutOfRangeException_ThrowIfNegativeOrZero(decimal amount)
	{
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: amount);

		var sut = new FundInGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.GetAsync(query).AsTask());

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
	[InlineData(0.0099)]
	public async Task ArgumentOutOfRangeException_ThrowIfLessThan_0_0099m(decimal amount)
	{
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: amount);

		var sut = new FundInGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.GetAsync(query).AsTask());

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
	public async Task ArgumentOutOfRangeException_ThrowIfGreaterThan_10000000_01m(decimal amount)
	{
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid(),
			CurrencyCode: "CNY",
			Amount: amount);

		var sut = new FundInGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.GetAsync(query).AsTask());

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
	public async Task Fund_In()
	{
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInQuery(
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
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString("N"),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
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
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			Arg.Is(query.WalletId),
			Arg.Is(query.Amount),
			Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundInGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

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
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateBySuccessAsync(
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(transactionResponse.Balance));

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
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
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInQuery(
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
			Arg.Is(query.PlayerCode)).Returns(Task.FromResult(false));

		var sut = new FundInGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(StateCode.TransactionLock, actual.State);

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(0)
			.UpdateBySuccessAsync(
			 Arg.Any<Guid>(),
			 Arg.Any<Guid>(),
			 Arg.Any<decimal>());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			 Arg.Any<string>(),
			 Arg.Any<string>());
	}

	[Fact]
	public async Task Insufficient_Balance_Of_Game_Provider()
	{
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInQuery(
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
			Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 10m,
			FreeBalance: 9m,
			OutstandingBalance: 1m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var sut = new FundInGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.GetAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_1000_Amount_1m_FundIn_Game_Provider_IDR()
	{
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInQuery(
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
			Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1999m,
			FreeBalance: 1000m,
			OutstandingBalance: 999m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString("N"),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			Arg.Is(query.WalletId),
			Arg.Is(query.Amount),
			Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundInGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

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
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateBySuccessAsync(
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(transactionResponse.Balance));

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_Amount_1000m_FundIn_Game_Provider_IDR_1000()
	{
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInQuery(
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
			Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 10m,
			FreeBalance: 1m,
			OutstandingBalance: 9m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString("N"),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			Arg.Is(query.WalletId),
			Arg.Is(query.Amount),
			Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundInGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

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
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateBySuccessAsync(
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(transactionResponse.Balance));

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_Amount_1m_FundIn_Game_Provider_IDR_1000()
	{
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInQuery(
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
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1m,
			FreeBalance: 0.001m,
			OutstandingBalance: 0.999m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var gameProviderFundTransferTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			GameProviderFundTransferTransactionId: Guid.NewGuid(),
			ExternalTransactionId: gameProviderFundTransferTransactionId.ToString("N"),
			GameProviderTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundProcessingResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			Arg.Is(query.WalletId),
			Arg.Is(query.Amount),
			Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			Arg.Is(fundProcessingResult.ExternalTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundInGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

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
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateBySuccessAsync(
			 Arg.Is(fundProcessingResult.GameProviderFundTransferTransactionId),
			 Arg.Is(transactionResponse.TransactionId),
			 Arg.Is(transactionResponse.Balance));

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task This_player_gets_a_game_provider_free_balance_less_than_zero()
	{
		var faceLogger = NullLogger<FundInGetService>.Instance;
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundInQuery(
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
			.Returns(Task.FromResult(true));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1720.00m,
			FreeBalance: -1720.00m,
			OutstandingBalance: 0.0m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var sut = new FundInGetService(
			faceLogger,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundInService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.GetAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}
}
