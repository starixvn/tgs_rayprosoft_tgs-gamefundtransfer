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
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<decimal>(),
                Arg.Any<string>()
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Add_FundOut_Async()
        {
            var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();
            _ = await fakeRepository.AddFundOutAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<decimal>(),
                Arg.Any<string>()
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Update_By_FailureOnGameProvider_Async()
        {
            var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();
            await fakeRepository.UpdateByFailureOnGameProviderAsync(
               Arg.Any<Guid>(),
               Arg.Any<Guid>(),
               Arg.Any<string>(),
               Arg.Any<decimal>(),
               Arg.Any<string>()
               ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Update_By_FailureOnMainWallet_Async()
        {
            var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();
            await fakeRepository.UpdateByFailureOnMainWalletAsync(
               Arg.Any<Guid>(),
               Arg.Any<Guid>(),
               Arg.Any<string>(),
               Arg.Any<decimal>(),
               Arg.Any<string>()
               ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Update_By_Success_Async()
        {
            var fakeRepository = Substitute.For<IFundTransferTransactionRepository>();
            await fakeRepository.UpdateBySuccessAsync(
               Arg.Any<Guid>(),
               Arg.Any<Guid>(),
               Arg.Any<string>(),
               Arg.Any<decimal>(),
               Arg.Any<string>()
               ).ConfigureAwait(false);
        }
    }
}
