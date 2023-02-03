using Grpc.Core;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.Protos;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.GrpcServices;

public class GameFundTransferGrpcService : GameFundTransferService.GameFundTransferServiceBase
{
    private readonly IGetService<FundInQuery, FundReslut> _fundInReslutService;
    private readonly ICommandService<FundInCommand> _fundInService;
    private readonly ICommandService<FundOutCommand> _fundOutService;

    public GameFundTransferGrpcService(
        ICommandService<FundInCommand> fundInService,
        ICommandService<FundOutCommand> fundOutService,
        IGetService<FundInQuery, FundReslut> fundInReslutService
        )
    {
        _fundInService = fundInService ?? throw new ArgumentNullException(nameof(fundInService));
        _fundOutService = fundOutService ?? throw new ArgumentNullException(nameof(fundOutService));
        _fundInReslutService = fundInReslutService ?? throw new ArgumentNullException(nameof(fundInReslutService));
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

    public override async Task<GameFundTransferReslutReply> FundInReslut(GameFundTransferFundRequest request, ServerCallContext context)
    {
        var query = new FundInQuery(
                    GameProviderId: request.GameProviderId,
                    GameProviderCode: request.GameProviderCode,
                    BrandCode: request.BrandCode,
                    PlayerCode: request.PlayerCode,
                    WalletId: request.WalletId,
                    CurrencyCode: request.CurrencyCode,
                    Amount: request.Amount);

        var reslut = await _fundInReslutService
             .GetAsync(
                 query: query,
                 cancellationToken: context.CancellationToken)
             .ConfigureAwait(false);

        return new GameFundTransferReslutReply()
        {
            State = reslut.State switch
            {
                Core.StateCode.Success => StateCode.Success,
                Core.StateCode.TransactionLock => StateCode.TransactionLock,
                Core.StateCode.InsufficientBalance => StateCode.InsufficientBalance,
                _ => StateCode.Exception
            },
            Message = reslut.Message,
            FundTransactionId = reslut.FundTransactionId,
            TransactionId = reslut.TransactionId,
            ExternalTransactionId = reslut.ExternalTransactionId
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
