using Grpc.Core;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.Protos;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.GrpcServices;

public class GameFundTransferGrpcService : GameFundTransferService.GameFundTransferServiceBase
{
    private readonly IGetService<FundInQuery, FundResult> _fundInResultService;
    private readonly ICommandService<FundInCommand> _fundInService;
    private readonly ICommandService<FundOutCommand> _fundOutService;

    public GameFundTransferGrpcService(
        ICommandService<FundInCommand> fundInService,
        ICommandService<FundOutCommand> fundOutService,
        IGetService<FundInQuery, FundResult> fundInResultService
        )
    {
        _fundInService = fundInService ?? throw new ArgumentNullException(nameof(fundInService));
        _fundOutService = fundOutService ?? throw new ArgumentNullException(nameof(fundOutService));
        _fundInResultService = fundInResultService ?? throw new ArgumentNullException(nameof(fundInResultService));
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

        await _fundInService
            .ExecuteAsync(
                command: command,
                cancellationToken: context.CancellationToken)
            .ConfigureAwait(false);

        return new GameFundTransferReply()
        {
            State = true
        };
    }

    public override async Task<GameFundTransferResultReply> FundInResult(GameFundTransferFundRequest request, ServerCallContext context)
    {
        var query = new FundInQuery(
                    GameProviderId: request.GameProviderId,
                    GameProviderCode: request.GameProviderCode,
                    BrandCode: request.BrandCode,
                    PlayerCode: request.PlayerCode,
                    WalletId: request.WalletId,
                    CurrencyCode: request.CurrencyCode,
                    Amount: request.Amount);

        var result = await _fundInResultService
             .GetAsync(
                 query: query,
                 cancellationToken: context.CancellationToken)
             .ConfigureAwait(false);

        return new GameFundTransferResultReply()
        {
            State = result.State switch
            {
                Core.StateCode.Success => Protos.StateCode.Success,
                Core.StateCode.TransactionLock => Protos.StateCode.TransactionLock,
                Core.StateCode.InsufficientBalance => Protos.StateCode.InsufficientBalance,
                Core.StateCode.ExternalTransactionIdEmpty => Protos.StateCode.ExternalTransactionIdEmpty,
                _ => Protos.StateCode.Exception
            },
            Message = result.Message,
            FundTransactionId = result.FundTransactionId,
            TransactionId = result.TransactionId,
            ExternalTransactionId = result.ExternalTransactionId
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

        await _fundOutService
            .ExecuteAsync(
                command: command,
                cancellationToken: context.CancellationToken)
            .ConfigureAwait(false);

        return new GameFundTransferReply()
        {
            State = true
        };
    }
}
