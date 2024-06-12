using Microsoft.Extensions.DependencyInjection;
using Grpc.Net.Client;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.Core;
using Tgs.ServiceUtility;
using NSubstitute;

namespace Tgs.GameFundTransfer.IntegrationTest;

public class FundTransferOutUnitTest
{
	[Fact]
	public async Task Normal_Process()
	{
		var gameProviderCode = "KY";
		var playerCode = "4ucmqplxb029";
		var walletId = Guid.NewGuid();
		var currencyCode = "CNY";
		var amount = 1.0m;

		var web = new GameFundTransferApplication(builder => _ = builder.ConfigureServices(services =>
		{
			var fakeService = Substitute.For<ICommandService<FundOutCommand>>();

			var command = new FundOutCommand(
				 GameProviderCode: gameProviderCode,
				 PlayerCode: playerCode,
				 WalletId: walletId,
				 CurrencyCode: currencyCode,
				 Amount: amount);

			_ = fakeService.ExecuteAsync(
				command: Arg.Is(command),
				cancellationToken: Arg.Any<CancellationToken>());

			_ = services.AddTransient(_ => fakeService);
		}));

		var sut = new Client.GameFundTransferClient(new Protos.GameFundTransfer.GameFundTransferClient(
				GrpcChannel.ForAddress(
					address: web.Server.BaseAddress,
					channelOptions: new GrpcChannelOptions { HttpHandler = web.Server.CreateHandler() })));

		var actual = await sut.FundTransferOutAsync(
			gameProviderCode: gameProviderCode,
			playerCode: playerCode,
			walletId: walletId,
			currencyCode: currencyCode,
			amount: amount);

		Assert.NotNull(actual);
	}
}
