using Microsoft.Extensions.Options;
using Tgs.GameFundTransfer.Client;
using Tgs.GameFundTransfer.Protos;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGameTransferClient(
            this IServiceCollection services,
            Action<GameTransferClientOptions, IServiceProvider> configure)
            => services
            .AddOptions<GameTransferClientOptions>()
            .Configure(configure)
            .Services
            .AddGrpcClient<GameFundTransferService.GameFundTransferServiceClient>((sp, configureClient) =>
            {
                var options = sp.GetRequiredService<IOptions<GameTransferClientOptions>>().Value;
                configureClient.Address = options.BaseUrl;
            })
            .ConfigureChannel((sp, configureChannel) =>
            {
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    EnableMultipleHttp2Connections = true
                };
                configureChannel.HttpHandler = handler;
            })
            .AddInterceptor<LoggingInterceptor>()
            .Services
            .AddSingleton<DiagnosticsInterceptorOptions>()
            .AddSingleton<LoggingInterceptor>()
            .AddTransient<IGameTransferGrpcService, GameTransferGrpcService>();
    }
}
