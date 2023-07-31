using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Core.UnitTest;

[TestClass]
public class FundInZeroGetServiceUnitTest
{
    [TestMethod]
    public async Task Fund_In()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInZeroQuery(
            Guid.NewGuid(),
            GameProviderType.Kaiyuan,
            "GPI",
            "4ucmqplxb029",
            Guid.NewGuid(),
            "CNY");

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

        _ = fakeDistributedLock
            .LockingAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                playerCode: Arg.Is(query.PlayerCode))
            .Returns(Task.FromResult(true));

        var gameProviderBalance = new PlayerBalanceResponse(
            Balance: 100.0m,
            FreeBalance: 10.0m,
            OutstandingBalance: 90m);
        _ = fakeGameProvidersClient
            .PlayerBalanceAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode))
            .Returns(ValueTask.FromResult(gameProviderBalance));

        var convertPlatformBalanceResult = new ConvertPlatformBalanceResult(
            ExchangeRate: 1,
            Balance: gameProviderBalance.FreeBalance);
        _ = fakeGameCurrencyClient
            .ConvertPlatformBalanceAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                currencyCode: Arg.Is(query.CurrencyCode),
                balance: Arg.Is(gameProviderBalance.FreeBalance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(convertPlatformBalanceResult);

        _ = fakeRepository
            .AddFundInAsync(
                gameProviderId: Arg.Is(query.GameProviderId),
                gameProviderCode: Arg.Is(query.GameProviderCode),
                playerCode: Arg.Is(query.PlayerCode),
                walletId: Arg.Is(query.WalletId),
                amount: Arg.Is(convertPlatformBalanceResult.Balance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

        _ = fakeGameProvidersClient
            .PlayerFundInAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                brandCode: Arg.Is(query.BrandCode),
                playerCode: Arg.Is(query.PlayerCode),
                currencyCode: Arg.Is(query.CurrencyCode),
                orderId: Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
                amount: Arg.Is(gameProviderBalance.FreeBalance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(gameProviderTransactionId));

        var transactionResponse = new TransactionResponse()
        {
            TransactionId = Guid.NewGuid(),
            Balance = 10
        };
        _ = fakeCashServiceClient
            .GameProviderFundInAsync(
                walletId: Arg.Is(query.WalletId),
                amount: Arg.Is(convertPlatformBalanceResult.Balance),
                description: Arg.Is($"【{query.GameProviderCode}】->【Default】"),
                externalId: Arg.Is(gameProviderFundTransferTransactionId.ToString()),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(transactionResponse));

        _ = fakeRepository
            .UpdateBySuccessAsync(
                gameProviderFundTransferTransactionId: Arg.Is(gameProviderFundTransferTransactionId),
                transactionId: Arg.Is(transactionResponse.TransactionId),
                gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
                balance: Arg.Is(transactionResponse.Balance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var fakeService = new FundInZeroGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        var actual = await fakeService
            .GetAsync(
                query)
            .ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.AreEqual(StateCode.Success, actual.State);

        _ = fakeDistributedLock
            .Received(1)
            .UnlockAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                playerCode: Arg.Is(query.PlayerCode));
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

        var query = new FundInZeroQuery(
            Guid.NewGuid(),
            GameProviderType.Kaiyuan,
            "GPI",
            "4ucmqplxb029",
            Guid.NewGuid(),
            "CNY");

        _ = fakeDistributedLock
            .LockingAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                playerCode: Arg.Is(query.PlayerCode))
            .Returns(Task.FromResult(false));

        var fakeService = new FundInZeroGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        var actual = await fakeService
            .GetAsync(
                query)
            .ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.AreEqual(StateCode.TransactionLock, actual.State);

        _ = fakeDistributedLock
            .Received(0)
            .UnlockAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                playerCode: Arg.Is(query.PlayerCode));
    }

    [TestMethod]
    public async Task Platform_Currency_IDR_1000_FundIn_Game_Provider_IDR_Amount_1()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInZeroQuery(
            Guid.NewGuid(),
            GameProviderType.Kaiyuan,
            "GPI",
            "4ucmqplxb029",
            Guid.NewGuid(),
            "IDR_1000");

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

        _ = fakeDistributedLock
            .LockingAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode))
            .Returns(Task.FromResult(true));

        var gameProviderBalance = new PlayerBalanceResponse(
            Balance: 10.0m,
            FreeBalance: 1.0m,
            OutstandingBalance: 9m);
        _ = fakeGameProvidersClient
            .PlayerBalanceAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode))
            .Returns(ValueTask.FromResult(gameProviderBalance));

        var convertPlatformBalanceResult = new ConvertPlatformBalanceResult(
            ExchangeRate: 0.001m,
            Balance: 1);
        _ = fakeGameCurrencyClient
            .ConvertPlatformBalanceAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.CurrencyCode),
                Arg.Is(gameProviderBalance.FreeBalance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(convertPlatformBalanceResult);

        _ = fakeRepository
            .AddFundInAsync(
                gameProviderId: Arg.Is(query.GameProviderId),
                gameProviderCode: Arg.Is(query.GameProviderCode),
                playerCode: Arg.Is(query.PlayerCode),
                walletId: Arg.Is(query.WalletId),
                amount: Arg.Is(convertPlatformBalanceResult.Balance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

        _ = fakeGameProvidersClient
            .PlayerFundInAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                brandCode: Arg.Is(query.BrandCode),
                playerCode: Arg.Is(query.PlayerCode),
                currencyCode: Arg.Is(query.CurrencyCode),
                orderId: Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
                amount: Arg.Is(gameProviderBalance.FreeBalance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(gameProviderTransactionId));

        var transactionResponse = new TransactionResponse()
        {
            TransactionId = Guid.NewGuid(),
            Balance = 1000
        };
        _ = fakeCashServiceClient
            .GameProviderFundInAsync(
                Arg.Is(query.WalletId),
                Arg.Is(convertPlatformBalanceResult.Balance),
                Arg.Is($"【{query.GameProviderCode}】->【Default】"),
                Arg.Is(gameProviderFundTransferTransactionId.ToString()),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(transactionResponse));

        _ = fakeRepository
            .UpdateBySuccessAsync(
                gameProviderFundTransferTransactionId: Arg.Is(gameProviderFundTransferTransactionId),
                transactionId: Arg.Is(transactionResponse.TransactionId),
                gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
                balance: Arg.Is(transactionResponse.Balance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var fakeService = new FundInZeroGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        var actual = await fakeService
            .GetAsync(
                query)
            .ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.AreEqual(StateCode.Success, actual.State);

        _ = fakeDistributedLock
            .Received(1)
            .UnlockAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                playerCode: Arg.Is(query.PlayerCode));
    }

    [TestMethod]
    public async Task Platform_Currency_IDR_FundIn_Game_Provider_IDR_1000_Amount_1()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInZeroQuery(
            Guid.NewGuid(),
            GameProviderType.Kaiyuan,
            "GPI",
            "4ucmqplxb029",
            Guid.NewGuid(),
            "IDR");

        var gameProviderFundTransferTransactionId = Guid.NewGuid();
        var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

        _ = fakeDistributedLock
            .LockingAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode))
            .Returns(Task.FromResult(true));

        var gameProviderBalance = new PlayerBalanceResponse(
            Balance: 10.0m,
            FreeBalance: 1.0m,
            OutstandingBalance: 9.0m);
        _ = fakeGameProvidersClient
            .PlayerBalanceAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode))
            .Returns(ValueTask.FromResult(gameProviderBalance));

        var convertPlatformBalanceResult = new ConvertPlatformBalanceResult(
            ExchangeRate: 1000,
            Balance: 1000);
        _ = fakeGameCurrencyClient
            .ConvertPlatformBalanceAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.CurrencyCode),
                Arg.Is(gameProviderBalance.FreeBalance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(convertPlatformBalanceResult);

        _ = fakeRepository
            .AddFundInAsync(
                gameProviderId: Arg.Is(query.GameProviderId),
                gameProviderCode: Arg.Is(query.GameProviderCode),
                playerCode: Arg.Is(query.PlayerCode),
                walletId: Arg.Is(query.WalletId),
                amount: Arg.Is(convertPlatformBalanceResult.Balance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

        _ = fakeGameProvidersClient
            .PlayerFundInAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                brandCode: Arg.Is(query.BrandCode),
                playerCode: Arg.Is(query.PlayerCode),
                currencyCode: Arg.Is(query.CurrencyCode),
                orderId: Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
                amount: Arg.Is(gameProviderBalance.FreeBalance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(gameProviderTransactionId));

        var transactionResponse = new TransactionResponse()
        {
            TransactionId = Guid.NewGuid(),
            Balance = 1000
        };
        _ = fakeCashServiceClient
            .GameProviderFundInAsync(
                Arg.Is(query.WalletId),
                Arg.Is(convertPlatformBalanceResult.Balance),
                Arg.Is($"【{query.GameProviderCode}】->【Default】"),
                Arg.Is(gameProviderFundTransferTransactionId.ToString()),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(transactionResponse));

        _ = fakeRepository
            .UpdateBySuccessAsync(
                gameProviderFundTransferTransactionId: Arg.Is(gameProviderFundTransferTransactionId),
                transactionId: Arg.Is(transactionResponse.TransactionId),
                gameProviderTransactionId: Arg.Is(gameProviderTransactionId),
                balance: Arg.Is(transactionResponse.Balance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var fakeService = new FundInZeroGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        var actual = await fakeService
            .GetAsync(
                query)
            .ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.AreEqual(StateCode.Success, actual.State);

        _ = fakeDistributedLock
            .Received(1)
            .UnlockAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                playerCode: Arg.Is(query.PlayerCode));
    }

    [TestMethod]
    public async Task This_Player_Has_Acquired_A_Game_Provider_Less_Than_Or_Equal_To_0()
    {
        var faceLogger = NullLogger<FundInGetService>.Instance;
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeDistributedLock = Substitute.For<IFundsDistributedLock>();
        var fakeGameCurrencyClient = Substitute.For<IGameCurrencyClient>();
        var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();
        var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();

        var query = new FundInZeroQuery(
            Guid.NewGuid(),
            GameProviderType.Kaiyuan,
            "GPI",
            "4ucmqplxb029",
            Guid.NewGuid(),
            "CNY");

        _ = fakeDistributedLock
            .LockingAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                playerCode: Arg.Is(query.PlayerCode))
            .Returns(Task.FromResult(true));

        var gameProviderBalance = new PlayerBalanceResponse(
            Balance: 10m,
            FreeBalance: 0m,
            OutstandingBalance: 10m);
        _ = fakeGameProvidersClient
            .PlayerBalanceAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode))
            .Returns(ValueTask.FromResult(gameProviderBalance));

        var convertPlatformBalanceResult = new ConvertPlatformBalanceResult(
            ExchangeRate: 1,
            Balance: gameProviderBalance.FreeBalance);
        _ = fakeGameCurrencyClient
            .ConvertPlatformBalanceAsync(
                gameProviderCode: Arg.Is(query.GameProviderCode),
                currencyCode: Arg.Is(query.CurrencyCode),
                balance: Arg.Is(gameProviderBalance.FreeBalance),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(convertPlatformBalanceResult);

        var fakeService = new FundInZeroGetService(
            faceLogger,
            fakeCashServiceClient,
            fakeDistributedLock,
            fakeGameCurrencyClient,
            fakeGameProvidersClient,
            fakeRepository);

        var actual = await fakeService.GetAsync(query).ConfigureAwait(false);

        Assert.IsNotNull(actual);
        Assert.AreEqual(StateCode.Success, actual.State);

        _ = fakeRepository
            .Received(0)
            .AddFundInAsync(
                gameProviderId: Arg.Any<Guid>(),
                gameProviderCode: Arg.Any<string>(),
                playerCode: Arg.Any<string>(),
                walletId: Arg.Any<Guid>(),
                amount: Arg.Any<decimal>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakeDistributedLock
            .Received(1)
            .UnlockAsync(
            Arg.Is(query.GameProviderCode),
            Arg.Is(query.PlayerCode));
    }
}
