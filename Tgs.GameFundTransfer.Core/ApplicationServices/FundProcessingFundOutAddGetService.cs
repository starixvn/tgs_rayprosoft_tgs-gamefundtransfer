using Microsoft.Extensions.Logging;
using Tgs.GameFundTransfer.Common.Enums;

using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundProcessingFundOutAddGetService(
	ILogger<FundProcessingFundOutAddGetService> logger,
	ISerialNumberGenerator serialNumberGenerator,
	IPlayerRepository playerRepository,
	IGameProviderRepository gameProviderRepository,
	IGameProviderTransactionIdUtensil gameProviderTransactionIdUtensil,
	IFundTransferTransactionRepository fundTransferTransactionRepository,
	IMessageSender messageSender) : IGetService<FundProcessingFundOutAddQuery, FundProcessingResult>
{
	public async ValueTask<FundProcessingResult> GetAsync(FundProcessingFundOutAddQuery query, CancellationToken cancellationToken = default)
	{
		var gameProviderFundTransferTransactionId = await serialNumberGenerator.GenerateAsync(
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var playerInfo = await playerRepository.GetAsync(
			playerCode: query.PlayerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var gameProviderInfo = await gameProviderRepository.GetAsync(
			gameProviderCode: query.GameProviderCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		try
		{
			var gameProviderTransactionId = gameProviderInfo.GameProviderCode switch
			{
				var gameProviderCode when gameProviderCode == GameProviders.Domains.GameProviderType.Cards365Transfer => await gameProviderTransactionIdUtensil.GetCards365TxnIdAsync(true, cancellationToken).ConfigureAwait(false),
				var gameProviderCode when gameProviderCode == GameProviders.Domains.GameProviderType.FaChai => await gameProviderTransactionIdUtensil.GetFaChaiTxnIdAsync(true, cancellationToken).ConfigureAwait(false),
				var gameProviderCode when gameProviderCode == GameProviders.Domains.GameProviderType.GKXGaming => await gameProviderTransactionIdUtensil.GetGKXGamingTxnIdAsync(query.PlayerCode, cancellationToken).ConfigureAwait(false),
				var gameProviderCode when gameProviderCode == GameProviders.Domains.GameProviderType.Kaiyuan => await gameProviderTransactionIdUtensil.GetKaiyuanTxnIdAsync(query.PlayerCode, cancellationToken).ConfigureAwait(false),
				var gameProviderCode when gameProviderCode == GameProviders.Domains.GameProviderType.LEGaming => await gameProviderTransactionIdUtensil.GetLEGamingTxnIdAsync(query.PlayerCode, cancellationToken).ConfigureAwait(false),
				var gameProviderCode when gameProviderCode == GameProviders.Domains.GameProviderType.SBOTransfer => await gameProviderTransactionIdUtensil.GetSBOTxnIdAsync(true, cancellationToken).ConfigureAwait(false),
				var gameProviderCode when gameProviderCode == GameProviders.Domains.GameProviderType.TPhere => await gameProviderTransactionIdUtensil.GetTPhereTxnIdAsync(true, cancellationToken).ConfigureAwait(false),
				var gameProviderCode when gameProviderCode == GameProviders.Domains.GameProviderType.V8Poker => await gameProviderTransactionIdUtensil.GetV8PokerTxnIdAsync(query.PlayerCode, cancellationToken).ConfigureAwait(false),
				var gameProviderCode when gameProviderCode == GameProviders.Domains.GameProviderType.YeeBet => (await gameProviderTransactionIdUtensil.GetYeeBetTxnIdAsync(cancellationToken).ConfigureAwait(false)).ToString(),
				_ => await gameProviderTransactionIdUtensil.GetTxnIdAsync(gameProviderFundTransferTransactionId, cancellationToken).ConfigureAwait(false),
			};

			await fundTransferTransactionRepository.AddProcessingAsync(
				gameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
				gameProviderId: gameProviderInfo.GameProviderId,
				walletId: playerInfo.WalletId,
				gameProviderTransactionId: gameProviderTransactionId,
				playerCode: playerInfo.PlayerCode,
				fundSource: FundSource.FundOut,
				fromWallet: WalletDef.MainWallet,
				toWallet: gameProviderInfo.GameProviderCode,
				amount: query.Amount,
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			return new FundProcessingResult(
				BrandId: playerInfo.BrandId,
				BrandCode: playerInfo.BrandCode,
				PlayerCode: playerInfo.PlayerCode,
				Username: playerInfo.Username,
				GameProviderId: gameProviderInfo.GameProviderId,
				GameProviderCode: gameProviderInfo.GameProviderCode,
				GameProviderFundTransferTransactionId: gameProviderFundTransferTransactionId,
				GameProviderTransactionId: gameProviderTransactionId);
		}
		catch (Exception ex)
		{
			logger.LogInformation(ex, "LogOn:{logOn} QueryInfo:{query} Mesaage:This fund transfer failed to create an order.",
				DateTime.Now.ToString("o"),
				query);

			throw;
		}
		finally
		{
			var gameFundTransferCacheRequest = new Common.Protos.GameFundTransferCacheRequest
			{
				Action = Common.Protos.Action.Release,
				GameProviderCode = gameProviderInfo.GameProviderCode,
				PlayerCode = playerInfo.PlayerCode,
				Balance = 0.0m,
				FreeBalance = 0.0m,
				OutstandingBalance = 0.0m
			};
			await messageSender.PublishAsync(
				subject: Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest,
				data: gameFundTransferCacheRequest,
				cancellationToken: cancellationToken).ConfigureAwait(false);
		}
	}
}
