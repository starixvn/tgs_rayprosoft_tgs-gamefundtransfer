using Tgs.GameFundTransfer;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollecitonExtensions
{
    public static IServiceCollection AddGameFundTransfer(
        this IServiceCollection services,
        Action<RedisOptions, IServiceProvider> redisDBconfigure)
        => services
            .AddOptions<RedisOptions>()
            .Configure(redisDBconfigure)
            .Services
            .AddSingleton<RedisConnectionManager>();
}
