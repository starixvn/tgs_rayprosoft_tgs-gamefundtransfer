using System.Diagnostics;

namespace Tgs.GameFundTransfer.Client;

public class DiagnosticsInterceptorOptions
{
    public ActivitySource InterceptorActivitySource { get; set; } =
        new ActivitySource($"Tgs.GameFundTransfer.Client.DiagnosticsInterceptor");
}
