using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices
{
    internal class FundOutCommandService : ICommandService<FundOutCommand>
    {
        private readonly ICashServiceClient _cashServiceClient;
        private readonly IFundTransferTransactionRepository _fundTransferTransactionRepository;
        private readonly IGameProvidersClient _gameProvidersClient;
        private readonly ILogger<FundOutCommandService> _logger;

        public FundOutCommandService(
            ICashServiceClient cashServiceClient,
            IFundTransferTransactionRepository fundTransferTransactionRepository,
            IGameProvidersClient gameProvidersClient,
            ILogger<FundOutCommandService> logger)
        {
            _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
            _fundTransferTransactionRepository = fundTransferTransactionRepository ?? throw new ArgumentNullException(nameof(fundTransferTransactionRepository));
            _gameProvidersClient = gameProvidersClient ?? throw new ArgumentNullException(nameof(gameProvidersClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask ExecuteAsync(FundOutCommand query, CancellationToken cancellationToken = default)
        {
            var amount = query.Amount;

            _ = await _gameProvidersClient.PlayerBalanceAsync(
               query.GameProviderCode,
               query.PlayerCode).ConfigureAwait(false);

            var playerBalance = await _cashServiceClient.GetBalanceAsync(query.WalletId, cancellationToken).ConfigureAwait(false);

            if (playerBalance - amount < 0)
            {
                throw new Exception($"Insufficient balance in main wallet.({playerBalance})");
            }

            var gameProviderFundTransferTransactionId = await _fundTransferTransactionRepository.AddFundOutAsync(
                query.GameProviderId,
                query.WalletId,
                query.PlayerCode,
                query.GameProviderCode,
                amount * -1,
                string.Empty)
                .ConfigureAwait(false);

            var transactionResponse = await _cashServiceClient.GameProviderFundOutAsync(
                query.WalletId,
                amount,
                $"【Default】->【{query.GameProviderCode}】",
                gameProviderFundTransferTransactionId.ToString(),
                cancellationToken)
                .ConfigureAwait(false);

            if (transactionResponse == null)
            {
                await _fundTransferTransactionRepository.UpdateByFailureOnMainWalletAsync(
                    gameProviderFundTransferTransactionId,
                    null,
                    string.Empty,
                    -1,
                    string.Empty)
                    .ConfigureAwait(false);
                throw new Exception("The main wallet transfer failed.");
            }

            try
            {
                var gameProviderTransactionId = await _gameProvidersClient.PlayerFundOutAsync(
                    query.GameProviderCode,
                    query.BrandCode,
                    query.PlayerCode,
                    query.CurrencyCode,
                    gameProviderFundTransferTransactionId.ToString("N"),
                    amount)
                    .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(gameProviderTransactionId))
                {
                    throw new Exception($"The game manufacturer's transfer failed..({gameProviderFundTransferTransactionId})");
                }

                await _fundTransferTransactionRepository.UpdateBySuccessAsync(
                    gameProviderFundTransferTransactionId,
                    transactionResponse.TransactionId,
                    gameProviderTransactionId,
                    transactionResponse.Balance,
                    string.Empty
                    ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, "The game manufacturer's transfer failed..({gameProviderFundTransferTransactionId})", gameProviderFundTransferTransactionId);

                await _fundTransferTransactionRepository.UpdateByFailureOnGameProviderAsync(
                    gameProviderFundTransferTransactionId,
                    null,
                    string.Empty,
                    -1,
                    string.Empty).ConfigureAwait(false);
            }
        }
    }
}
