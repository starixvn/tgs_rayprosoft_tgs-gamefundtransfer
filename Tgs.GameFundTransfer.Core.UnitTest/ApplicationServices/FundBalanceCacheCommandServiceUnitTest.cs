using NSubstitute;
using Tgs.GameCurrency.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameProviders.Domains;
using Valhalla.MessageQueue;

namespace Tgs.GameFundTransfer.Core.UnitTest.ApplicationServices;
public class FundBalanceCacheCommandServiceUnitTest
{
	[Fact]
	public async Task Cache_Does_Not_Exist()
	{
		var fakeFundBalanceRepository = Substitute.For<IFundBalanceRepository>();

		var command = new FundBalanceCacheCommand(
			 Action: Common.Protos.Action.Cache,
			 GameProviderCode: GameProviderType.Kaiyuan,
			 PlayerCode: "4ucmqplxb029",
			 Balance: 100.0m,
			 FreeBalance: 90.0m,
			 OutstandingBalance: 10.0m);

		_ = fakeFundBalanceRepository.ExistsAsync(
			gameProviderCode: Arg.Is(command.GameProviderCode),
			playerCode: Arg.Is(command.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(false);

		var sut = new FundBalanceCacheCommandService(
			fakeFundBalanceRepository);

		await sut.ExecuteAsync(command);

		_ = fakeFundBalanceRepository
			.Received(1)
			.SettingsAsync(
			gameProviderCode: Arg.Is(command.GameProviderCode),
			playerCode: Arg.Is(command.PlayerCode),
			source: Arg.Is(new Models.FundBalanceInfo(
				Balance: command.Balance,
				FreeBalance: command.FreeBalance,
				OutstandingBalance: command.OutstandingBalance)),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Cache_Already_Exists()
	{
		var fakeFundBalanceRepository = Substitute.For<IFundBalanceRepository>();

		var command = new FundBalanceCacheCommand(
			 Action: Common.Protos.Action.Cache,
			 GameProviderCode: GameProviderType.Kaiyuan,
			 PlayerCode: "4ucmqplxb029",
			 Balance: 100.0m,
			 FreeBalance: 90.0m,
			 OutstandingBalance: 10.0m);

		_ = fakeFundBalanceRepository.ExistsAsync(
			gameProviderCode: Arg.Is(command.GameProviderCode),
			playerCode: Arg.Is(command.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>())
			.Returns(true);

		var sut = new FundBalanceCacheCommandService(
			fakeFundBalanceRepository);

		await sut.ExecuteAsync(command);

		_ = fakeFundBalanceRepository
			.Received(0)
			.SettingsAsync(
			gameProviderCode: Arg.Is(command.GameProviderCode),
			playerCode: Arg.Is(command.PlayerCode),
			source: Arg.Is(new Models.FundBalanceInfo(
				Balance: command.Balance,
				FreeBalance: command.FreeBalance,
				OutstandingBalance: command.OutstandingBalance)),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Cache_Release()
	{
		var fakeFundBalanceRepository = Substitute.For<IFundBalanceRepository>();

		var command = new FundBalanceCacheCommand(
			 Action: Common.Protos.Action.Release,
			 GameProviderCode: GameProviderType.Kaiyuan,
			 PlayerCode: "4ucmqplxb029",
			 Balance: 100.0m,
			 FreeBalance: 90.0m,
			 OutstandingBalance: 10.0m);

		var sut = new FundBalanceCacheCommandService(
			fakeFundBalanceRepository);

		await sut.ExecuteAsync(command);

		_ = fakeFundBalanceRepository
			.Received(0)
			.ExistsAsync(
			gameProviderCode: Arg.Is(command.GameProviderCode),
			playerCode: Arg.Is(command.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeFundBalanceRepository
			.Received(0)
			.SettingsAsync(
			gameProviderCode: Arg.Is(command.GameProviderCode),
			playerCode: Arg.Is(command.PlayerCode),
			source: Arg.Is(new Models.FundBalanceInfo(
				Balance: command.Balance,
				FreeBalance: command.FreeBalance,
				OutstandingBalance: command.OutstandingBalance)),
			cancellationToken: Arg.Any<CancellationToken>());

		_ = fakeFundBalanceRepository
			.Received(1)
			.ReleaseAsync(
			gameProviderCode: Arg.Is(command.GameProviderCode),
			playerCode: Arg.Is(command.PlayerCode),
			cancellationToken: Arg.Any<CancellationToken>());
	}
}
