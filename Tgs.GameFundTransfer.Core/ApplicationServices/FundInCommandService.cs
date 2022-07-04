using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices
{
    internal class FundInCommandService : ICommandService<FundInCommand>
    {
        private readonly ICashServiceClient _cashServiceClient;
        private readonly IFundTransferTransactionRepository _fundTransferTransactionRepository;
        private readonly IGameProvidersClient _gameProvidersClient;
        private readonly ILogger<FundInCommandService> _logger;

        public FundInCommandService(
            ICashServiceClient cashServiceClient,
            IFundTransferTransactionRepository fundTransferTransactionRepository,
            IGameProvidersClient gameProvidersClient,
            ILogger<FundInCommandService> logger)
        {
            _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
            _fundTransferTransactionRepository = fundTransferTransactionRepository ?? throw new ArgumentNullException(nameof(fundTransferTransactionRepository));
            _gameProvidersClient = gameProvidersClient ?? throw new ArgumentNullException(nameof(gameProvidersClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask ExecuteAsync(FundInCommand query, CancellationToken cancellationToken = default)
        {
            var amount = query.Amount;

            var gameProviderBalance = await _gameProvidersClient.PlayerBalanceAsync(
                query.GameProviderCode,
                query.PlayerCode).ConfigureAwait(false);

            if (gameProviderBalance - amount < 0)
            {
                throw new Exception($"Insufficient game wallet balance.({gameProviderBalance})");
            }

            var gameProviderFundTransferTransactionId = await _fundTransferTransactionRepository.AddFundInAsync(
                query.GameProviderId,
                query.WalletId,
                query.PlayerCode,
                query.GameProviderCode,
                amount,
                string.Empty)
                .ConfigureAwait(false);

            var gameProviderTransactionId = await _gameProvidersClient.PlayerFundInAsync(
                query.GameProviderCode,
                query.BrandCode,
                query.PlayerCode,
                query.CurrencyCode,
                gameProviderFundTransferTransactionId.ToString("N"),
                amount)
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(gameProviderTransactionId))
            {
                await _fundTransferTransactionRepository.UpdateByFailureOnGameProviderAsync(
                    gameProviderFundTransferTransactionId,
                    null,
                    string.Empty,
                    -1,
                    string.Empty
                     )
                    .ConfigureAwait(false);
                throw new Exception("The game manufacturer's transfer failed.");
            }

            try
            {
                var transactionResponse = await _cashServiceClient.GameProviderFundInAsync(
                      query.WalletId,
                      amount,
                      $"【{query.GameProviderCode}】->【Default】",
                      gameProviderFundTransferTransactionId.ToString(),
                      cancellationToken)
                      .ConfigureAwait(false);

                await _fundTransferTransactionRepository.UpdateBySuccessAsync(
                gameProviderFundTransferTransactionId,
                transactionResponse.TransactionId,
                gameProviderTransactionId,
                transactionResponse.Balance,
                string.Empty
                )
                .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _fundTransferTransactionRepository.UpdateByFailureOnMainWalletAsync(
                    gameProviderFundTransferTransactionId,
                    null,
                    gameProviderTransactionId,
                    -1,
                    string.Empty).ConfigureAwait(false);
                _logger.LogError(exception: ex, "The main wallet transfer failed.({gameProviderFundTransferTransactionId})", gameProviderFundTransferTransactionId);
            }
        }
    }
}
