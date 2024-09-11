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

public class FundTransferInGetServiceUnitTest
{
	[Theory]
	[InlineData(0.0)]
	[InlineData(-1.0)]
	public async Task ArgumentOutOfRangeException_ThrowIfNegativeOrZero(decimal amount)
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

		var query = new FundTransferInQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: amount);

		var sut = new FundTransferInGetService(
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

		_ = fakeClient
			.Received(0)
			.GameFundTransferUpdateBySuccessAsync(
			id: Arg.Any<Guid>(),
			cashRespTransactionId: Arg.Any<Guid>(),
			gameRespTransactionId: Arg.Any<string>(),
			mainWalletBalance: Arg.Any<decimal>(),
			description: Arg.Any<string>(),
			additionalData: Arg.Any<JsonNode>(),
			cancellationToken: Arg.Any<CancellationToken>());

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
		var faceLogger = NullLogger<FundTransferInGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeClient = Substitute.For<IGameFundClient>();

		var query = new FundTransferInQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: amount);

		var sut = new FundTransferInGetService(
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

		_ = fakeClient
			.Received(0)
			.GameFundTransferUpdateBySuccessAsync(
			id: Arg.Any<Guid>(),
			cashRespTransactionId: Arg.Any<Guid>(),
			gameRespTransactionId: Arg.Any<string>(),
			mainWalletBalance: Arg.Any<decimal>(),
			description: Arg.Any<string>(),
			additionalData: Arg.Any<JsonNode>(),
			cancellationToken: Arg.Any<CancellationToken>());

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
		var faceLogger = NullLogger<FundTransferInGetService>.Instance;
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
		var fakeFundsDistributedLock = Substitute.For<IFundsDistributedLock>();
		var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeService = Substitute.For<IGetService<FundProcessingFundInAddQuery, FundProcessingResult>>();
		var fakeGameProviderFundInService = Substitute.For<IGetService<GameProviderFundInQuery, GameProviderFundInResult>>();
		var fakeClient = Substitute.For<IGameFundClient>();

		var query = new FundTransferInQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: amount);

		var sut = new FundTransferInGetService(
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

		_ = fakeClient
			.Received(0)
			.GameFundTransferUpdateBySuccessAsync(
			id: Arg.Any<Guid>(),
			cashRespTransactionId: Arg.Any<Guid>(),
			gameRespTransactionId: Arg.Any<string>(),
			mainWalletBalance: Arg.Any<decimal>(),
			description: Arg.Any<string>(),
			additionalData: Arg.Any<JsonNode>(),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			gameProviderCode: Arg.Any<string>(),
			playerCode: Arg.Any<string>());
	}

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

		var query = new FundTransferInQuery(
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
			Arg.Is(query.Amount)
			).Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 100m,
			FreeBalance: 10m,
			OutstandingBalance: 90m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			FundTransactionId: fundTransactionId,
			CashTransactionId: fundTransactionId.ToString("N"),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
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
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundInResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 10
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			Arg.Is(playerInfo.WalletId),
			Arg.Is(query.Amount),
			Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			Arg.Is(fundProcessingResult.CashTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundTransferInGetService(
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
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
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

		var query = new FundTransferInQuery(
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
			Arg.Is(query.Amount)
			).Returns(exchangeRateFundResult);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromException(new FundTransferException(query.GameProviderCode, query.PlayerCode)));

		var sut = new FundTransferInGetService(
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

		_ = fakeClient
			.Received(0)
			.GameFundTransferUpdateBySuccessAsync(
			id: Arg.Any<Guid>(),
			cashRespTransactionId: Arg.Any<Guid>(),
			gameRespTransactionId: Arg.Any<string>(),
			mainWalletBalance: Arg.Any<decimal>(),
			description: Arg.Any<string>(),
			additionalData: Arg.Any<JsonNode>(),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeFundsDistributedLock
			.Received(0)
			.UnlockAsync(
			 Arg.Any<string>(),
			 Arg.Any<string>());
	}

	[Fact]
	public async Task Insufficient_Balance_Of_Game_Provider()
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

		var query = new FundTransferInQuery(
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
			Arg.Is(query.Amount)
			).Returns(exchangeRateFundResult);

		_ = fakeFundsDistributedLock.LockingAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 10m,
			FreeBalance: 9m,
			OutstandingBalance: 1m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var sut = new FundTransferInGetService(
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
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_1000_Amount_1m_FundIn_Game_Provider_IDR()
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

		var query = new FundTransferInQuery(
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			Amount: 1m);

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

		var exchangeRateFundResult = new ConvertGameProviderFundsResult(
			ExchangeRate: 0.001m,
			Amount: 1000m);
		_ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is("IDR_1000"),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1999m,
			FreeBalance: 1000m,
			OutstandingBalance: 999m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			FundTransactionId: fundTransactionId,
			CashTransactionId: fundTransactionId.ToString("N"),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
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
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundInResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			Arg.Is(playerInfo.WalletId),
			Arg.Is(query.Amount),
			Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			Arg.Is(fundProcessingResult.CashTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundTransferInGetService(
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
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_Amount_1000m_FundIn_Game_Provider_IDR_1000()
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

		var query = new FundTransferInQuery(
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
			Arg.Is("IDR"),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 10m,
			FreeBalance: 1m,
			OutstandingBalance: 9m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			FundTransactionId: fundTransactionId,
			CashTransactionId: fundTransactionId.ToString("N"),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
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
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundInResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			Arg.Is(playerInfo.WalletId),
			Arg.Is(query.Amount),
			Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			Arg.Is(fundProcessingResult.CashTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundTransferInGetService(
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
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task Platform_Currency_IDR_Amount_1m_FundIn_Game_Provider_IDR_1000()
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

		var query = new FundTransferInQuery(
			GameProviderCode: GameProviderType.AWC_PGS,
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
			Arg.Is("IDR"),
			Arg.Is(query.Amount))
			.Returns(exchangeRateFundResult);

		var gameProviderBalance = new PlayerBalanceResponse(
			Balance: 1m,
			FreeBalance: 0.001m,
			OutstandingBalance: 0.999m);
		_ = fakeGameProvidersClient
			.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var fundTransactionId = Guid.NewGuid();
		var fundProcessingResult = new FundProcessingResult(
			BrandId: Guid.NewGuid(),
			BrandCode: "GPI",
			PlayerCode: query.PlayerCode,
			Username: query.PlayerCode,
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: query.GameProviderCode,
			FundTransactionId: fundTransactionId,
			CashTransactionId: fundTransactionId.ToString("N"),
			GameTransactionId: "72412202405200607050004ucmqplxb029");
		_ = fakeService.GetAsync(
			query: Arg.Is(new FundProcessingFundInAddQuery(
				GameProviderCode: query.GameProviderCode,
				PlayerCode: query.PlayerCode,
				Amount: query.Amount)),
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
				Amount: exchangeRateFundResult.Amount),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(gameProviderFundInResult);

		var transactionResponse = new TransactionResponse()
		{
			TransactionId = Guid.NewGuid(),
			Balance = 1m
		};
		_ = fakeCashServiceClient.GameProviderFundInAsync(
			Arg.Is(playerInfo.WalletId),
			Arg.Is(query.Amount),
			Arg.Is($"【{query.GameProviderCode}】->【Default】"),
			Arg.Is(fundProcessingResult.CashTransactionId.ToString()))
			.Returns(ValueTask.FromResult(transactionResponse));

		var sut = new FundTransferInGetService(
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
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task This_player_gets_a_game_provider_free_balance_less_than_zero()
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

		var query = new FundTransferInQuery(
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
			Balance: 1720.00m,
			FreeBalance: -1720.00m,
			OutstandingBalance: 0.0m);
		_ = fakeGameProvidersClient.PlayerBalanceAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode))
			.Returns(ValueTask.FromResult(gameProviderBalance));

		var sut = new FundTransferInGetService(
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

		_ = fakeFundsDistributedLock
			.Received(1)
			.UnlockAsync(
			Arg.Is(query.GameProviderCode),
			Arg.Is(query.PlayerCode));
	}
}
