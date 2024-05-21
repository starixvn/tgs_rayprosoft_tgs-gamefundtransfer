using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Core;

internal class GameProviderTransactionIdUtensil(
	TimeProvider timeProvider,
	ISnowflakeWorkUtensil snowflakeWorkUtensil,
	IPlayerRepository playerRepository,
	IGameProviderRepository gameProviderRepository,
	IBrandGameProviderRepository brandGameProviderRepository,
	IGameProviderConfigurationRepository gameProviderConfigurationRepository) : IGameProviderTransactionIdUtensil
{
	public ValueTask<string> GetCards365TxnIdAsync(bool Deposit, CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(string.Concat(Deposit ? "D" : "W", snowflakeWorkUtensil.GetId()));

	public ValueTask<string> GetFaChaiTxnIdAsync(bool Deposit, CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(string.Concat(Deposit ? "D" : "W", snowflakeWorkUtensil.GetId()));

	public async ValueTask<string> GetGKXGamingTxnIdAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var gameProviderInfo = await gameProviderRepository.GetAsync(
			gameProviderCode: GameProviderType.GKXGaming,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var playerInfo = await playerRepository.GetAsync(
			playerCode: playerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var gameProviderConfigurationId = await brandGameProviderRepository.GetGameProviderConfigurationIdAsync(
			brandId: playerInfo.BrandId,
			gameProviderId: gameProviderInfo.GameProviderId,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var jsonNode = await gameProviderConfigurationRepository.GetConfigurationDataAsync(
			gameProviderConfigurationId: gameProviderConfigurationId,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return string.Concat(jsonNode["AgentId"]!.ToString(), timeProvider.GetUtcNow().ToString("yyyyMMddHHmmssfff"), playerCode);
	}

	public async ValueTask<string> GetKaiyuanTxnIdAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var gameProviderInfo = await gameProviderRepository.GetAsync(
			gameProviderCode: GameProviderType.Kaiyuan,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var playerInfo = await playerRepository.GetAsync(
			playerCode: playerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var gameProviderConfigurationId = await brandGameProviderRepository.GetGameProviderConfigurationIdAsync(
			brandId: playerInfo.BrandId,
			gameProviderId: gameProviderInfo.GameProviderId,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var jsonNode = await gameProviderConfigurationRepository.GetConfigurationDataAsync(
			gameProviderConfigurationId: gameProviderConfigurationId,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return string.Concat(jsonNode["AgentId"]!.ToString(), timeProvider.GetUtcNow().ToString("yyyyMMddHHmmssfff"), playerCode);
	}

	public async ValueTask<string> GetLEGamingTxnIdAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var gameProviderInfo = await gameProviderRepository.GetAsync(
			gameProviderCode: GameProviderType.LEGaming,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var playerInfo = await playerRepository.GetAsync(
			playerCode: playerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var gameProviderConfigurationId = await brandGameProviderRepository.GetGameProviderConfigurationIdAsync(
			brandId: playerInfo.BrandId,
			gameProviderId: gameProviderInfo.GameProviderId,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var jsonNode = await gameProviderConfigurationRepository.GetConfigurationDataAsync(
			gameProviderConfigurationId: gameProviderConfigurationId,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return string.Concat(jsonNode["AgentId"]!.ToString(), timeProvider.GetUtcNow().ToString("yyyyMMddHHmmssfff"), playerCode);
	}

	public ValueTask<string> GetSBOTxnIdAsync(bool Deposit, CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(string.Concat(Deposit ? "D" : "W", snowflakeWorkUtensil.GetId()));

	public ValueTask<string> GetTPhereTxnIdAsync(bool Deposit, CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(string.Concat(Deposit ? "D" : "W", snowflakeWorkUtensil.GetId()));

	public ValueTask<string> GetTxnIdAsync(Guid gameProviderFundTransferTransactionId, CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(gameProviderFundTransferTransactionId.ToString("N"));

	public async ValueTask<string> GetV8PokerTxnIdAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var gameProviderInfo = await gameProviderRepository.GetAsync(
			gameProviderCode: GameProviderType.V8Poker,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var playerInfo = await playerRepository.GetAsync(
			playerCode: playerCode,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var gameProviderConfigurationId = await brandGameProviderRepository.GetGameProviderConfigurationIdAsync(
			brandId: playerInfo.BrandId,
			gameProviderId: gameProviderInfo.GameProviderId,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
		var jsonNode = await gameProviderConfigurationRepository.GetConfigurationDataAsync(
			gameProviderConfigurationId: gameProviderConfigurationId,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return string.Concat(jsonNode["AgentId"]!.ToString(), timeProvider.GetUtcNow().ToString("yyyyMMddHHmmssfff"), playerCode);
	}

	public ValueTask<long> GetYeeBetTxnIdAsync(CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(snowflakeWorkUtensil.GetId());
}
