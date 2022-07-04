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

            _ = fakeGameProvidersClient.PlayerBalanceAsync(
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.PlayerCode)).Returns(ValueTask.FromResult(gameProviderBalance));

            _ = fakeCashServiceClient.GetBalanceAsync(
                Arg.Is(query.WalletId)).Returns(ValueTask.FromResult(balance));

            _ = fakeFundTransferTransactionRepository.AddFundOutAsync(
                Arg.Is(query.GameProviderId),
                Arg.Is(query.WalletId),
                Arg.Is(query.PlayerCode),
                Arg.Is(query.GameProviderCode),
                Arg.Is(query.Amount * -1),
                Arg.Is(string.Empty)).Returns(ValueTask.FromResult(gameProviderFundTransferTransactionId));

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
                Arg.Is(transactionResponse.Balance),
                Arg.Is(string.Empty)
                );

            var fakeService = new FundOutCommandService(
                fakeCashServiceClient,
                fakeFundTransferTransactionRepository,
                fakeGameProvidersClient,
                faceLogger);

            await fakeService.ExecuteAsync(query).ConfigureAwait(false);
        }
    }
}
