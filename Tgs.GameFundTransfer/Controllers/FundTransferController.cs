using Microsoft.AspNetCore.Mvc;
using Tgs.GameFundTransfer.Common.ViewModels;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Controllers;

[ApiController]
[Route("[controller]")]
public class FundTransferController : ControllerBase
{
	/// <summary>
	/// Funds the transfer in asynchronous.
	/// </summary>
	/// <param name="service">The service.</param>
	/// <param name="playerCode">The player code.</param>
	/// <param name="gameProviderCode">The game provider code.</param>
	/// <param name="source">The source.</param>
	/// <returns></returns>
	[HttpPost(template: "{playerCode}/{gameProviderCode}/in", Name = "FundTransfer_In")]
	public async ValueTask<FundTransferResultViewModel> FundTransferInAsync(
		[FromServices] IGetService<FundTransferInQuery, FundTransferResult> service,
		string playerCode,
		string gameProviderCode,
		[FromBody] FundTransferViewModel source)
	{
		var result = await service.GetAsync(
			query: new FundTransferInQuery(
				GameProviderCode: gameProviderCode,
				PlayerCode: playerCode,
				Amount: source.Amount),
			cancellationToken: HttpContext.RequestAborted)
			 .ConfigureAwait(false);

		return new FundTransferResultViewModel(
			result.FundTransactionId,
			result.CashTransactionId,
			result.CashRespTransactionId,
			result.GameTransactionId,
			result.GameRespTransactionId);
	}

	/// <summary>
	/// Funds the transfer in one click asynchronous.
	/// </summary>
	/// <param name="service">The service.</param>
	/// <param name="playerCode">The player code.</param>
	/// <param name="gameProviderCode">The game provider code.</param>
	/// <returns></returns>
	[HttpPost(template: "{playerCode}/{gameProviderCode}/in-one-click", Name = "FundTransfer_InOneClick")]
	public async ValueTask<FundTransferResultViewModel> FundTransferInOneClickAsync(
		[FromServices] IGetService<FundTransferInOneClickQuery, FundTransferResult> service,
		string playerCode,
		string gameProviderCode)
	{
		var result = await service.GetAsync(
			query: new FundTransferInOneClickQuery(
				GameProviderCode: gameProviderCode,
				PlayerCode: playerCode),
			cancellationToken: HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new FundTransferResultViewModel(
			result.FundTransactionId,
			result.CashTransactionId,
			result.CashRespTransactionId,
			result.GameTransactionId,
			result.GameRespTransactionId);
	}

	/// <summary>
	/// Funds the transfer out asynchronous.
	/// </summary>
	/// <param name="service">The service.</param>
	/// <param name="playerCode">The player code.</param>
	/// <param name="gameProviderCode">The game provider code.</param>
	/// <param name="source">The source.</param>
	/// <returns></returns>
	[HttpPost(template: "{playerCode}/{gameProviderCode}/out", Name = "FundTransfer_Out")]
	public async ValueTask<FundTransferResultViewModel> FundTransferOutAsync(
		[FromServices] IGetService<FundTransferOutQuery, FundTransferResult> service,
		string playerCode,
		string gameProviderCode,
		[FromBody] FundTransferViewModel source)
	{
		var result = await service.GetAsync(
			query: new FundTransferOutQuery(
				GameProviderCode: gameProviderCode,
				PlayerCode: playerCode,
				Amount: source.Amount),
			cancellationToken: HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new FundTransferResultViewModel(
			result.FundTransactionId,
			result.CashTransactionId,
			result.CashRespTransactionId,
			result.GameTransactionId,
			result.GameRespTransactionId);
	}

	/// <summary>
	/// Funds the transfer balance asynchronous.
	/// </summary>
	/// <param name="service">The service.</param>
	/// <param name="playerCode">The player code.</param>
	/// <param name="gameProviderCode">The game provider code.</param>
	/// <returns></returns>
	[HttpGet(template: "{playerCode}/{gameProviderCode}/balance", Name = "FundTransfer_Balance")]
	public async ValueTask<FundTransferBalanceResultViewModel> FundTransferBalanceAsync(
		[FromServices] IGetService<FundBalanceQuery, FundBalanceResult> service,
		string playerCode,
		string gameProviderCode)
	{
		var result = await service.GetAsync(
			query: new FundBalanceQuery(
				GameProviderCode: gameProviderCode,
				PlayerCode: playerCode),
			cancellationToken: HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new FundTransferBalanceResultViewModel(
			result.Balance,
			result.FreeBalance,
			result.OutstandingBalance);
	}
}
