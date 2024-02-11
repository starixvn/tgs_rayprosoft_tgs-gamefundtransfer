using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.Exceptions;
using Tgs.GameProviders.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundInCommandService(
    ILogger<FundInCommandService> logger,
    ICashServiceClient cashServiceClient,
    IFundsDistributedLock fundsLocked,
    IFundTransferTransactionRepository fundTransferTransactionRepository,
    IGameCurrencyClient gameCurrencyClient,
    IGameProvidersClient gameProvidersClient) : ICommandService<FundInCommand>
{
    public async ValueTask ExecuteAsync(FundInCommand query, CancellationToken cancellationToken = default)
    {
        if (query.Amount < 0)
        {
            throw new ValidationException($"The field Amount must be between 0 and 79228162514264337593543950335.({query.Amount})");
        }

        var exchangeRateFundResult = await gameCurrencyClient
            .ConvertGameProviderFundsAsync(
            query.GameProviderCode,
            query.CurrencyCode,
            query.Amount,
            cancellationToken)
            .ConfigureAwait(false);

        var gameProviderAmount = exchangeRateFundResult.Amount;

        if (!await fundsLocked
            .LockingAsync(
            gameProviderCode: query.GameProviderCode,
            playerCode: query.PlayerCode)
            .ConfigureAwait(false))

        {
            logger.LogWarning("Player ({playerCode}) funds transfer game provider ({gameProviderCode}) has been locked.", query.PlayerCode, query.GameProviderCode);
            return;
        }

        try
        {
            var playerBalance = await gameProvidersClient
                .PlayerBalanceAsync(
                gameProviderCode: query.GameProviderCode,
                playerCode: query.PlayerCode,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (playerBalance.FreeBalance < 0)
            {
                throw new FreeBalanceException(query.PlayerCode, query.GameProviderCode, playerBalance.FreeBalance);
            }

            if (playerBalance.FreeBalance - gameProviderAmount < 0)
            {
                throw new Exception($"Insufficient game wallet balance.({playerBalance.FreeBalance})");
            }

            var gameProviderFundTransferTransactionId = await fundTransferTransactionRepository
                .AddFundInAsync(
                gameProviderId: query.GameProviderId,
                gameProviderCode: query.GameProviderCode,
                playerCode: query.PlayerCode,
                walletId: query.WalletId,
                amount: query.Amount,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var gameProviderTransactionId = await gameProvidersClient
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
                await fundTransferTransactionRepository
                    .UpdateByGameProviderFundFailedAsync(
                    gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                throw new Exception("The game manufacturer's transfer failed.");
            }

            try
            {
                var transactionResponse = await cashServiceClient
                    .GameProviderFundInAsync(
                    walletId: query.WalletId,
                    amount: query.Amount,
                    description: $"【{query.GameProviderCode}】->【Default】",
                    externalId: gameProviderFundTransferTransactionId.ToString(),
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await fundTransferTransactionRepository.UpdateBySuccessAsync(
                    gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                    transactionId: transactionResponse.TransactionId,
                    gameProviderTransactionId: gameProviderTransactionId,
                    balance: transactionResponse.Balance,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await fundTransferTransactionRepository
                    .UpdateByMainWalletFundFailedAsync(
                    gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
                    gameProviderTransactionId: gameProviderTransactionId,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                logger.LogError(exception: ex, "The main wallet transfer failed.({gameProviderFundTransferTransactionId})", gameProviderFundTransferTransactionId);
            }
        }
        finally
        {
            await fundsLocked.UnlockAsync(
                  gameProviderCode: query.GameProviderCode,
                  playerCode: query.PlayerCode).ConfigureAwait(false);
        }
    }
}
