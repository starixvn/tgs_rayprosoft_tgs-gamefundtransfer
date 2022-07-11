using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Client;
using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Core.UnitTest
{
    [TestClass]
    public class FundOutCommandServiceUnitTest
    {
        [TestMethod]
        public async Task Fund_Out()
        {
            var faceLogger = NullLogger<FundOutCommandService>.Instance;
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakeFundsLocked = Substitute.For<IFundsLocked>();
            var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
            var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

            var query = new FundOutCommand(
                Guid.NewGuid(),
                GameProviderType.KY,
                "GPI",
                "4ucmqplxb029",
                Guid.NewGuid(),
                "CNY",
                10m);

            decimal? gameProviderBalance = 0m;
            var balance = 10.0m;
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

            _ = fakeCashServiceClient.GetBalanceAsync(
                Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

            _ = fakeFundTransferTransactionRepository.AddFundOutAsync(
                Arg.Is(query.GameProviderId),
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode),
                Arg.Is(query.WalletId),
                Arg.Is(query.Amount)).Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

            _ = fakeCashServiceClient.GameProviderFundOutAsync(
                 Arg.Is(query.WalletId),
                 Arg.Is(query.Amount),
                 Arg.Is($"【Default】->【{query.GameProviderCode}】"),
                 Arg.Is(gameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(transactionResponse));

            _ = fakeGameProvidersClient.PlayerFundOutAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.BrandCode),
                Arg.Is(query.PlayerCode),
                Arg.Is(query.CurrencyCode),
                Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
                Arg.Is(query.Amount)).Returns(ValueTask.FromResult<string?>(gameProviderTransactionId));

            await fakeFundTransferTransactionRepository.UpdateBySuccessAsync(
                Arg.Is(gameProviderFundTransferTransactionId),
                Arg.Is(transactionResponse.TransactionId),
                Arg.Is(gameProviderTransactionId),
                Arg.Is(transactionResponse.Balance));

            var fakeService = new FundOutCommandService(
                fakeCashServiceClient,
                fakeFundsLocked,
                fakeFundTransferTransactionRepository,
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
            var faceLogger = NullLogger<FundOutCommandService>.Instance;
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakeFundsLocked = Substitute.For<IFundsLocked>();
            var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
            var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

            var query = new FundOutCommand(
                Guid.NewGuid(),
                GameProviderType.KY,
                "GPI",
                "4ucmqplxb029",
                Guid.NewGuid(),
                "CNY",
                10m);

            decimal? gameProviderBalance = 0m;
            var balance = 10.0m;
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

            _ = fakeCashServiceClient.GetBalanceAsync(
                Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

            _ = fakeFundTransferTransactionRepository.AddFundOutAsync(
                Arg.Is(query.GameProviderId),
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode),
                Arg.Is(query.WalletId),
                Arg.Is(query.Amount)).Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

            _ = fakeCashServiceClient.GameProviderFundOutAsync(
                 Arg.Is(query.WalletId),
                 Arg.Is(query.Amount),
                 Arg.Is($"【Default】->【{query.GameProviderCode}】"),
                 Arg.Is(gameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(transactionResponse));

            _ = fakeGameProvidersClient.PlayerFundOutAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.BrandCode),
                Arg.Is(query.PlayerCode),
                Arg.Is(query.CurrencyCode),
                Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
                Arg.Is(query.Amount)).Returns(ValueTask.FromResult<string?>(gameProviderTransactionId));

            await fakeFundTransferTransactionRepository.UpdateBySuccessAsync(
                Arg.Is(gameProviderFundTransferTransactionId),
                Arg.Is(transactionResponse.TransactionId),
                Arg.Is(gameProviderTransactionId),
                Arg.Is(transactionResponse.Balance));

            var fakeService = new FundOutCommandService(
                fakeCashServiceClient,
                fakeFundsLocked,
                fakeFundTransferTransactionRepository,
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
            var faceLogger = NullLogger<FundOutCommandService>.Instance;
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakeFundsLocked = Substitute.For<IFundsLocked>();
            var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
            var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

            var query = new FundOutCommand(
                Guid.NewGuid(),
                GameProviderType.KY,
                "GPI",
                "4ucmqplxb029",
                Guid.NewGuid(),
                "CNY",
                10m);

            decimal? gameProviderBalance = 0m;
            var balance = 10.0m;
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

            _ = fakeCashServiceClient.GetBalanceAsync(
                Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

            _ = fakeFundTransferTransactionRepository.AddFundOutAsync(
                Arg.Is(query.GameProviderId),
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode),
                Arg.Is(query.WalletId),
                Arg.Is(query.Amount)).Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

            _ = fakeCashServiceClient.GameProviderFundOutAsync(
                 Arg.Is(query.WalletId),
                 Arg.Is(query.Amount),
                 Arg.Is($"【Default】->【{query.GameProviderCode}】"),
                 Arg.Is(gameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(transactionResponse));

            _ = fakeGameProvidersClient.PlayerFundOutAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.BrandCode),
                Arg.Is(query.PlayerCode),
                Arg.Is(query.CurrencyCode),
                Arg.Is(gameProviderFundTransferTransactionId.ToString("N")),
                Arg.Is(query.Amount)).Returns(ValueTask.FromResult<string?>(gameProviderTransactionId));

            await fakeFundTransferTransactionRepository.UpdateBySuccessAsync(
                Arg.Is(gameProviderFundTransferTransactionId),
                Arg.Is(transactionResponse.TransactionId),
                Arg.Is(gameProviderTransactionId),
                Arg.Is(transactionResponse.Balance));

            var fakeService = new FundOutCommandService(
                fakeCashServiceClient,
                fakeFundsLocked,
                fakeFundTransferTransactionRepository,
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
        public async Task Insufficient_Balance_In_Main_Wallet()
        {
            var faceLogger = NullLogger<FundOutCommandService>.Instance;
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakeFundsLocked = Substitute.For<IFundsLocked>();
            var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
            var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

            var query = new FundOutCommand(
                Guid.NewGuid(),
                GameProviderType.KY,
                "GPI",
                "4ucmqplxb029",
                Guid.NewGuid(),
                "CNY",
                10m);

            decimal? gameProviderBalance = 0m;
            var balance = 9m;
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

            _ = fakeCashServiceClient.GetBalanceAsync(
                Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

            var fakeService = new FundOutCommandService(
                fakeCashServiceClient,
                fakeFundsLocked,
                fakeFundTransferTransactionRepository,
                fakeGameProvidersClient,
                faceLogger);

            _ = await Assert.ThrowsExceptionAsync<Exception>(() => fakeService.ExecuteAsync(query).AsTask()).ConfigureAwait(false);

            _ = fakeFundsLocked
                .Received(1)
                .UnlockAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode));
        }

        [TestMethod]
        public async Task Main_Wallet_Balance_Is_Below_0_And_The_Refund_Fails()
        {
            var faceLogger = NullLogger<FundOutCommandService>.Instance;
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakeFundsLocked = Substitute.For<IFundsLocked>();
            var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
            var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

            var query = new FundOutCommand(
                Guid.NewGuid(),
                GameProviderType.KY,
                "GPI",
                "4ucmqplxb029",
                Guid.NewGuid(),
                "CNY",
                10m);

            decimal? gameProviderBalance = 0m;
            var balance = 10.0m;
            var gameProviderFundTransferTransactionId = Guid.NewGuid();

            _ = fakeFundsLocked.ExistAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(Task.FromResult(false));

            _ = fakeFundsLocked.LockingAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

            _ = fakeGameProvidersClient.PlayerBalanceAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(ValueTask.FromResult(gameProviderBalance));

            _ = fakeCashServiceClient.GetBalanceAsync(
                Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

            _ = fakeFundTransferTransactionRepository.AddFundOutAsync(
                Arg.Is(query.GameProviderId),
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode),
                Arg.Is(query.WalletId),
                Arg.Is(query.Amount)).Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

            var transactionResponse = new TransactionResponse()
            {
                TransactionId = Guid.NewGuid(),
                Balance = -10
            };
            _ = fakeCashServiceClient.GameProviderFundOutAsync(
                 Arg.Is(query.WalletId),
                 Arg.Is(query.Amount),
                 Arg.Is($"【Default】->【{query.GameProviderCode}】"),
                 Arg.Is(gameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(transactionResponse));

            var fakeService = new FundOutCommandService(
                fakeCashServiceClient,
                fakeFundsLocked,
                fakeFundTransferTransactionRepository,
                fakeGameProvidersClient,
                faceLogger);

            await fakeService.ExecuteAsync(query).ConfigureAwait(false);

            _ = fakeCashServiceClient
                .Received(1)
                .GameProviderFundRefundAsync(
                 Arg.Is(query.WalletId),
                 Arg.Is(query.Amount),
                 Arg.Is(transactionResponse.TransactionId.ToString()),
                 Arg.Is(gameProviderFundTransferTransactionId.ToString()));

            _ = fakeFundTransferTransactionRepository
                .Received(1)
                .UpdateByMainWalletFundRefundFailedAsync(
                    Arg.Is(gameProviderFundTransferTransactionId),
                    Arg.Any<string>());

            _ = fakeFundsLocked
                .Received(1)
                .UnlockAsync(
                    Arg.Is(query.GameProviderCode),
                    Arg.Is(query.PlayerCode));
        }

        [TestMethod]
        public async Task Main_Wallet_Balance_Is_Below_0_And_The_Refund_Successful()
        {
            var faceLogger = NullLogger<FundOutCommandService>.Instance;
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakeFundsLocked = Substitute.For<IFundsLocked>();
            var fakeFundTransferTransactionRepository = Substitute.For<IFundTransferTransactionRepository>();
            var fakeGameProvidersClient = Substitute.For<IGameProvidersClient>();

            var query = new FundOutCommand(
                Guid.NewGuid(),
                GameProviderType.KY,
                "GPI",
                "4ucmqplxb029",
                Guid.NewGuid(),
                "CNY",
                10m);

            decimal? gameProviderBalance = 0m;
            var balance = 10.0m;
            var gameProviderFundTransferTransactionId = Guid.NewGuid();

            _ = fakeFundsLocked.ExistAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(Task.FromResult(false));

            _ = fakeFundsLocked.LockingAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(Task.FromResult(true));

            _ = fakeGameProvidersClient.PlayerBalanceAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(ValueTask.FromResult(gameProviderBalance));

            _ = fakeCashServiceClient.GetBalanceAsync(
                Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

            _ = fakeFundTransferTransactionRepository.AddFundOutAsync(
                Arg.Is(query.GameProviderId),
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode),
                Arg.Is(query.WalletId),
                Arg.Is(query.Amount)).Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

            var transactionResponse = new TransactionResponse()
            {
                TransactionId = Guid.NewGuid(),
                Balance = -10
            };

            _ = fakeCashServiceClient.GameProviderFundOutAsync(
                 Arg.Is(query.WalletId),
                 Arg.Is(query.Amount),
                 Arg.Is($"【Default】->【{query.GameProviderCode}】"),
                 Arg.Is(gameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(transactionResponse));

            var fundRefundResponse = new TransactionResponse()
            {
                TransactionId = Guid.NewGuid(),
                Balance = 0
            };
            _ = fakeCashServiceClient.GameProviderFundRefundAsync(
                 Arg.Is(query.WalletId),
                 Arg.Is(query.Amount),
                 Arg.Is(transactionResponse.TransactionId.ToString()),
                 Arg.Is(gameProviderFundTransferTransactionId.ToString())).Returns(ValueTask.FromResult(fundRefundResponse));

            var fakeService = new FundOutCommandService(
                fakeCashServiceClient,
                fakeFundsLocked,
                fakeFundTransferTransactionRepository,
                fakeGameProvidersClient,
                faceLogger);

            await fakeService.ExecuteAsync(query).ConfigureAwait(false);

            _ = fakeFundTransferTransactionRepository.Received(1)
                .UpdateByMainWalletFundRefundSuccessAsync(
                    Arg.Is(gameProviderFundTransferTransactionId),
                    Arg.Is(fundRefundResponse.TransactionId),
                    Arg.Is(fundRefundResponse.Balance),
                    Arg.Any<string>());

            _ = fakeFundsLocked
                .Received(1)
                .UnlockAsync(
                    Arg.Is(query.GameProviderCode),
                    Arg.Is(query.PlayerCode));
        }
    }
}
