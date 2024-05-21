using Tgs.GameFundTransfer.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceRegisterExtensions
{
	public static async Task<WebApplication> UseRegisterAsync(this WebApplication app)
	{
		using (var scope = app.Services.CreateScope())
		{
			var gameProviderRepository = scope.ServiceProvider.GetRequiredService<IGameProviderRepository>();

			var gameProviderCodes = await gameProviderRepository.QueryAsync()
				.ToArrayAsync()
				.ConfigureAwait(false)
				?? Enumerable.Empty<string>();

			foreach (var gameProviderCode in gameProviderCodes)
			{
				_ = await gameProviderRepository.GetAsync(
					gameProviderCode: gameProviderCode)
					.ConfigureAwait(false);
			}
		}

		return app;
	}
}
