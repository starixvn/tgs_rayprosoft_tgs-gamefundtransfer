using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.GameFundTransfer.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.IntegrationTest;

public class FundTransferBalanceUnitTest
{
	[Fact]
	public async Task Normal_Process()
	{
		var playerCode = "4ucmqplxb029";
		var gameProviderCode = "CQ9";

		var web = new GameFundTransferApplication(builder => _ = builder.ConfigureServices(services =>
		{
			var fakeService = Substitute.For<IGetService<FundBalanceQuery, FundBalanceResult>>();

			var query = new FundBalanceQuery(
				 GameProviderCode: gameProviderCode,
				 PlayerCode: playerCode);

			_ = fakeService.GetAsync(
				query: Arg.Is(query),
				cancellationToken: Arg.Any<CancellationToken>())
			.Returns(new FundBalanceResult(
				Balance: 100.00m,
				FreeBalance: 91.23m,
				OutstandingBalance: 8.77m));

			_ = services.AddTransient(_ => fakeService);
		}));

		var httpClient = web.CreateClient();

		var sut = new GameFundTransferClient(httpClient);

		var actual = await sut.FundTransferBalanceAsync(
			playerCode: playerCode,
			gameProviderCode: gameProviderCode);

		Assert.NotNull(actual);
		Assert.Equal(100.00m, actual.Balance);
		Assert.Equal(91.23m, actual.FreeBalance);
		Assert.Equal(8.77m, actual.OutstandingBalance);
	}
}
