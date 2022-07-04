using Tgs.GameFundTransfer.Protos;

namespace Tgs.GameFundTransfer.Client
{
    internal class GameTransferGrpcService : IGameTransferGrpcService
    {
        private readonly GameFundTransferService.GameFundTransferServiceClient _client;

        public GameTransferGrpcService(GameFundTransferService.GameFundTransferServiceClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async ValueTask<bool> FundIn(GameFundTransferFundRequest request, CancellationToken cancellationToken = default)
        {
            var reply = await _client.FundInAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);

            return reply.State;
        }

        public async ValueTask<bool> FundOut(GameFundTransferFundRequest request, CancellationToken cancellationToken = default)
        {
            var reply = await _client.FundOutAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);

            return reply.State;
        }
    }
}
