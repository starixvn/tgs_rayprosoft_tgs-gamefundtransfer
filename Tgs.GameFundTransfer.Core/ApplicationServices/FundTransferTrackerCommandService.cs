using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Tgs.GameFund.Tracker.Client;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Core.ApplicationServices;

internal class FundTransferTrackerCommandService(
	ILogger<FundTransferTrackerCommandService> logger,
	IOptions<PolicySettingsOptions> policySettingsOptions,
	IGameFundTrackerClient client) : ICommandService<FundTransferTrackerCommand>
{
	private readonly PolicySettingsOptions _policySettingsOptions = policySettingsOptions.Value;

	public async ValueTask ExecuteAsync(FundTransferTrackerCommand command, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("{logInformation}",
			JsonSerializer.Serialize(new
			{
				LogOn = DateTime.Now.ToString("u"),
				Data = new
				{
					command.GameProviderId,
					command.GameProviderCode,
					command.WalletId,
					command.PlayerCode
				},
				Message = "This receive message handler."
			}));

		var retryCount = _policySettingsOptions.RetryCount;
		var retrySeconds = _policySettingsOptions.RetrySeconds;
		var retryPolicy = Policy
			.Handle<HttpRequestException>()
			.WaitAndRetryAsync(
				retryCount: retryCount,
				retryAttempt => TimeSpan.FromSeconds(Math.Pow(retrySeconds, retryAttempt)),
				onRetry: (exception, retryTime, retryCount, context) => logger.LogWarning(exception, "{logWarning}",
					new
					{
						LogOn = DateTime.Now.ToString("u"),
						RetryTime = retryTime,
						RetryCount = retryCount,
						Data = new
						{
							command.GameProviderId,
							command.GameProviderCode,
							command.WalletId,
							command.PlayerCode
						},
						exception.Message
					}));

		await retryPolicy.ExecuteAsync(() => client.TraceGameFundAsync(
			gameProviderId: command.GameProviderId,
			walletId: command.WalletId,
			playerCode: command.PlayerCode,
			cancellationToken: cancellationToken).AsTask())
			.ConfigureAwait(false);
	}
}
