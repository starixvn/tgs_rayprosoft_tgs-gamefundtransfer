using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.Extensions;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundInZeroGetService(
    ILogger<FundInGetService> logger,
    ICashServiceClient cashServiceClient,
    IFundsDistributedLock distributedLock,
    IGameCurrencyClient gameCurrencyClient,
    IGameProvidersClient gameProvidersClient,
    IFundTransferTransactionRepository repository) : IGetService<FundInZeroQuery, FundResult>
{
    public async ValueTask<FundResult> GetAsync(FundInZeroQuery query, CancellationToken cancellationToken = default)
    {
        if (!await distributedLock.LockingAsync(
            gameProviderCode: query.GameProviderCode,
            playerCode: query.PlayerCode)
            .ConfigureAwait(false))

        {
            logger.LogWarning("This player ({playerCode}) transfer game provider({gameProviderCode}) is locked.",
                query.PlayerCode,
                query.GameProviderCode);

            return new FundResult(
                state: StateCode.TransactionLock,
                message: $"This player ({query.PlayerCode}) transfer game provider({query.GameProviderCode}) is locked.");
        }

        try
        {
            var playerBalance = await gameProvidersClient
                .PlayerBalanceAsync(
                gameProviderCode: query.GameProviderCode,
                playerCode: query.PlayerCode,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var convertPlatformBalanceResult = await gameCurrencyClient
                .ConvertPlatformBalanceAsync(
                gameProviderCode: query.GameProviderCode,
                currencyCode: query.CurrencyCode,
                balance: playerBalance.Balance,
                freeBalance: playerBalance.FreeBalance,
                outstandingBalance: playerBalance.OutstandingBalance,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (convertPlatformBalanceResult.FreeBalance <= 0)
            {
                logger.LogInformation(
                    "This player  ({playerCode}) has acquired a game provider ({gameProviderCode}) less than or equal to 0.({gameProviderBalance}->{convertPlatformBalance})",
                    query.PlayerCode,
                    query.GameProviderCode,
                    playerBalance.FreeBalance,
                    convertPlatformBalanceResult.FreeBalance);

                return new FundResult(
                    state: StateCode.Success,
                    message: $"This player  ({query.PlayerCode}) has acquired a game provider ({query.GameProviderCode}) less than or equal to 0.({playerBalance.FreeBalance}->{convertPlatformBalanceResult.Balance})",
                    fundTransactionId: Guid.Empty,
                    transactionId: Guid.Empty,
                    externalTransactionId: string.Empty);
            }

            var fundTransactionId = await repository
                .AddFundInAsync(
                gameProviderId: query.GameProviderId,
                gameProviderCode: query.GameProviderCode,
                playerCode: query.PlayerCode,
                walletId: query.WalletId,
                amount: convertPlatformBalanceResult.FreeBalance,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var gameProviderTransactionId = await gameProvidersClient
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
                await repository
                    .UpdateByGameProviderFundFailedAsync(
                    gameProviderFundTransferTransactionId: fundTransactionId,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                logger.LogError(
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
                var transactionResponse = await cashServiceClient
                    .GameProviderFundInAsync(
                    walletId: query.WalletId,
                    amount: convertPlatformBalanceResult.Balance,
                    description: $"【{query.GameProviderCode}】->【Default】",
                    externalId: fundTransactionId.ToString(),
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await repository
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
                await repository
                    .UpdateByMainWalletFundFailedAsync(
                    gameProviderFundTransferTransactionId: fundTransactionId,
                    gameProviderTransactionId: gameProviderTransactionId,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                logger.LogError(exception: ex, "The main wallet transfer failed.({gameProviderFundTransferTransactionId})", fundTransactionId);

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
            await distributedLock
                .UnlockAsync(
                gameProviderCode: query.GameProviderCode,
                playerCode: query.PlayerCode)
                .ConfigureAwait(false);
        }
    }
}
