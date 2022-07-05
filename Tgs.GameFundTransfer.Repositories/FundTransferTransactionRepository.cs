using Microsoft.EntityFrameworkCore;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Repositories.Entities;
using Tgs.SerialNumberGenerator;

namespace Tgs.GameFundTransfer.Repositories
{
    internal class FundTransferTransactionRepository : IFundTransferTransactionRepository
    {
        private const string MainWallet = "Default";
        private readonly TgsContext _context;
        private readonly ISerialNumberGenerator _serialNumberGenerator;

        public FundTransferTransactionRepository(TgsContext context, ISerialNumberGenerator serialNumberGenerator)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
        }

        public async ValueTask<Guid> AddFundInAsync(
            Guid gameProviderId,
            string gameProviderCode,
            string playerCode,
            Guid walletId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            var gameProviderFundTransferTransactionId = await _serialNumberGenerator.GenerateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var systemDateTime = DateTime.UtcNow;
            var fundTransferTransaction = new GameProviderFundTransferTransaction()
            {
                GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
                GameProviderId = gameProviderId,
                WalletId = walletId,
                PlayerCode = playerCode,
                FundSource = (short)FundSource.FundIn,
                FromWallet = gameProviderCode,
                ToWallet = MainWallet,
                Amount = amount,
                Status = (short)FundTransferTransactionStatus.FailureOnGameProvider,
                CreatedOn = systemDateTime,
                UpdatedOn = systemDateTime
            };
            _ = await _context.GameProviderFundTransferTransactions
                .AddAsync(
                    entity: fundTransferTransaction,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            _ = await _context
                 .SaveChangesAsync(
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            return gameProviderFundTransferTransactionId;
        }

        public async ValueTask<Guid> AddFundOutAsync(
            Guid gameProviderId,
            string gameProviderCode,
            string playerCode,
            Guid walletId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            var gameProviderFundTransferTransactionId = await _serialNumberGenerator.GenerateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var systemDateTime = DateTime.UtcNow;
            var fundTransferTransaction = new GameProviderFundTransferTransaction()
            {
                GameProviderFundTransferTransactionId = gameProviderFundTransferTransactionId,
                GameProviderId = gameProviderId,
                WalletId = walletId,
                PlayerCode = playerCode,
                FundSource = (short)FundSource.FundOut,
                FromWallet = MainWallet,
                ToWallet = gameProviderCode,
                Amount = amount,
                Status = (short)FundTransferTransactionStatus.FailureOnMainWallet,
                CreatedOn = systemDateTime,
                UpdatedOn = systemDateTime
            };
            _ = await _context.GameProviderFundTransferTransactions
                .AddAsync(
                    entity: fundTransferTransaction,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            _ = await _context
                  .SaveChangesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            return gameProviderFundTransferTransactionId;
        }

        public async ValueTask UpdateByFailureOnGameProviderAsync(
            Guid gameProviderFundTransferTransactionId,
            CancellationToken cancellationToken = default)
        {
            var fundTransferTransaction = await _context.GameProviderFundTransferTransactions
                .Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
                .FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            fundTransferTransaction.TransactionId = null;
            fundTransferTransaction.GameProviderTransactionId = null;
            fundTransferTransaction.Status = (short)FundTransferTransactionStatus.FailureOnGameProvider;
            fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

            _ = _context.GameProviderFundTransferTransactions
               .Update(fundTransferTransaction);

            _ = await _context
                 .SaveChangesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask UpdateByFailureOnMainWalletAsync(
            Guid gameProviderFundTransferTransactionId,
            string gameProviderTransactionId,
            CancellationToken cancellationToken = default)
        {
            var fundTransferTransaction = await _context.GameProviderFundTransferTransactions
                .Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
                .FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            fundTransferTransaction.GameProviderTransactionId = gameProviderTransactionId;
            fundTransferTransaction.Status = (short)FundTransferTransactionStatus.FailureOnMainWallet;
            fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

            _ = _context.GameProviderFundTransferTransactions
               .Update(fundTransferTransaction);

            _ = await _context
                 .SaveChangesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask UpdateBySuccessAsync(
            Guid gameProviderFundTransferTransactionId,
            Guid? transactionsId,
            string gameProviderTransactionId,
            decimal postBalance,
            CancellationToken cancellationToken = default)
        {
            var fundTransferTransaction = await _context.GameProviderFundTransferTransactions
                .Where(x => x.GameProviderFundTransferTransactionId == gameProviderFundTransferTransactionId)
                .FirstAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            fundTransferTransaction.TransactionId = transactionsId;
            fundTransferTransaction.GameProviderTransactionId = gameProviderTransactionId;
            fundTransferTransaction.Status = (short)FundTransferTransactionStatus.Success;
            fundTransferTransaction.Balance = postBalance;
            fundTransferTransaction.UpdatedOn = DateTime.UtcNow;

            _ = _context.GameProviderFundTransferTransactions
               .Update(fundTransferTransaction);

            _ = await _context
                .SaveChangesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
