using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tgs.GameFund.Tracker.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;
public class FundTransferTrackerCommandServiceUnitTest
{
	[Fact]
	public async Task Normal_Process()
	{
		var faceLogger = NullLogger<FundTransferTrackerCommandService>.Instance;
		var fakeOptions = Options.Create(new PolicySettingsOptions
		{
			RetryCount = 1,
			RetrySeconds = 1,
		});
		var fakeGameFundTrackerClient = Substitute.For<IGameFundTrackerClient>();

		var command = new FundTransferTrackerCommand(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid());

		var sut = new FundTransferTrackerCommandService(
			faceLogger,
			fakeOptions,
			fakeGameFundTrackerClient);

		await sut.ExecuteAsync(command);

		_ = fakeGameFundTrackerClient
			.Received(1)
			.TraceGameFundAsync(
			Arg.Is(command.GameProviderId),
			Arg.Is(command.WalletId),
			Arg.Is(command.PlayerCode));
	}

	[Fact]
	public async Task Policy_Process()
	{
		var faceLogger = NullLogger<FundTransferTrackerCommandService>.Instance;
		var fakeOptions = Options.Create(new PolicySettingsOptions
		{
			RetryCount = 1,
			RetrySeconds = 1,
		});
		var fakeGameFundTrackerClient = Substitute.For<IGameFundTrackerClient>();

		var command = new FundTransferTrackerCommand(
			GameProviderId: Guid.NewGuid(),
			GameProviderCode: GameProviderType.Kaiyuan,
			PlayerCode: "4ucmqplxb029",
			WalletId: Guid.NewGuid());

		_ = fakeGameFundTrackerClient
			.TraceGameFundAsync(
			Arg.Is(command.GameProviderId),
			Arg.Is(command.WalletId),
			Arg.Is(command.PlayerCode))
			.Returns(ValueTask.FromException(new HttpRequestException()), ValueTask.CompletedTask);

		var sut = new FundTransferTrackerCommandService(
			faceLogger,
			fakeOptions,
			fakeGameFundTrackerClient);

		await sut.ExecuteAsync(command);
	}
}
