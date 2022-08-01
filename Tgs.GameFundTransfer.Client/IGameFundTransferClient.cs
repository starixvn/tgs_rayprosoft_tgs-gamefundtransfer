using Tgs.GameFundTransfer.Protos;

namespace Tgs.GameFundTransfer.Client
{
    public interface IGameFundTransferClient
    {
        ValueTask<bool> FundInAsync(GameFundTransferFundRequest request, CancellationToken cancellationToken = default);

        ValueTask<bool> FundOutAsync(GameFundTransferFundRequest request, CancellationToken cancellationToken = default);
    }
}
