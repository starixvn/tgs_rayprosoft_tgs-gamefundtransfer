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

public class FundTransferOutGetServiceUnitTest
{
	[Theory]
	[InlineData(0.0)]
	[InlineData(-1.0)]
	public async Task ArgumentOutOfRangeException_ThrowIfNegativeOrZero(decimal amount)
	{
		var faceLogger = NullLogger<FundTransferOutGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundTransferOutQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: amount);

		var sut = new FundTransferOutGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
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
	public async Task ArgumentOutOfRangeException_ThrowIfGreaterThan(decimal amount)
	{
		var faceLogger = NullLogger<FundTransferOutGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundTransferOutQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: amount);

		var sut = new FundTransferOutGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
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
	public async Task ArgumentOutOfRangeException_ThrowIfGreaterThan_Platform_Currency_IDR_Amount_1m_FundIn_Game_Provider_IDR_1000()
	{
		var faceLogger = NullLogger<FundTransferOutGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundTransferOutQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: 1m);

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

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1000,
			Amount: 0.001m);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(playerInfo.CurrencyCode),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var sut = new FundTransferOutGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
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
	public async Task Fund_Out()
	{
		var faceLogger = NullLogger<FundTransferOutGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundTransferOutQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

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

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(playerInfo.CurrencyCode),
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
			Arg.Is(playerInfo.WalletId))
			.Returns(ValueTask.FromResult(balance));

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			FundTransactionId: fundTransactionId,
			CashTransactionId: fundTransactionId.ToString(),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
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
			walletId: Arg.Is(playerInfo.WalletId),
			amount: Arg.Is(query.Amount),
			description: Arg.Is($"【Default】->【{query.GameProviderCode}】"),
			externalId: Arg.Is(fundProcessingResult.CashTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var gameProviderFundOutResult = new GameProviderFundOutResult(
			GameTransactionId: fundProcessingResult.GameTransactionId,
			GameRespTransactionId: Guid.NewGuid().ToString("N"));
		_ = fakeGameProviderFundOutService
			.GetAsync(
			query: new GameProviderFundOutQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				WalletId: playerInfo.WalletId,
				GameTransactionId: fundProcessingResult.GameTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundOutResult);

		var sut = new FundTransferOutGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(fundProcessingResult.FundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundProcessingResult.CashTransactionId, actual.CashTransactionId);
		Assert.Equal(transactionResponse.TransactionId, actual.CashRespTransactionId);
		Assert.Equal(fundProcessingResult.GameTransactionId, actual.GameTransactionId);
		Assert.Equal(gameProviderFundOutResult.GameRespTransactionId, actual.GameRespTransactionId);

		_ = fakeFundsDistributedLock
			.Received(1)
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.FundTransactionId),
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
		var faceLogger = NullLogger<FundTransferOutGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundTransferOutQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

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

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(playerInfo.CurrencyCode),
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

		var balance = 10.0m;
		_ = fakeCashServiceClient.GetBalanceAsync(
			Arg.Is(playerInfo.WalletId))
			.Returns(balance);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromException(new FundTransferException(query.GameProviderCode, query.PlayerCode)));

		var sut = new FundTransferOutGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<FundTransferException>(() => sut.GetAsync(query).AsTask());

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
		var faceLogger = NullLogger<FundTransferOutGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundTransferOutQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

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

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(playerInfo.CurrencyCode),
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
			Arg.Is(playerInfo.WalletId))
			.Returns(balance);

		var sut = new FundTransferOutGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.GetAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(0)
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

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
		var faceLogger = NullLogger<FundTransferOutGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundTransferOutQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

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

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(playerInfo.CurrencyCode),
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
			walletId: Arg.Is(playerInfo.WalletId))
			.Returns(balance);

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			FundTransactionId: fundTransactionId,
			CashTransactionId: fundTransactionId.ToString(),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
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
			walletId: Arg.Is(playerInfo.WalletId),
			amount: Arg.Is(query.Amount),
			description: Arg.Is($"【Default】->【{query.GameProviderCode}】"),
			externalId: Arg.Is(fundProcessingResult.CashTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		_ = fakeCashServiceClient.GameProviderFundRefundAsync(
			 walletId: Arg.Is(playerInfo.WalletId),
			 amount: Arg.Is(query.Amount),
			 description: Arg.Is(transactionResponse.TransactionId.ToString()),
			 externalId: Arg.Is(fundProcessingResult.CashTransactionId.ToString()))
			.Returns(ValueTask.FromException<TransactionResponse>(new HttpRequestException()));

		var sut = new FundTransferOutGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(1)
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(0)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Any<Guid>(),
			transactionId: Arg.Any<Guid>(),
			balance: Arg.Any<decimal>());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateByMainWalletFundRefundFailedAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.FundTransactionId),
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
		var faceLogger = NullLogger<FundTransferOutGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundTransferOutQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

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

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(playerInfo.CurrencyCode),
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
			walletId: Arg.Is(playerInfo.WalletId))
			.Returns(ValueTask.FromResult(balance));

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			FundTransactionId: fundTransactionId,
			CashTransactionId: fundTransactionId.ToString(),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
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
			walletId: Arg.Is(playerInfo.WalletId),
			amount: Arg.Is(query.Amount),
			description: Arg.Is($"【Default】->【{query.GameProviderCode}】"),
			externalId: Arg.Is(fundProcessingResult.CashTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var fundRefundResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 0
		};
		_ = fakeCashServiceClient.GameProviderFundRefundAsync(
			 Arg.Is(playerInfo.WalletId),
			 Arg.Is(query.Amount),
			 Arg.Is(transactionResponse.TransactionId.ToString()),
			 Arg.Is(fundProcessingResult.CashTransactionId.ToString()))
			.Returns(ValueTask.FromResult(fundRefundResponse));

		var sut = new FundTransferOutGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<CashTransferNegativeException>(() => sut.GetAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(1)
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateByMainWalletFundRefundSuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.FundTransactionId),
			transactionId: Arg.Is(fundRefundResponse.TransactionId),
			balance: Arg.Is(fundRefundResponse.Balance),
			description: Arg.Any<string>());

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(0)
			.UpdateByMainWalletFundRefundFailedAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.FundTransactionId),
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
		var faceLogger = NullLogger<FundTransferOutGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundTransferOutQuery(
			GameProviderCode: GameProviderType.AWC_PGS,
			PlayerCode: "4ucmqplxb029",
			Amount: 1000m);

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

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1000,
			Amount: 1);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(playerInfo.CurrencyCode),
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
			walletId: Arg.Is(playerInfo.WalletId))
			.Returns(ValueTask.FromResult(balance));

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			FundTransactionId: fundTransactionId,
			CashTransactionId: fundTransactionId.ToString(),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
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
			walletId: Arg.Is(playerInfo.WalletId),
			amount: Arg.Is(query.Amount),
			description: Arg.Is($"【Default】->【{query.GameProviderCode}】"),
			externalId: Arg.Is(fundProcessingResult.CashTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var gameProviderFundOutResult = new GameProviderFundOutResult(
			GameTransactionId: fundProcessingResult.GameTransactionId,
			GameRespTransactionId: Guid.NewGuid().ToString("N"));
		_ = fakeGameProviderFundOutService
			.GetAsync(
			query: new GameProviderFundOutQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				WalletId: playerInfo.WalletId,
				GameTransactionId: fundProcessingResult.GameTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundOutResult);

		var sut = new FundTransferOutGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(fundProcessingResult.FundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundProcessingResult.CashTransactionId, actual.CashTransactionId);
		Assert.Equal(transactionResponse.TransactionId, actual.CashRespTransactionId);
		Assert.Equal(fundProcessingResult.GameTransactionId, actual.GameTransactionId);
		Assert.Equal(gameProviderFundOutResult.GameRespTransactionId, actual.GameRespTransactionId);

		_ = fakeFundsDistributedLock
			.Received(1)
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.FundTransactionId),
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
		var faceLogger = NullLogger<FundTransferOutGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundTransferOutQuery(
			GameProviderCode: GameProviderType.AWC_PGS,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

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

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1000,
			Amount: 0.01m);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			gameProviderCode: Arg.Is(query.GameProviderCode),
			currencyCode: Arg.Is(playerInfo.CurrencyCode),
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
			walletId: Arg.Is(playerInfo.WalletId))
			.Returns(ValueTask.FromResult(balance));

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			FundTransactionId: fundTransactionId,
			CashTransactionId: fundTransactionId.ToString(),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
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
			walletId: Arg.Is(playerInfo.WalletId),
			amount: Arg.Is(query.Amount),
			description: Arg.Is($"【Default】->【{query.GameProviderCode}】"),
			externalId: Arg.Is(fundProcessingResult.CashTransactionId.ToString()),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(ValueTask.FromResult(transactionResponse));

		var gameProviderFundOutResult = new GameProviderFundOutResult(
			GameTransactionId: fundProcessingResult.GameTransactionId,
			GameRespTransactionId: Guid.NewGuid().ToString("N"));
		_ = fakeGameProviderFundOutService
			.GetAsync(
			query: new GameProviderFundOutQuery(
				GameProviderCode: fundProcessingResult.GameProviderCode,
				PlayerCode: fundProcessingResult.PlayerCode,
				WalletId: playerInfo.WalletId,
				GameTransactionId: fundProcessingResult.GameTransactionId,
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundOutResult);

		var sut = new FundTransferOutGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		var actual = await sut.GetAsync(query);

		Assert.NotNull(actual);
		Assert.Equal(fundProcessingResult.FundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundProcessingResult.CashTransactionId, actual.CashTransactionId);
		Assert.Equal(transactionResponse.TransactionId, actual.CashRespTransactionId);
		Assert.Equal(fundProcessingResult.GameTransactionId, actual.GameTransactionId);
		Assert.Equal(gameProviderFundOutResult.GameRespTransactionId, actual.GameRespTransactionId);

		_ = fakeFundsDistributedLock
			.Received(1)
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

		_ = fakeGameProviderFundTransferTransactionRepository
			.Received(1)
			.UpdateBySuccessAsync(
			gameProviderFundTransferTransactionId: Arg.Is(fundProcessingResult.FundTransactionId),
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
		var faceLogger = NullLogger<FundTransferOutGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundOutService = Substitute.For<IGetService<GameProviderFundOutQuery, GameProviderFundOutResult>>();
		var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();

		var query = new FundTransferOutQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: 10m);

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

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 1,
			Amount: query.Amount);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(playerInfo.CurrencyCode),
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
			Arg.Is(playerInfo.WalletId))
			.Returns(ValueTask.FromResult(balance));

		var sut = new FundTransferOutGetService(
			faceLogger,
			fakePlayerRepository,
			fakeGameCurrencyClient,
			fakeFundsDistributedLock,
			fakeGameProvidersClient,
			fakeCashServiceClient,
			fakeService,
			fakeGameProviderFundOutService,
			fakeGameProviderFundTransferTransactionRepository);

		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.GetAsync(query).AsTask());

		_ = fakeFundsDistributedLock
			.Received(0)
			.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

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
