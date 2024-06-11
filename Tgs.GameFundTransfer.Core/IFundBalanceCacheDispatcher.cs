using Tgs.GameFundTransfer.Core.ApplicationServices;

namespace Tgs.GameFundTransfer.Core;

public interface IFundBalanceCacheDispatcher
{
	ValueTask PushCommandAsync(FundBalanceCacheCommand command, CancellationToken cancellationToken = default);
}
