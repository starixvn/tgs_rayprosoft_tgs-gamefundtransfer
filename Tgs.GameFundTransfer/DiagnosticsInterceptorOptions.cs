using System.Diagnostics;

namespace Tgs.GameFundTransfer
{
    public class DiagnosticsInterceptorOptions
    {
        public ActivitySource InterceptorActivitySource { get; set; } =
            new ActivitySource($"Tgs.GameTransfer.DiagnosticsInterceptor");
    }
}
