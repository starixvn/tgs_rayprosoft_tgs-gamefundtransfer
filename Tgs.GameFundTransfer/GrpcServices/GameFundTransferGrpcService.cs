using Grpc.Core;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.Protos;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.GrpcServices;

public class GameFundTransferGrpcService(
	IGetService<FundTransferInQuery, FundTransferResult> fundInResultService,
	IGetService<FundTransferInOneClickQuery, FundTransferResult> fundInZeroService,
	IGetService<FundTransferOutQuery, FundTransferResult> fundOutService,
	IGetService<FundBalanceQuery, FundBalanceResult> fundBalanceService) : Protos.GameFundTransfer.GameFundTransferBase
{
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

	public override async Task<FundTransferReply> FundTransferIn(FundTransferRequest request, ServerCallContext context)
	{
		var query = new FundTransferInQuery(
			GameProviderCode: request.GameProviderCode,
			PlayerCode: request.PlayerCode,
			WalletId: request.WalletId,
			CurrencyCode: request.CurrencyCode,
			Amount: request.Amount);

		var result = await fundInResultService.GetAsync(
			query: query,
			cancellationToken: context.CancellationToken)
			.ConfigureAwait(false);

		return new FundTransferReply()
		{
			FundTransactionId = result.FundTransactionId,
			CashTransactionId = result.CashTransactionId,
			CashRespTransactionId = result.CashRespTransactionId,
			GameTransactionId = result.GameTransactionId,
			GameRespTransactionId = result.GameRespTransactionId,
		};
	}

	public override async Task<FundTransferReply> FundTransferInOneClick(FundTransferInOneClickRequest request, ServerCallContext context)
	{
		var query = new FundTransferInOneClickQuery(
			GameProviderCode: request.GameProviderCode,
			PlayerCode: request.PlayerCode,
			WalletId: request.WalletId,
			CurrencyCode: request.CurrencyCode);

		var result = await fundInZeroService.GetAsync(
			query: query,
			cancellationToken: context.CancellationToken)
			 .ConfigureAwait(false);

		return new FundTransferReply()
		{
			FundTransactionId = result.FundTransactionId,
			CashTransactionId = result.CashTransactionId,
			CashRespTransactionId = result.CashRespTransactionId,
			GameTransactionId = result.GameTransactionId,
			GameRespTransactionId = result.GameRespTransactionId,
		};
	}

	public override async Task<FundTransferReply> FundTransferOut(FundTransferRequest request, ServerCallContext context)
	{
		var query = new FundTransferOutQuery(
			GameProviderCode: request.GameProviderCode,
			PlayerCode: request.PlayerCode,
			WalletId: request.WalletId,
			CurrencyCode: request.CurrencyCode,
			Amount: request.Amount);

		var result = await fundOutService.GetAsync(
			query: query,
			cancellationToken: context.CancellationToken)
			.ConfigureAwait(false);

		return new FundTransferReply()
		{
			FundTransactionId = result.FundTransactionId,
			CashTransactionId = result.CashTransactionId,
			CashRespTransactionId = result.CashRespTransactionId,
			GameTransactionId = result.GameTransactionId,
			GameRespTransactionId = result.GameRespTransactionId,
		};
	}
}
