using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GamePlayerOnline.Client;
using Tgs.GameProviders.Domains;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;

public class FundBalanceOnlineCommandServiceUnitTest
{
	[Fact]
	public async Task Normal_Process()
	{
		var faceLogger = NullLogger<FundBalanceOnlineCommandService>.Instance;
		var fakeGamePlayerOnlineClient = Substitute.For<IGamePlayerOnlineClient>();
		var command = new FundBalanceOnlineCommand(
			 GameProviderCode: GameProviderType.Kaiyuan,
			 PlayerCode: "4ucmqplxb029",
			 Balance: 100.0m,
			 FreeBalance: 90.0m,
			 OutstandingBalance: 10.0m);

		var sut = new FundBalanceOnlineCommandService(
			faceLogger,
			fakeGamePlayerOnlineClient);

		await sut.ExecuteAsync(command);

		_ = fakeGamePlayerOnlineClient
			.Received(1)
			.PlayerOnlineUpsertAsync(
			gameProviderCode: command.GameProviderCode,
			playerCode: command.PlayerCode,
			balance: command.Balance,
			freeBalance: command.FreeBalance,
			outstandingBalance: command.OutstandingBalance,
			cancellationToken: Arg.Any<CancellationToken>());
	}
}
