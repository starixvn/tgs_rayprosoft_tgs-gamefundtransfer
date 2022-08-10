using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices
{
    internal class FundInCommandService : ICommandService<FundInCommand>
    {
        private readonly ICashServiceClient _cashServiceClient;
        private readonly IFundsLocked _fundsLocked;
        private readonly IFundTransferTransactionRepository _fundTransferTransactionRepository;
        private readonly IGameCurrencyClient _gameCurrencyClient;
        private readonly IGameProvidersClient _gameProvidersClient;
        private readonly ILogger<FundInCommandService> _logger;

        public FundInCommandService(
            ILogger<FundInCommandService> logger,
            ICashServiceClient cashServiceClient,
            IFundsLocked fundsLocked,
            IFundTransferTransactionRepository fundTransferTransactionRepository,
            IGameCurrencyClient gameCurrencyClient,
            IGameProvidersClient gameProvidersClient)
        {
            _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
            _fundsLocked = fundsLocked ?? throw new ArgumentNullException(nameof(fundsLocked));
            _fundTransferTransactionRepository = fundTransferTransactionRepository ?? throw new ArgumentNullException(nameof(fundTransferTransactionRepository));
            _gameProvidersClient = gameProvidersClient ?? throw new ArgumentNullException(nameof(gameProvidersClient));
            _gameCurrencyClient = gameCurrencyClient ?? throw new ArgumentNullException(nameof(gameCurrencyClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask ExecuteAsync(FundInCommand query, CancellationToken cancellationToken = default)
        {
            var exchangeRateFundResult = await _gameCurrencyClient.ConvertGameProviderFundsAsync(
                query.GameProviderCode,
                query.CurrencyCode,
                query.Amount,
                cancellationToken).ConfigureAwait(false);

            var gameProviderAmount = exchangeRateFundResult.Amount;

            if (await _fundsLocked.ExistAsync(
                    gameProviderCode: query.GameProviderCode,
                    playerCode: query.PlayerCode).ConfigureAwait(false))
            {
                _logger.LogWarning("Player ({playerCode}) funds transfer game provider ({gameProviderCode}) has been locked.", query.PlayerCode, query.GameProviderCode);
                return;
            }

            if (!await _fundsLocked.LockingAsync(
                    gameProviderCode: query.GameProviderCode,
                    playerCode: query.PlayerCode).ConfigureAwait(false))

            {
                _logger.LogWarning("Player ({playerCode}) funds transfer game provider ({gameProviderCode}) has been locked.", query.PlayerCode, query.GameProviderCode);
                return;
            }

            try
            {
                var gameProviderBalance = await _gameProvidersClient.PlayerBalanceAsync(
                        gameProviderCode: query.GameProviderCode,
                        playerCode: query.PlayerCode,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                if (gameProviderBalance - gameProviderAmount < 0)
                {
                    throw new Exception($"Insufficient game wallet balance.({gameProviderBalance})");
                }

                var gameProviderFundTransferTransactionId = await _fundTransferTransactionRepository.AddFundInAsync(
                        gameProviderId: query.GameProviderId,
                        gameProviderCode: query.GameProviderCode,
                        playerCode: query.PlayerCode,
                        walletId: query.WalletId,
                        amount: query.Amount,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                var gameProviderTransactionId = await _gameProvidersClient.PlayerFundInAsync(
                        gameProviderCode: query.GameProviderCode,
                        brandCode: query.BrandCode,
                        playerCode: query.PlayerCode,
                        currencyCode: query.CurrencyCode,
                        orderId: gameProviderFundTransferTransactionId.ToString("N"),
                        amount: gameProviderAmount,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(gameProviderTransactionId))
                {
                    await _fundTransferTransactionRepository.UpdateByGameProviderFundFailedAsync(
                            gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                    throw new Exception("The game manufacturer's transfer failed.");
                }

                try
                {
                    var transactionResponse = await _cashServiceClient.GameProviderFundInAsync(
                            query.WalletId,
                            query.Amount,
                            $"【{query.GameProviderCode}】->【Default】",
                            gameProviderFundTransferTransactionId.ToString(),
                            cancellationToken).ConfigureAwait(false);

                    await _fundTransferTransactionRepository.UpdateBySuccessAsync(
                            gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                            transactionId: transactionResponse.TransactionId,
                            gameProviderTransactionId: gameProviderTransactionId,
                            balance: transactionResponse.Balance,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await _fundTransferTransactionRepository.UpdateByMainWalletFundFailedAsync(
                            gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                            gameProviderTransactionId: gameProviderTransactionId,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                    _logger.LogError(exception: ex, "The main wallet transfer failed.({gameProviderFundTransferTransactionId})", gameProviderFundTransferTransactionId);
                }
            }
            finally
            {
                await _fundsLocked.UnlockAsync(
                      gameProviderCode: query.GameProviderCode,
                      playerCode: query.PlayerCode).ConfigureAwait(false);
            }
        }
    }
}
