using Microsoft.Extensions.DependencyInjection;
using Grpc.Net.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.Core;
using Tgs.ServiceUtility;
using NSubstitute;

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

		var web = new GameFundTransferApplication(builder => _ = builder.ConfigureServices(services =>
		{
			var fakeService = Substitute.For<IGetService<FundInZeroQuery, FundResult>>();

			var query = new FundInZeroQuery(
				 GameProviderCode: gameProviderCode,
				 PlayerCode: playerCode,
				 WalletId: walletId,
				 CurrencyCode: currencyCode);

			_ = fakeService.GetAsync(
				query: Arg.Is(query),
				cancellationToken: Arg.Any<CancellationToken>())
			.Returns(new FundResult(
				state: StateCode.Success,
				message: string.Empty,
				fundTransactionId: Guid.Empty,
				transactionId: Guid.Empty,
				externalTransactionId: string.Empty));

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
	}
}
