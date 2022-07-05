using Microsoft.EntityFrameworkCore;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Repositories;
using Tgs.GameFundTransfer.Repositories.Entities;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGameTransferRepository(this IServiceCollection services,
            Action<IServiceProvider, DbContextOptionsBuilder> efConfigure)
            => services
                .AddSerialNumberGenerator()
                .AddDbContext<TgsContext>(efConfigure)
                .AddTransient<IFundTransferTransactionRepository, FundTransferTransactionRepository>();
    }
}
