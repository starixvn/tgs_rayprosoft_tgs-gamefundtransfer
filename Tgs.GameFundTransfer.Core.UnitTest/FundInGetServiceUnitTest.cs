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
public class FundInGetServiceUnitTest
{
    [TestMethod]
    public async Task Amount_Can_Only_Be_Positive()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInQuery(
            Guid.NewGuid(),
            GameProviderType.Kaiyuan,
            "GPI",
            "4ucmqplxb029",
            Guid.NewGuid(),
            "CNY",
            -1.0m
            );

        var sut = new FundInGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        _ = await Assert.ThrowsExceptionAsync<AmountFormatException>(async () =>
        await sut.GetAsync(query).ConfigureAwait(false))
            .ConfigureAwait(false);

        _ = fakeDistributedLock
            .Received(0)
            .UnlockAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode));
    }

    [TestMethod]
    public async Task Fund_In()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInQuery(
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

        _ = fakeDistributedLock.LockingAsync(
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

        _ = fakeRepository.AddFundInAsync(
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
            Arg.Is(query.Amount)).Returns(ValueTask.FromResult(gameProviderTransactionId));

        _ = fakeCashServiceClient.GameProviderFundInAsync(
             Arg.Is(query.WalletId),
             Arg.Is(query.Amount),
             Arg.Is($"【{query.GameProviderCode}】->【Default】"),
             Arg.Is(gameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(transactionResponse));

        await fakeRepository.UpdateBySuccessAsync(
             Arg.Is(gameProviderFundTransferTransactionId),
             Arg.Is(transactionResponse.TransactionId),
             Arg.Is(gameProviderTransactionId),
             Arg.Is(transactionResponse.Balance));

        var fakeService = new FundInGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        var actual = await fakeService.GetAsync(query).ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.AreEqual(StateCode.Success, actual.State);

        _ = fakeDistributedLock
            .Received(1)
            .UnlockAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode));
    }

    [TestMethod]
    public async Task Fund_Transfer_Lock_Failed()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInQuery(
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

        _ = fakeDistributedLock.LockingAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode)).Returns(Task.FromResult(false));

        var gameProviderBalance = new PlayerBalanceResponse(
            Balance: 100m,
            FreeBalance: 10m,
            OutstandingBalance: 90m);
        _ = fakeGameProvidersClient.PlayerBalanceAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode)).Returns(ValueTask.FromResult(gameProviderBalance));

        _ = fakeRepository.AddFundInAsync(
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
            Arg.Is(query.Amount)).Returns(ValueTask.FromResult(gameProviderTransactionId));

        _ = fakeCashServiceClient.GameProviderFundInAsync(
             Arg.Is(query.WalletId),
             Arg.Is(query.Amount),
             Arg.Is($"【{query.GameProviderCode}】->【Default】"),
             Arg.Is(gameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(transactionResponse));

        await fakeRepository.UpdateBySuccessAsync(
             Arg.Is(gameProviderFundTransferTransactionId),
             Arg.Is(transactionResponse.TransactionId),
             Arg.Is(gameProviderTransactionId),
             Arg.Is(transactionResponse.Balance));

        var fakeService = new FundInGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        var actual = await fakeService.GetAsync(query).ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.AreEqual(StateCode.TransactionLock, actual.State);

        _ = fakeDistributedLock
            .Received(0)
            .UnlockAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode));
    }

    [TestMethod]
    public async Task Insufficient_Balance_Of_Game_Provider()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInQuery(
            Guid.NewGuid(),
            GameProviderType.Kaiyuan,
            "GPI",
            "4ucmqplxb029",
            Guid.NewGuid(),
            "CNY",
            10m
            );

        _ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.CurrencyCode),
            Arg.Is(query.Amount)
            ).Returns(new ConvertGameProviderFundsResult(
                ExchangeRate: 1,
                Amount: query.Amount));

        _ = fakeDistributedLock.LockingAsync(
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

        var fakeService = new FundInGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        _ = await Assert.ThrowsExceptionAsync<FreeBalanceInsufficientException>(() => fakeService.GetAsync(query).AsTask()).ConfigureAwait(false);

        _ = fakeDistributedLock
            .Received(1)
            .UnlockAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode));
    }

    [TestMethod]
    public async Task Platform_Currency_IDR_1000_Amount_0001m_FundIn_Game_Provider_IDR()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInQuery(
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

        _ = fakeGameCurrencyClient.ConvertGameProviderFundsAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.CurrencyCode),
            Arg.Is(query.Amount)).Returns(exchangeRateFundResult);

        _ = fakeDistributedLock.LockingAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

        var gameProviderBalance = new PlayerBalanceResponse(
            Balance: 10m,
            FreeBalance: 1m,
            OutstandingBalance: 9m);
        _ = fakeGameProvidersClient.PlayerBalanceAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode))
            .Returns(ValueTask.FromResult(gameProviderBalance));

        _ = fakeRepository.AddFundInAsync(
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

        await fakeRepository.UpdateBySuccessAsync(
             Arg.Is(gameProviderFundTransferTransactionId),
             Arg.Is(transactionResponse.TransactionId),
             Arg.Is(gameProviderTransactionId),
             Arg.Is(transactionResponse.Balance));

        var fakeService = new FundInGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        var actual = await fakeService.GetAsync(query).ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.AreEqual(StateCode.Success, actual.State);

        _ = fakeDistributedLock
            .Received(1)
            .UnlockAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode));
    }

    [TestMethod]
    public async Task Platform_Currency_IDR_1000_Amount_1m_FundIn_Game_Provider_IDR()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInQuery(
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

        _ = fakeDistributedLock.LockingAsync(
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

        _ = fakeRepository.AddFundInAsync(
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
            Arg.Is(exchangeRateFundResult.Amount)).Returns(gameProviderTransactionId);

        _ = fakeCashServiceClient.GameProviderFundInAsync(
             Arg.Is(query.WalletId),
             Arg.Is(query.Amount),
             Arg.Is($"【{query.GameProviderCode}】->【Default】"),
             Arg.Is(gameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(transactionResponse));

        await fakeRepository.UpdateBySuccessAsync(
             Arg.Is(gameProviderFundTransferTransactionId),
             Arg.Is(transactionResponse.TransactionId),
             Arg.Is(gameProviderTransactionId),
             Arg.Is(transactionResponse.Balance));

        var fakeService = new FundInGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        var actual = await fakeService.GetAsync(query).ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.AreEqual(StateCode.Success, actual.State);

        _ = fakeDistributedLock
            .Received(1)
            .UnlockAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode));
    }

    [TestMethod]
    public async Task Platform_Currency_IDR_Amount_1000m_FundIn_Game_Provider_IDR_1000()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInQuery(
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

        _ = fakeDistributedLock.LockingAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

        var gameProviderBalance = new PlayerBalanceResponse(
            Balance: 10m,
            FreeBalance: 1m,
            OutstandingBalance: 9m);
        _ = fakeGameProvidersClient
            .PlayerBalanceAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode))
            .Returns(ValueTask.FromResult(gameProviderBalance));

        _ = fakeRepository.AddFundInAsync(
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

        await fakeRepository.UpdateBySuccessAsync(
             Arg.Is(gameProviderFundTransferTransactionId),
             Arg.Is(transactionResponse.TransactionId),
             Arg.Is(gameProviderTransactionId),
             Arg.Is(transactionResponse.Balance));

        var fakeService = new FundInGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        var actual = await fakeService.GetAsync(query).ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.AreEqual(StateCode.Success, actual.State);

        _ = fakeDistributedLock
            .Received(1)
            .UnlockAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode));
    }

    [TestMethod]
    public async Task Platform_Currency_IDR_Amount_1m_FundIn_Game_Provider_IDR_1000()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInQuery(
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

        _ = fakeDistributedLock.LockingAsync(
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

        _ = fakeRepository.AddFundInAsync(
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

        await fakeRepository.UpdateBySuccessAsync(
             Arg.Is(gameProviderFundTransferTransactionId),
             Arg.Is(transactionResponse.TransactionId),
             Arg.Is(gameProviderTransactionId),
             Arg.Is(transactionResponse.Balance));

        var fakeService = new FundInGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        var actual = await fakeService.GetAsync(query).ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.AreEqual(StateCode.Success, actual.State);

        _ = fakeDistributedLock
            .Received(1)
            .UnlockAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode));
    }

    [TestMethod]
    public async Task This_player_gets_a_game_provider_free_balance_less_than_zero()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInQuery(
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

        _ = fakeDistributedLock.LockingAsync(
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

        var sut = new FundInGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        _ = await Assert.ThrowsExceptionAsync<FreeBalanceException>(async () =>
        await sut.GetAsync(query).ConfigureAwait(false))
            .ConfigureAwait(false);

        _ = fakeDistributedLock
            .Received(1)
            .UnlockAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode));
    }
}
