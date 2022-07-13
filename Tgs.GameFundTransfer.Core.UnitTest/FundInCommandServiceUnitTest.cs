using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Core.UnitTest
{
    [TestClass]
    public class FundInCommandServiceUnitTest
    {
        [TestMethod]
        public async Task Fund_In()
        {
            var faceLogger = NullLogger<FundInCommandService>.Instance;
            var fakeFundsLocked = Substitute.For<IFundsLocked>();
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
            var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

            var query = new FundInCommand(
                Guid.NewGuid(),
                GameProviderType.KY,
                "GPI",
                "4ucmqplxb029",
                Guid.NewGuid(),
                "CNY",
                10m
                );

            decimal? gameProviderBalance = 10.0m;
            var gameProviderFundTransferTransactionId = Guid.NewGuid();
            var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

            var transactionResponse = new TransactionResponse()
            {
                TransactionId = Guid.NewGuid(),
                Balance = 10
            };

            _ = fakeFundsLocked.ExistAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(Task.FromResult(false));

            _ = fakeFundsLocked.LockingAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

            _ = fakeGameProvidersClient.PlayerBalanceAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(ValueTask.FromResult(gameProviderBalance));

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
                Arg.Is(query.Amount)).Returns(ValueTask.FromResult<string?>(gameProviderTransactionId));

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
                fakeCashServiceClient,
                fakeFundsLocked,
                fakeGameProviderFundTransferTransactionRepository,
                fakeGameProvidersClient,
                faceLogger);

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
            var fakeFundsLocked = Substitute.For<IFundsLocked>();
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
            var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

            var query = new FundInCommand(
                Guid.NewGuid(),
                GameProviderType.KY,
                "GPI",
                "4ucmqplxb029",
                Guid.NewGuid(),
                "CNY",
                10m
                );

            decimal? gameProviderBalance = 10.0m;
            var gameProviderFundTransferTransactionId = Guid.NewGuid();
            var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

            var transactionResponse = new TransactionResponse()
            {
                TransactionId = Guid.NewGuid(),
                Balance = 10
            };

            _ = fakeFundsLocked.ExistAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(Task.FromResult(false));

            _ = fakeFundsLocked.LockingAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(Task.FromResult(false));

            _ = fakeGameProvidersClient.PlayerBalanceAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(ValueTask.FromResult(gameProviderBalance));

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
                Arg.Is(query.Amount)).Returns(ValueTask.FromResult<string?>(gameProviderTransactionId));

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
                fakeCashServiceClient,
                fakeFundsLocked,
                fakeGameProviderFundTransferTransactionRepository,
                fakeGameProvidersClient,
                faceLogger);

            await fakeService.ExecuteAsync(query).ConfigureAwait(false);

            _ = fakeFundsLocked
                .Received(0)
                .UnlockAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode));
        }

        [TestMethod]
        public async Task Funds_Transfer_Exists()
        {
            var faceLogger = NullLogger<FundInCommandService>.Instance;
            var fakeFundsLocked = Substitute.For<IFundsLocked>();
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
            var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

            var query = new FundInCommand(
                Guid.NewGuid(),
                GameProviderType.KY,
                "GPI",
                "4ucmqplxb029",
                Guid.NewGuid(),
                "CNY",
                10m
                );

            decimal? gameProviderBalance = 10.0m;
            var gameProviderFundTransferTransactionId = Guid.NewGuid();
            var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

            var transactionResponse = new TransactionResponse()
            {
                TransactionId = Guid.NewGuid(),
                Balance = 10
            };

            _ = fakeFundsLocked.ExistAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

            _ = fakeGameProvidersClient.PlayerBalanceAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(ValueTask.FromResult(gameProviderBalance));

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
                Arg.Is(query.Amount)).Returns(ValueTask.FromResult<string?>(gameProviderTransactionId));

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
                fakeCashServiceClient,
                fakeFundsLocked,
                fakeGameProviderFundTransferTransactionRepository,
                fakeGameProvidersClient,
                faceLogger);

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
            var fakeFundsLocked = Substitute.For<IFundsLocked>();
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakeGameProviderFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
            var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

            var query = new FundInCommand(
                Guid.NewGuid(),
                GameProviderType.KY,
                "GPI",
                "4ucmqplxb029",
                Guid.NewGuid(),
                "CNY",
                10m
                );

            decimal? gameProviderBalance = 9m;
            var gameProviderFundTransferTransactionId = Guid.NewGuid();
            var gameProviderTransactionId = $"72412{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+8)):yyyyMMddHHmmssfff}4udwnknl3eg7";

            var transactionResponse = new TransactionResponse()
            {
                TransactionId = Guid.NewGuid(),
                Balance = 10
            };

            _ = fakeFundsLocked.ExistAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(Task.FromResult(false));

            _ = fakeFundsLocked.LockingAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

            _ = fakeGameProvidersClient.PlayerBalanceAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(ValueTask.FromResult(gameProviderBalance));

            var fakeService = new FundInCommandService(
                fakeCashServiceClient,
                fakeFundsLocked,
                fakeGameProviderFundTransferTransactionRepository,
                fakeGameProvidersClient,
                faceLogger);

            _ = await Assert.ThrowsExceptionAsync<Exception>(() => fakeService.ExecuteAsync(query).AsTask()).ConfigureAwait(false);

            _ = fakeFundsLocked
                .Received(1)
                .UnlockAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode));
        }
    }
}
