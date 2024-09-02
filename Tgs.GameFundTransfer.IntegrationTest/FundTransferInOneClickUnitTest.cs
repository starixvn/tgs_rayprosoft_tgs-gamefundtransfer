using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.GameFundTransfer.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.IntegrationTest;

public class FundTransferInOneClickUnitTest
{
	[Fact]
	public async Task Normal_Process()
	{
		var playerCode = "4ucmqplxb029";
		var gameProviderCode = "CQ9";

		var fundTransactionId = Guid.NewGuid();
		var fundTransferResult = new FundTransferResult(
			FundTransactionId: fundTransactionId,
			CashTransactionId: fundTransactionId.ToString(),
			CashRespTransactionId: Guid.NewGuid(),
			GameTransactionId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
			GameRespTransactionId: Guid.NewGuid().ToString("N"));

		var web = new GameFundTransferApplication(builder => _ = builder.ConfigureServices(services =>
		{
			var fakeService = Substitute.For<IGetService<FundTransferInOneClickQuery, FundTransferResult>>();

			var query = new FundTransferInOneClickQuery(
				 GameProviderCode: gameProviderCode,
				 PlayerCode: playerCode);

			_ = fakeService.GetAsync(
				query: Arg.Is(query),
				cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransferResult);

			_ = services.AddTransient(_ => fakeService);
		}));

		var httpClient = web.CreateClient();

		var sut = new GameFundTransferClient(httpClient);

		var actual = await sut.FundTransferInOneClickAsync(
			playerCode: playerCode,
			gameProviderCode: gameProviderCode);

		Assert.NotNull(actual);
		Assert.Equal(fundTransferResult.FundTransactionId, actual.FundTransactionId);
		Assert.Equal(fundTransferResult.CashTransactionId, actual.CashTransactionId);
		Assert.Equal(fundTransferResult.CashRespTransactionId, actual.CashRespTransactionId);
		Assert.Equal(fundTransferResult.GameTransactionId, actual.GameTransactionId);
		Assert.Equal(fundTransferResult.GameRespTransactionId, actual.GameRespTransactionId);
	}
}
