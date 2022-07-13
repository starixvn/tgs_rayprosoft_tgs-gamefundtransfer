using Grpc.Core;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.Protos;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.GrpcServices
{
    public class GameFundTransferGrpcService : GameFundTransferService.GameFundTransferServiceBase
    {
        private readonly ICommandService<FundInCommand> _fundInService;
        private readonly ICommandService<FundOutCommand> _fundOutService;

        public GameFundTransferGrpcService(
            ICommandService<FundInCommand> fundInService,
            ICommandService<FundOutCommand> fundOutService
            )
        {
            _fundInService = fundInService ?? throw new ArgumentNullException(nameof(fundInService));
            _fundOutService = fundOutService ?? throw new ArgumentNullException(nameof(fundOutService));
        }

        public override async Task<GameFundTransferReply> FundIn(GameFundTransferFundRequest request, ServerCallContext context)
        {
            var command = new FundInCommand(
                GameProviderId: request.GameProviderId,
                GameProviderCode: request.GameProviderCode,
                BrandCode: request.BrandCode,
                PlayerCode: request.PlayerCode,
                WalletId: request.WalletId,
                CurrencyCode: request.CurrencyCode,
                Amount: request.Amount);

            await _fundInService.ExecuteAsync(command: command).ConfigureAwait(false);

            return new GameFundTransferReply()
            {
                State = true
            };
        }

        public override async Task<GameFundTransferReply> FundOut(GameFundTransferFundRequest request, ServerCallContext context)
        {
            var command = new FundOutCommand(
                GameProviderId: request.GameProviderId,
                GameProviderCode: request.GameProviderCode,
                BrandCode: request.BrandCode,
                PlayerCode: request.PlayerCode,
                WalletId: request.WalletId,
                CurrencyCode: request.CurrencyCode,
                Amount: request.Amount);

            await _fundOutService.ExecuteAsync(command: command).ConfigureAwait(false);

            return new GameFundTransferReply()
            {
                State = true
            };
        }
    }
}
