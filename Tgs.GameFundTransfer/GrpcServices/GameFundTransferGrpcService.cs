using Grpc.Core;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.Protos;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.GrpcServices;

public class GameFundTransferGrpcService(
	ILogger<GameFundTransferGrpcService> logger,
	IGetService<FundInQuery, FundResult> fundInResultService,
	IGetService<FundInZeroQuery, FundResult> fundInZeroService,
	ICommandService<FundOutCommand> fundOutService,
	IGetService<FundBalanceQuery, FundBalanceResult> fundBalanceService) : GameFundTransferService.GameFundTransferServiceBase
{
	public override Task<CheckingServiceReply> CheckingService(CheckingServiceRequest request, ServerCallContext context)
	{
		logger.LogWarning("LogOn:{logOn} Message: Create a check connection.({machineName})", DateTime.Now.ToString("o"), request.MachineName);

		return Task.FromResult(new CheckingServiceReply
		{
			MachineName = Environment.MachineName
		});
	}

	public override async Task<FundBalanceReply> FundBalance(FundBalanceRequest request, ServerCallContext context)
	{
		var query = new FundBalanceQuery(
			GameProviderCode: request.GameProviderCode,
			PlayerCode: request.PlayerCode);

		var result = await fundBalanceService.GetAsync(
			query: query,
			cancellationToken: context.CancellationToken)
			.ConfigureAwait(false);

		return new FundBalanceReply()
		{
			Balance = result.Balance,
			FreeBalance = result.FreeBalance,
			OutstandingBalance = result.OutstandingBalance,
		};
	}

	public override async Task<GameFundTransferResultReply> FundInResult(GameFundTransferFundRequest request, ServerCallContext context)
	{
		var query = new FundInQuery(
			GameProviderCode: request.GameProviderCode,
			PlayerCode: request.PlayerCode,
			WalletId: request.WalletId,
			CurrencyCode: request.CurrencyCode,
			Amount: request.Amount);

		var result = await fundInResultService.GetAsync(
			query: query,
			cancellationToken: context.CancellationToken)
			.ConfigureAwait(false);

		return new GameFundTransferResultReply()
		{
			State = result.State switch
			{
				Core.StateCode.Success => Protos.StateCode.Success,
				Core.StateCode.TransactionLock => Protos.StateCode.TransactionLock,
				Core.StateCode.ExternalTransactionIdEmpty => Protos.StateCode.ExternalTransactionIdEmpty,
				_ => Protos.StateCode.Exception
			},
			Message = result.Message,
			FundTransactionId = result.FundTransactionId,
			TransactionId = result.TransactionId,
			ExternalTransactionId = result.ExternalTransactionId
		};
	}

	public override async Task<GameFundTransferResultReply> FundInZeroResult(GameFundTransferFundZeroRequest request, ServerCallContext context)
	{
		var query = new FundInZeroQuery(
			GameProviderCode: request.GameProviderCode,
			PlayerCode: request.PlayerCode,
			WalletId: request.WalletId,
			CurrencyCode: request.CurrencyCode);

		var result = await fundInZeroService.GetAsync(
			query: query,
			cancellationToken: context.CancellationToken)
			 .ConfigureAwait(false);

		return new GameFundTransferResultReply()
		{
			State = result.State switch
			{
				Core.StateCode.Success => Protos.StateCode.Success,
				Core.StateCode.TransactionLock => Protos.StateCode.TransactionLock,
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
			GameProviderCode: request.GameProviderCode,
			PlayerCode: request.PlayerCode,
			WalletId: request.WalletId,
			CurrencyCode: request.CurrencyCode,
			Amount: request.Amount);

		await fundOutService.ExecuteAsync(
			command: command,
			cancellationToken: context.CancellationToken)
			.ConfigureAwait(false);

		return new GameFundTransferReply()
		{
			State = true
		};
	}
}
