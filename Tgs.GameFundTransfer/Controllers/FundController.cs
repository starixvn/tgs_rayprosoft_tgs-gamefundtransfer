using Microsoft.AspNetCore.Mvc;
using Tgs.GameFundTransfer.Attributes;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Controllers;

[ApiController]
[Route("[controller]")]
public class FundController : ControllerBase
{
	/// <summary>
	/// Funds the transfer in asynchronous.
	/// </summary>
	/// <param name="service">The service.</param>
	/// <param name="source">The source.</param>
	/// <returns></returns>
	[HiddenApi]
	[HttpPost(template: "in", Name = "FundTransferIn")]
	public async ValueTask<FundTransferResultViewModel> FundTransferInAsync(
		[FromServices] IGetService<FundTransferInQuery, FundTransferResult> service,
		FundInRequestViewModel source)
	{
		var query = new FundTransferInQuery(
			GameProviderCode: source.GameProviderCode,
			PlayerCode: source.PlayerCode,
			WalletId: source.WalletId,
			CurrencyCode: source.CurrencyCode,
			Amount: source.Amount);

		var result = await service.GetAsync(
			query: query,
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
	/// <param name="source">The source.</param>
	/// <returns></returns>
	[HttpPost(template: "in-one-click", Name = "FundTransferInOneClick")]
	public async ValueTask<FundTransferResultViewModel> FundTransferInOneClickAsync(
		[FromServices] IGetService<FundTransferInOneClickQuery, FundTransferResult> service,
		FundTransferInOneClickViewModel source)
	{
		var query = new FundTransferInOneClickQuery(
			GameProviderCode: source.GameProviderCode,
			PlayerCode: source.PlayerCode,
			WalletId: source.WalletId,
			CurrencyCode: source.CurrencyCode);

		var result = await service.GetAsync(
			query: query,
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
	/// <param name="source">The source.</param>
	/// <returns></returns>
	[HiddenApi]
	[HttpPost(template: "out", Name = "FundTransferOut")]
	public async ValueTask<FundTransferResultViewModel> FundTransferOutAsync(
		[FromServices] IGetService<FundTransferOutQuery, FundTransferResult> service,
		FundOutRequestViewModel source)
	{
		var query = new FundTransferOutQuery(
			GameProviderCode: source.GameProviderCode,
			PlayerCode: source.PlayerCode,
			WalletId: source.WalletId,
			CurrencyCode: source.CurrencyCode,
			Amount: source.Amount);

		var result = await service.GetAsync(
			query: query,
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
	/// Funds the balance asynchronous.
	/// </summary>
	/// <param name="service">The service.</param>
	/// <param name="gameProviderCode">The game provider code.</param>
	/// <param name="playerCode">The player code.</param>
	/// <returns></returns>
	[HttpPost(template: "{gameProviderCode}/{playerCode}", Name = "FundBalance")]
	public async ValueTask<FundBalanceResultViewModel> FundBalanceAsync(
		[FromServices] IGetService<FundBalanceQuery, FundBalanceResult> service,
		string gameProviderCode,
		string playerCode)
	{
		var result = await service.GetAsync(new FundBalanceQuery(
			GameProviderCode: gameProviderCode,
			PlayerCode: playerCode))
			.ConfigureAwait(false);

		return new FundBalanceResultViewModel(
			result.Balance,
			result.FreeBalance,
			result.OutstandingBalance);
	}
}
