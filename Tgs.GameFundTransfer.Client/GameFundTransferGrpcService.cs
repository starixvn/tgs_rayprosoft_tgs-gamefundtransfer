using Tgs.GameFundTransfer.Protos;

namespace Tgs.GameFundTransfer.Client
{
    internal class GameFundTransferGrpcService : IGameFundTransferGrpcService
    {
        private readonly GameFundTransferService.GameFundTransferServiceClient _client;

        public GameFundTransferGrpcService(GameFundTransferService.GameFundTransferServiceClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async ValueTask<bool> FundInAsync(GameFundTransferFundRequest request, CancellationToken cancellationToken = default)
        {
            var reply = await _client.FundInAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);

            return reply.State;
        }

        public async ValueTask<bool> FundOutAsync(GameFundTransferFundRequest request, CancellationToken cancellationToken = default)
        {
            var reply = await _client.FundOutAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);

            return reply.State;
        }
    }
}
