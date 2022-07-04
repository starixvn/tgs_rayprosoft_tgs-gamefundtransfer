using NSubstitute;
using Tgs.GameFundTransfer.Core;

namespace Tgs.GameFundTransfer.Repositories.UnitTest
{
    [TestClass]
    public class FundTransferTransactionRepositoryUnitTest
    {
        [TestMethod]
        public async Task Add_FundIn_Async()
        {
            var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();
            _ = await fakeRepository.AddFundInAsync(
                gameProviderId: Arg.Any<Guid>(),
                gameProviderCode: Arg.Any<string>(),
                playerCode: Arg.Any<string>(),
                walletId: Arg.Any<Guid>(),
                amount: Arg.Any<decimal>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Add_FundOut_Async()
        {
            var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();
            _ = await fakeRepository.AddFundOutAsync(
                gameProviderId: Arg.Any<Guid>(),
                gameProviderCode: Arg.Any<string>(),
                playerCode: Arg.Any<string>(),
                walletId: Arg.Any<Guid>(),
                amount: Arg.Any<decimal>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Update_By_FailureOnGameProvider_Async()
        {
            var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();
            await fakeRepository.UpdateByFailureOnGameProviderAsync(
                gameProviderFundTransferTransactionId: Arg.Any<Guid>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Update_By_FailureOnMainWallet_Async()
        {
            var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();
            await fakeRepository.UpdateByFailureOnMainWalletAsync(
               gameProviderFundTransferTransactionId: Arg.Any<Guid>(),
               gameProviderTransactionId: Arg.Any<string>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Update_By_Success_Async()
        {
            var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();
            await fakeRepository.UpdateBySuccessAsync(
               Arg.Any<Guid>(),
               Arg.Any<Guid>(),
               Arg.Any<string>(),
               Arg.Any<decimal>()).ConfigureAwait(false);
        }
    }
}
