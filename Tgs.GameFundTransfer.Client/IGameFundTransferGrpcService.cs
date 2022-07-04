using Tgs.GameFundTransfer.Protos;

namespace Tgs.GameFundTransfer.Client
{
    public interface IGameFundTransferGrpcService
    {
        ValueTask<bool> FundIn(GameFundTransferFundRequest request, CancellationToken cancellationToken = default);

        ValueTask<bool> FundOut(GameFundTransferFundRequest request, CancellationToken cancellationToken = default);
    }
}
