using Tgs.GameFundTransfer.Protos;

namespace Tgs.GameFundTransfer.Client
{
    public interface IGameTransferGrpcService
    {
        ValueTask<bool> FundIn(GameFundTransferFundRequest request, CancellationToken cancellationToken = default);

        ValueTask<bool> FundOut(GameFundTransferFundRequest request, CancellationToken cancellationToken = default);
    }
}
