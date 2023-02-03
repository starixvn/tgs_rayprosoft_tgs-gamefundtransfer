using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.Extensions;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundInGetService : IGetService<FundInQuery, FundResult>
{
    private readonly ICashServiceClient _cashServiceClient;
    private readonly IFundsDistributedLock _distributedLock;
    private readonly IGameCurrencyClient _gameCurrencyClient;
    private readonly IGameProvidersClient _gameProvidersClient;
    private readonly ILogger<FundInGetService> _logger;
    private readonly IFundTransferTransactionRepository _repository;

    public FundInGetService(
        ILogger<FundInGetService> logger,
        ICashServiceClient cashServiceClient,
        IFundsDistributedLock distributedLock,
        IGameCurrencyClient gameCurrencyClient,
        IGameProvidersClient gameProvidersClient,
        IFundTransferTransactionRepository repository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
        _distributedLock = distributedLock ?? throw new ArgumentNullException(nameof(distributedLock));
        _gameCurrencyClient = gameCurrencyClient ?? throw new ArgumentNullException(nameof(gameCurrencyClient));
        _gameProvidersClient = gameProvidersClient ?? throw new ArgumentNullException(nameof(gameProvidersClient));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async ValueTask<FundResult> GetAsync(FundInQuery query, CancellationToken cancellationToken = default)
    {
        var exchangeRateFundResult = await _gameCurrencyClient
            .ConvertGameProviderFundsAsync(
                query.GameProviderCode,
                query.CurrencyCode,
                query.Amount,
                cancellationToken)
            .ConfigureAwait(false);

        var gameProviderAmount = exchangeRateFundResult.Amount;

        if (!await _distributedLock.LockingAsync(
                gameProviderCode: query.GameProviderCode,
                playerCode: query.PlayerCode).ConfigureAwait(false))

        {
            string message = $"This player ({query.PlayerCode}) transfer game provider({query.GameProviderCode}) is locked.";

            _logger.LogWarning(message);

            return new FundResult(
                state: StateCode.TransactionLock,
                message: message);
        }

        try
        {
            var gameProviderBalance = await _gameProvidersClient
            .PlayerBalanceAsync(
                gameProviderCode: query.GameProviderCode,
                playerCode: query.PlayerCode,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

            if (gameProviderBalance - gameProviderAmount < 0)
            {
                string message = $"This player ({query.PlayerCode}) game provider ({query.GameProviderCode})has insufficient balance({gameProviderBalance}).";

                _logger.LogWarning(message);

                return new FundResult(
                    state: StateCode.InsufficientBalance,
                    message: message);
            }

            var gameProviderFundTransferTransactionId = await _repository
                .AddFundInAsync(
                    gameProviderId: query.GameProviderId,
                    gameProviderCode: query.GameProviderCode,
                    playerCode: query.PlayerCode,
                    walletId: query.WalletId,
                    amount: query.Amount,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var gameProviderTransactionId = await _gameProvidersClient
                .PlayerFundInAsync(
                    gameProviderCode: query.GameProviderCode,
                    brandCode: query.BrandCode,
                    playerCode: query.PlayerCode,
                    currencyCode: query.CurrencyCode,
                    orderId: gameProviderFundTransferTransactionId.ToString("N"),
                    amount: gameProviderAmount,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(gameProviderTransactionId))
            {
                await _repository.UpdateByGameProviderFundFailedAsync(
                        gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                throw new Exception("The game manufacturer's transfer failed.");
            }

            try
            {
                var transactionResponse = await _cashServiceClient
                    .GameProviderFundInAsync(
                        walletId: query.WalletId,
                        amount: query.Amount,
                        description: $"【{query.GameProviderCode}】->【Default】",
                        externalId: gameProviderFundTransferTransactionId.ToString(),
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await _repository
                    .UpdateBySuccessAsync(
                        gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                        transactionId: transactionResponse.TransactionId,
                        gameProviderTransactionId: gameProviderTransactionId,
                        balance: transactionResponse.Balance,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return new FundResult(
                    state: StateCode.Success,
                    message: StateCode.Success.GetDescription(),
                    fundTransactionId: gameProviderFundTransferTransactionId,
                    transactionId: transactionResponse.TransactionId,
                    externalTransactionId: gameProviderTransactionId);
            }
            catch (Exception ex)
            {
                await _repository
                    .UpdateByMainWalletFundFailedAsync(
                        gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                        gameProviderTransactionId: gameProviderTransactionId,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                string message = $"The main wallet transfer failed.({gameProviderFundTransferTransactionId})";
                _logger.LogError(exception: ex, message);

                return new FundResult(
                    state: StateCode.Exception,
                    message: message,
                    fundTransactionId: gameProviderFundTransferTransactionId,
                    transactionId: Guid.Empty,
                    externalTransactionId: gameProviderTransactionId);
            }
        }
        finally
        {
            await _distributedLock.UnlockAsync(
                  gameProviderCode: query.GameProviderCode,
                  playerCode: query.PlayerCode).ConfigureAwait(false);
        }
    }
}
