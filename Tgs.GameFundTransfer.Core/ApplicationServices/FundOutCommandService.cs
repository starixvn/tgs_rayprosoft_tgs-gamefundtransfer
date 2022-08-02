using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices
{
    internal class FundOutCommandService : ICommandService<FundOutCommand>
    {
        private readonly ICashServiceClient _cashServiceClient;
        private readonly IFundsLocked _fundsLocked;
        private readonly IFundTransferTransactionRepository _fundTransferTransactionRepository;
        private readonly IGameCurrencyClient _gameCurrencyClient;
        private readonly IGameProvidersClient _gameProvidersClient;
        private readonly ILogger<FundOutCommandService> _logger;

        public FundOutCommandService(
            ILogger<FundOutCommandService> logger,
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

        public async ValueTask ExecuteAsync(FundOutCommand query, CancellationToken cancellationToken = default)
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
                _ = await _gameProvidersClient.PlayerBalanceAsync(
                        gameProviderCode: query.GameProviderCode,
                        playerCode: query.PlayerCode,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                var playerBalance = await _cashServiceClient.GetBalanceAsync(
                        walletId: query.WalletId,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                if (playerBalance - query.Amount < 0)
                {
                    throw new Exception($"Insufficient balance in main wallet.({playerBalance})");
                }

                var gameProviderFundTransferTransactionId = await _fundTransferTransactionRepository.AddFundOutAsync(
                        gameProviderId: query.GameProviderId,
                        gameProviderCode: query.GameProviderCode,
                        playerCode: query.PlayerCode,
                        walletId: query.WalletId,
                        amount: query.Amount,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                var transactionResponse = await _cashServiceClient.GameProviderFundOutAsync(
                        walletId: query.WalletId,
                        amount: query.Amount,
                        description: $"【Default】->【{query.GameProviderCode}】",
                        externalId: gameProviderFundTransferTransactionId.ToString(),
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                if (transactionResponse == null)
                {
                    await _fundTransferTransactionRepository.UpdateByMainWalletFundFailedAsync(
                        gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                        gameProviderTransactionId: string.Empty,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    throw new Exception("The main wallet transfer failed.");
                }

                if (transactionResponse.Balance < 0)
                {
                    var description = $"The main wallet balance ({transactionResponse.Balance}) is negative to perform this transaction transaction id ({transactionResponse.TransactionId}) refund.";

                    try
                    {
                        var fundRefundResponse = await _cashServiceClient.GameProviderFundRefundAsync(
                                walletId: query.WalletId,
                                amount: query.Amount,
                                description: transactionResponse.TransactionId.ToString(),
                                externalId: gameProviderFundTransferTransactionId.ToString(),
                                cancellationToken: cancellationToken).ConfigureAwait(false);

                        await _fundTransferTransactionRepository.UpdateByMainWalletFundRefundSuccessAsync(
                                gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                                transactionId: fundRefundResponse.TransactionId,
                                balance: fundRefundResponse.Balance,
                                description: description,
                                cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(exception: ex, "This transaction id({transactionId}) main wallet fund refund failed.", transactionResponse.TransactionId);

                        await _fundTransferTransactionRepository.UpdateByMainWalletFundRefundFailedAsync(
                                gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                                description: description,
                                cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        _logger.LogWarning("The main wallet balance ({balance}) is negative to perform this transaction transaction id ({transactionId}) refund.", transactionResponse.Balance, transactionResponse.TransactionId);
                    }
                    return;
                }

                try
                {
                    var gameProviderTransactionId = await _gameProvidersClient.PlayerFundOutAsync(
                            gameProviderCode: query.GameProviderCode,
                            brandCode: query.BrandCode,
                            playerCode: query.PlayerCode,
                            currencyCode: query.CurrencyCode,
                            orderId: gameProviderFundTransferTransactionId.ToString("N"),
                            amount: gameProviderAmount,
                            cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(gameProviderTransactionId))
                    {
                        throw new Exception($"The game manufacturer's transfer failed.({gameProviderFundTransferTransactionId})");
                    }

                    await _fundTransferTransactionRepository.UpdateBySuccessAsync(
                            gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                            transactionId: transactionResponse.TransactionId,
                            gameProviderTransactionId: gameProviderTransactionId,
                            balance: transactionResponse.Balance,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(exception: ex, "The game manufacturer's transfer failed.({gameProviderFundTransferTransactionId})", gameProviderFundTransferTransactionId);

                    await _fundTransferTransactionRepository.UpdateByGameProviderFundFailedAsync(
                            gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
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
