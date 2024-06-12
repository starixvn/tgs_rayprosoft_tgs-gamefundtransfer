using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Tgs.GameFundTransfer.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddGameFundTransferClient(
		this IServiceCollection services,
		Action<GameFundTransferClientOptions, IServiceProvider> configure)
	{
		var isDns = false;
		_ = services
			.AddOptions<GameFundTransferClientOptions>()
			.Configure(configure)
			.Services
			.AddGrpcClient<Tgs.GameFundTransfer.Protos.GameFundTransfer.GameFundTransferClient>((sp, configureClient) =>
			{
				var options = sp.GetRequiredService<IOptions<GameFundTransferClientOptions>>().Value;
				isDns = options.BaseUrl.Scheme.Equals("dns", StringComparison.OrdinalIgnoreCase);
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
				var defaultMethodConfig = new MethodConfig
				{
					Names = { MethodName.Default },
					RetryPolicy = new RetryPolicy
					{
						// The maximum number of call attempts,including the original attempt.(最大呼叫嘗試次數，包括原始嘗試。)
						// This value is limited by GrpcChannelOptions.MaxRetryAttempts which defaults to 5.(該值受到限制，GrpcChannelOptions.MaxRetryAttempts默認為 5。)
						// A value is required and must be greater than 1.(一個值是必需的，並且必須大於 1。)
						MaxAttempts = 5,
						// The initial backoff delay between retry attempts.(重試嘗試之間的初始退避延遲。)
						// A randomized delay between 0 and the current backoff determines when the next retry attempt is made.(0 和當前退避之間的隨機延遲決定何時進行下一次重試。)
						// After each attempt, the current backoff is multiplied by BackoffMultiplier.(每次嘗試後，當前退避都會乘以BackoffMultiplier。)
						// A value is required and must be greater than zero.(需要一個值並且必須大於零。)
						InitialBackoff = TimeSpan.FromSeconds(1),
						// The maximum backoff places an upper limit on exponential backoff growth.(最大退避設置了指數退避增長的上限。)
						// A value is required and must be greater than zero.(需要一個值並且必須大於零。)
						MaxBackoff = TimeSpan.FromSeconds(5),
						// The backoff will be multiplied by this value after each retry attempt and will increase exponentially when the multiplier is greater than 1.(每次重試後，退避將乘以該值，並且當乘數大於 1 時，退避將呈指數增加。)
						// A value is required and must be greater than zero.(需要一個值，並且該值必須大於零。)
						BackoffMultiplier = 1.5,
						// A collection of status codes.(狀態代碼的集合。)
						// A gRPC call that fails with a matching status will be automatically retried.(失敗且狀態匹配的 gRPC 調用將自動重試。)
						// For more information about status codes, see Status codes and their use in gRPC.(有關狀態代碼的更多信息，請參閱狀態代碼及其在 gRPC 中的使用。)
						// At least one retryable status code is required.(至少需要一個可重試的狀態代碼。)
						RetryableStatusCodes = { StatusCode.Unavailable }
					}
				};
				configureChannel.HttpHandler = handler;
				configureChannel.ServiceConfig = new ServiceConfig
				{
					MethodConfigs = { defaultMethodConfig }
				};
				configureChannel.Credentials = isDns
				? ChannelCredentials.Insecure
				: null;
			})
			.AddInterceptor<LoggingInterceptor>(InterceptorScope.Client)
			.Services
			.AddSingleton<DiagnosticsInterceptorOptions>()
			.AddSingleton<LoggingInterceptor>()
			.AddTransient<IGameFundTransferClient, GameFundTransferClient>();

		if (isDns)
		{
			//負載平衡時，效能很重要。 藉由快取位址，解析位址的延遲會從 gRPC 呼叫中排除。 進行第一個 gRPC 呼叫時，將會叫用解析程式，而後續的呼叫會使用快取。
			services.TryAddSingleton<ResolverFactory>(sp => new DnsResolverFactory(refreshInterval: TimeSpan.FromSeconds(30)));
		}

		return services;
	}
}
