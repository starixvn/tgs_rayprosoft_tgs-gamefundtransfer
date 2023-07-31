using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.Extensions;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundInZeroGetService : IGetService<FundInZeroQuery, FundResult>
{
    private readonly ICashServiceClient _cashServiceClient;
    private readonly IFundsDistributedLock _distributedLock;
    private readonly IGameCurrencyClient _gameCurrencyClient;
    private readonly IGameProvidersClient _gameProvidersClient;
    private readonly ILogger<FundInGetService> _logger;
    private readonly IFundTransferTransactionRepository _repository;

    public FundInZeroGetService(
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

    public async ValueTask<FundResult> GetAsync(FundInZeroQuery query, CancellationToken cancellationToken = default)
    {
        if (!await _distributedLock
            .LockingAsync(
            gameProviderCode: query.GameProviderCode,
            playerCode: query.PlayerCode)
            .ConfigureAwait(false))

        {
            _logger.LogWarning("This player ({playerCode}) transfer game provider({gameProviderCode}) is locked.",
                query.PlayerCode,
                query.GameProviderCode);

            return new FundResult(
                state: StateCode.TransactionLock,
                message: $"This player ({query.PlayerCode}) transfer game provider({query.GameProviderCode}) is locked.");
        }

        try
        {
            var playerBalance = await _gameProvidersClient
                .PlayerBalanceAsync(
                gameProviderCode: query.GameProviderCode,
                playerCode: query.PlayerCode,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var convertPlatformBalanceResult = await _gameCurrencyClient
                .ConvertPlatformBalanceAsync(
                query.GameProviderCode,
                query.CurrencyCode,
                playerBalance.FreeBalance,
                cancellationToken)
                .ConfigureAwait(false);

            if (convertPlatformBalanceResult.Balance <= 0)
            {
                _logger.LogInformation(
                    "This player  ({playerCode}) has acquired a game provider ({gameProviderCode}) less than or equal to 0.({gameProviderBalance}->{convertPlatformBalance})",
                    query.PlayerCode,
                    query.GameProviderCode,
                    playerBalance.FreeBalance,
                    convertPlatformBalanceResult.Balance);

                return new FundResult(
                    state: StateCode.Success,
                    message: $"This player  ({query.PlayerCode}) has acquired a game provider ({query.GameProviderCode}) less than or equal to 0.({playerBalance.FreeBalance}->{convertPlatformBalanceResult.Balance})",
                    fundTransactionId: Guid.Empty,
                    transactionId: Guid.Empty,
                    externalTransactionId: string.Empty);
            }

            var fundTransactionId = await _repository
                .AddFundInAsync(
                gameProviderId: query.GameProviderId,
                gameProviderCode: query.GameProviderCode,
                playerCode: query.PlayerCode,
                walletId: query.WalletId,
                amount: convertPlatformBalanceResult.Balance,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var gameProviderTransactionId = await _gameProvidersClient
                .PlayerFundInAsync(
                gameProviderCode: query.GameProviderCode,
                brandCode: query.BrandCode,
                playerCode: query.PlayerCode,
                currencyCode: query.CurrencyCode,
                orderId: fundTransactionId.ToString("N"),
                amount: playerBalance.FreeBalance,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(gameProviderTransactionId))
            {
                await _repository
                    .UpdateByGameProviderFundFailedAsync(
                    gameProviderFundTransferTransactionId: fundTransactionId,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogError(
                    "The transaction number provided by this game provider ({gameProviderCode}) is empty.({gameProviderFundTransferTransactionId})",
                    query.GameProviderCode,
                    fundTransactionId);

                return new FundResult(
                    state: StateCode.ExternalTransactionIdEmpty,
                    message: $"The transaction number provided by this game provider ({query.GameProviderCode}) is empty.({fundTransactionId})",
                    fundTransactionId: fundTransactionId,
                    transactionId: Guid.Empty,
                    externalTransactionId: string.Empty);
            }

            try
            {
                var transactionResponse = await _cashServiceClient
                    .GameProviderFundInAsync(
                    walletId: query.WalletId,
                    amount: convertPlatformBalanceResult.Balance,
                    description: $"【{query.GameProviderCode}】->【Default】",
                    externalId: fundTransactionId.ToString(),
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await _repository
                    .UpdateBySuccessAsync(
                    gameProviderFundTransferTransactionId: fundTransactionId,
                    transactionId: transactionResponse.TransactionId,
                    gameProviderTransactionId: gameProviderTransactionId,
                    balance: transactionResponse.Balance,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return new FundResult(
                    state: StateCode.Success,
                    message: StateCode.Success.GetDescription(),
                    fundTransactionId: fundTransactionId,
                    transactionId: transactionResponse.TransactionId,
                    externalTransactionId: gameProviderTransactionId);
            }
            catch (Exception ex)
            {
                await _repository
                    .UpdateByMainWalletFundFailedAsync(
                    gameProviderFundTransferTransactionId: fundTransactionId,
                    gameProviderTransactionId: gameProviderTransactionId,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogError(exception: ex, "The main wallet transfer failed.({gameProviderFundTransferTransactionId})", fundTransactionId);

                return new FundResult(
                    state: StateCode.Exception,
                    message: $"The main wallet transfer failed.({fundTransactionId})",
                    fundTransactionId: fundTransactionId,
                    transactionId: Guid.Empty,
                    externalTransactionId: gameProviderTransactionId);
            }
        }
        finally
        {
            await _distributedLock
                .UnlockAsync(
                gameProviderCode: query.GameProviderCode,
                playerCode: query.PlayerCode)
                .ConfigureAwait(false);
        }
    }
}
