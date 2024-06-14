using Grpc.Net.Client;

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.IntegrationTest;

public class FundTransferInOneClickUnitTest
{
	[Fact]
	public async Task Normal_Process()
	{
		var gameProviderCode = "KY";
		var playerCode = "4ucmqplxb029";
		var walletId = Guid.NewGuid();
		var currencyCode = "CNY";

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
				 PlayerCode: playerCode,
				 WalletId: walletId,
				 CurrencyCode: currencyCode);

			_ = fakeService.GetAsync(
				query: Arg.Is(query),
				cancellationToken: Arg.Any<CancellationToken>())
			.Returns(fundTransferResult);

			_ = services.AddTransient(_ => fakeService);
		}));

		var sut = new Client.GameFundTransferClient(new Protos.GameFundTransfer.GameFundTransferClient(
				GrpcChannel.ForAddress(
					address: web.Server.BaseAddress,
					channelOptions: new GrpcChannelOptions { HttpHandler = web.Server.CreateHandler() })));

		var actual = await sut.FundTransferInOneClickAsync(
			gameProviderCode: gameProviderCode,
			playerCode: playerCode,
			walletId: walletId,
			currencyCode: currencyCode);

		Assert.NotNull(actual);
		Assert.Equal(fundTransferResult.FundTransactionId, actual.FundTransactionId.ToGuid());
		Assert.Equal(fundTransferResult.CashTransactionId, actual.CashTransactionId);
		Assert.Equal(fundTransferResult.CashRespTransactionId, actual.CashRespTransactionId.ToGuid());
		Assert.Equal(fundTransferResult.GameTransactionId, actual.GameTransactionId);
		Assert.Equal(fundTransferResult.GameRespTransactionId, actual.GameRespTransactionId);
	}
}
