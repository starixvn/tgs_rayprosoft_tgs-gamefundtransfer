using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenTelemetry.Exporter;
using StackExchange.Redis;
using Tgs.GameFundTransfer.Entities;

namespace Tgs.GameFundTransfer.IntegrationTest;
internal class GameFundTransferApplication(Action<IWebHostBuilder>? webHostConfigure = null) : WebApplicationFactory<Program>
{
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		_ = builder
			.ConfigureLogging(logging => logging
				.ClearProviders()
				.AddConsole())
			.ConfigureServices(services => services
				.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
					sp => new ConfigureNamedOptions<OtlpExporterOptions>(
						Options.DefaultName,
						exporterOptions => exporterOptions.TimeoutMilliseconds = 0))

				.AddFakeNatsMessageQueue()
				.Replace(ServiceDescriptor.Singleton(sp =>
				{
					var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsContext>();
					_ = dbContextOptionsBuilder
						.UseInMemoryDatabase(Guid.NewGuid().ToString())
						.ConfigureWarnings(x => x.Ignore(
							InMemoryEventId.TransactionIgnoredWarning));

					return new TgsContext(dbContextOptionsBuilder.Options);
				}))
				.AddKeyedSingleton("Redis", Substitute.For<IDatabase>())
				.AddKeyedSingleton("Redis", Substitute.For<IConnectionMultiplexer>()));

		webHostConfigure?.Invoke(builder);
	}
}
