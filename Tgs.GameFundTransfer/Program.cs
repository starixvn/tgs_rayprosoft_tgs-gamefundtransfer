using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NATS.Client.Core;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.GameFundTransfer;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.DocumentFilters;
using Tgs.GameFundTransfer.GrpcServices;
using Tgs.GameFundTransfer.Messages;
using Tgs.GamePlayerOnline.Client.DependencyInjection;
using Tgs.MessageQueue.Nats.Serializers;

var filterInstrumentations = new[] { "html", "ico", "png", "jpg" };

var builder = WebApplication.CreateBuilder(args);

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("Otel:NodeIp")!;
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("Otel:ServiceName")!;
			fluentOptions.UseMessagePack();
		});
	}
	);

// Add services to the container.
builder.Services
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers(options =>
	{
		options.Filters.Add(new ProducesAttribute(MediaTypeNames.Application.Json));
		options.Filters.Add(new ConsumesAttribute(MediaTypeNames.Application.Json));
	})
	.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddSingleton<DiagnosticsInterceptorOptions>();
builder.Services.AddSingleton<LoggingInterceptor>();
builder.Services.AddGrpc(options =>
{
	options.Interceptors.Add<LoggingInterceptor>();
	// If true, detailed exception messages are returned to clients when an exception is thrown in a service method.
	// The default is false. Setting EnableDetailedErrors to true can leak sensitive information.
	options.EnableDetailedErrors = true;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	foreach (var xmlFileName in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml"))
	{
		c.IncludeXmlComments(xmlFileName);
	}

	c.DocumentFilter<HiddenDocumentFilter>();
});

builder.Services.AddGameFundTransfer(
	(options, sp) =>
	{
		var configuration = sp.GetRequiredService<IConfiguration>();
		options.ConnectionString = configuration.GetConnectionString("Redis")!;
	},
	(sp, dbBuilder) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		_ = dbBuilder.UseNpgsql(
			config.GetConnectionString("Tgs"));
	},
	(options, sp) =>
	{
		var configuration = sp.GetRequiredService<IConfiguration>();
		configuration.GetSection("LockSeconds").Bind(options);
	},
	(options, sp) =>
	{
		var configuration = sp.GetRequiredService<IConfiguration>();
		options.BaseUrl = configuration.GetValue<Uri>("GameTransfer:CashServiceClientBaseUrl")!;
	},
	(options, sp) =>
	{
		var configuration = sp.GetRequiredService<IConfiguration>();
		options.BaseUrl = configuration.GetValue<Uri>("GameTransfer:GameInterfaceSabaClientBaseUrl")!;
	})
	.AddOptions<IPAddressOptions>()
	.Configure((options) => options.IPAddress = builder.Configuration.GetSection("POD_IP").Value
	?? Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault()?.ToString()
	?? "127.0.0.1")
	.Services
	.AddGameProvidersClient((options, sp) =>
	{
		var configuration = sp.GetRequiredService<IConfiguration>();
		options.BaseUrl = configuration.GetValue<Uri>("GameTransfer:GameProvidersClientBaseUrl")!;
	})
	.AddGameCurrencyClient(httpClientBuilder => httpClientBuilder.ConfigureHttpClient((sp, http) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		http.BaseAddress = config.GetValue<Uri>("GameTransfer:GameCurrencyClientBaseUrl");
	}))
	.AddGamePlayerOnlineClient(httpClientBuilder => httpClientBuilder.ConfigureHttpClient((sp, http) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		http.BaseAddress = config.GetValue<Uri>("GameTransfer:GamePlayerOnlineClientBaseUrl");
	}));

builder.Services
	.AddKeyedSingleton("Nats", (sp, _) => new NatsConnection(NatsOpts.Default with
	{
		Url = builder.Configuration.GetConnectionString("Nats")!,
		SerializerRegistry = TgsNatsSerializerRegistration.Default
	}))
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config
	.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats"))
	.AddHandler<GameFundTransferBalanceCacheMessageHandler>(Tgs.GameFundTransfer.Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest, typeof(GameFundTransferBalanceCacheMessageHandler).FullName!)
	.AddHandler<GameFundTransferBalanceOnlineMessageHandler>(Tgs.GameFundTransfer.Common.GameFundTransferNatsConst.GameFundTransferBalanceOnlineRequest, typeof(GameFundTransferBalanceOnlineMessageHandler).FullName!))
	.ClearAllExchanges()
	.AddNatsGlobPatternExchange(Tgs.GameFundTransfer.Common.GameFundTransferNatsConst.GameFundTransferBalanceCacheRequest)
	.AddNatsGlobPatternExchange(Tgs.GameFundTransfer.Common.GameFundTransferNatsConst.GameFundTransferBalanceOnlineRequest);

builder.Services
	.AddOpenTelemetry()
	.ConfigureResource(_ => _.AddService(builder.Configuration.GetValue<string>("Otel:ServiceName")))
	.WithTracing(tracingBuilder => tracingBuilder
		.SetResourceBuilder(ResourceBuilder.CreateDefault())
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddSource("Valhalla.*")
		.AddAspNetCoreInstrumentation(options => options.Filter =
			httpContext => !httpContext.Request.Path.HasValue
			|| !httpContext.Request.Path.StartsWithSegments("/swagger")
			&& !httpContext.Request.Path.StartsWithSegments("/health")
			&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
			&& !httpContext.Request.Path.StartsWithSegments("/hangfire")
			&& !httpContext.Request.Path.StartsWithSegments("/metrics")
			&& !filterInstrumentations.Any(ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
		.AddHttpClientInstrumentation()
		.AddGrpcClientInstrumentation()
		.AddGrpcCoreInstrumentation()
		.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
		.AddOtlpExporter(options => options.Endpoint = builder.Configuration.GetValue<Uri>("Otel:Collector")))
	.WithMetrics(meterBuiler => meterBuiler
	.SetResourceBuilder(ResourceBuilder.CreateDefault()
		.AddEnvironmentVariableDetector())
		.AddRuntimeInstrumentation()
		.AddAspNetCoreInstrumentation()
		.AddOtlpExporter())
	.Services
	.AddInterceptionService()
	.AddServiceUtiltiyInterceptors();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
_ = app.UseSwagger();
_ = app.UseSwaggerUI();

_ = app.UseAuthorization();

_ = app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/");

// Configure the HTTP request pipeline.
app.MapGrpcService<GameFundTransferGrpcService>();

_ = app.UseRegisterAsync();

app.Run();
