using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Repositories;
using Tgs.GameFundTransfer.Repositories.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameTransferRepository(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> efConfigure,
        Func<IServiceProvider, IDatabase> redisDatabaseResolver)
        => services
            .AddSerialNumberGenerator()
            .AddDbContext<TgsContext>(efConfigure)
            .AddTransient<IFundsDistributedLock>(sp => ActivatorUtilities.CreateInstance<FundsDistributedLock>(sp, redisDatabaseResolver(sp)))
            .AddTransient<IFundTransferTransactionRepository, FundTransferTransactionRepository>();
}
