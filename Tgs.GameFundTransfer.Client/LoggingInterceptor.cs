using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Tgs.GameFundTransfer.Client;

internal class LoggingInterceptor(
	ILogger<LoggingInterceptor> logger,
	DiagnosticsInterceptorOptions options) : Interceptor
{
	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
	{
		var methodType = context.Method.Type;
		var method = context.Method.Name;

		using var activity = options.InterceptorActivitySource.StartActivity($"{methodType}.{method}");
		try
		{
			_ = activity?.Start();
			logger.LogInformation("TraceId:{traceId} LogOn:{logOn}  MethodType:{methodType} Method:{method} Request:{request}", activity?.TraceId.ToString(), DateTime.Now.ToString("o"), methodType, method, request);

			var response = continuation(request, context);

			activity?.Stop();
			logger.LogInformation("TraceId:{traceId} LogOn:{logOn}  MethodType:{methodType} Method:{method} Spend:{spend:0.0000}ms Response:{response}", activity?.TraceId.ToString(), DateTime.Now.ToString("o"), methodType, method, activity?.Duration.TotalMilliseconds, response);
			return response;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error thrown by {context.Method}.", method);
			throw;
		}
	}
}
