using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Tgs.GameFundTransfer
{
    internal class LoggingInterceptor : Interceptor
    {
        private readonly ILogger _logger;
        private readonly DiagnosticsInterceptorOptions _options;

        public LoggingInterceptor(
            ILogger<LoggingInterceptor> logger,
             DiagnosticsInterceptorOptions options)
        {
            _logger = logger;
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var methodType = MethodType.Unary;
            var method = context.Method;

            using var activity = _options.InterceptorActivitySource.StartActivity($"{methodType}.{method}");
            try
            {
                _ = activity?.Start();
                _logger.LogInformation("TraceId:{traceId} LogOn:{logOn}  MethodType:{methodType} Method:{method} Request:{request}", activity?.TraceId.ToString(), DateTime.Now.ToString("o"), methodType, method, request);

                var response = await continuation(request, context);

                activity?.Stop();
                _logger.LogInformation("TraceId:{traceId} LogOn:{logOn}  MethodType:{methodType} Method:{method} Spend:{spend:0.0000}ms Response:{response}", activity?.TraceId.ToString(), DateTime.Now.ToString("o"), methodType, method, activity?.Duration.TotalMilliseconds, response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error thrown by {context.Method}.", method);
                throw;
            }
        }
    }
}
