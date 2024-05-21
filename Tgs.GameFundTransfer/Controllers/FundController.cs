using Microsoft.AspNetCore.Mvc;
using Tgs.GameFundTransfer.Attributes;
using Tgs.GameFundTransfer.Core;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Controllers;

[ApiController]
[Route("[controller]")]
public class FundController : ControllerBase
{
	/// <summary>
	/// Funds the in asynchronous.
	/// </summary>
	/// <param name="service">The service.</param>
	/// <param name="model">The model.</param>
	/// <returns></returns>
	[HiddenApi]
	[HttpPost(template: "in", Name = "FundIn")]
	public ValueTask FundInAsync(
		[FromServices] ICommandService<FundInCommand> service,
		FundRequestViewModel model)
		=> service.ExecuteAsync(new FundInCommand(
			GameProviderCode: model.GameProviderCode,
			PlayerCode: model.PlayerCode,
			WalletId: model.WalletId,
			CurrencyCode: model.CurrencyCode,
			Amount: model.Amount));

	/// <summary>
	/// Funds the in result asynchronous.
	/// </summary>
	/// <param name="service">The service.</param>
	/// <param name="model">The model.</param>
	/// <returns></returns>
	[HiddenApi]
	[HttpPost(template: "in-result", Name = "FundInResult")]
	public async ValueTask<FundResultViewModel> FundInResultAsync(
		[FromServices] IGetService<FundInQuery, FundResult> service,
		FundRequestViewModel model)
	{
		var query = new FundInQuery(
			GameProviderCode: model.GameProviderCode,
			PlayerCode: model.PlayerCode,
			WalletId: model.WalletId,
			CurrencyCode: model.CurrencyCode,
			Amount: model.Amount);
		var result = await service
			 .GetAsync(
				 query: query,
				 cancellationToken: HttpContext.RequestAborted)
			 .ConfigureAwait(false);

		return new FundResultViewModel(
			result.State,
			result.Message,
			result.FundTransactionId,
			result.TransactionId,
			result.ExternalTransactionId);
	}

	/// <summary>
	/// Funds the in zero result asynchronous.
	/// </summary>
	/// <param name="service">The service.</param>
	/// <param name="model">The model.</param>
	/// <returns></returns>
	[HttpPost(template: "in-result-zero", Name = "FundInZeroResult")]
	public async ValueTask<FundResultViewModel> FundInZeroResultAsync(
		[FromServices] IGetService<FundInZeroQuery, FundResult> service,
		FundZeroRequestViewModel model)
	{
		var query = new FundInZeroQuery(
			GameProviderCode: model.GameProviderCode,
			PlayerCode: model.PlayerCode,
			WalletId: model.WalletId,
			CurrencyCode: model.CurrencyCode);
		var result = await service
			 .GetAsync(
				 query: query,
				 cancellationToken: HttpContext.RequestAborted)
			 .ConfigureAwait(false);

		return new FundResultViewModel(
			result.State,
			result.Message,
			result.FundTransactionId,
			result.TransactionId,
			result.ExternalTransactionId);
	}

	/// <summary>
	/// Funds the out asynchronous.
	/// </summary>
	/// <param name="service">The service.</param>
	/// <param name="model">The model.</param>
	/// <returns></returns>
	[HiddenApi]
	[HttpPost(template: "out", Name = "FundOut")]
	public ValueTask FundOutAsync(
		[FromServices] ICommandService<FundOutCommand> service,
		FundOutRequestViewModel model)
		=> service.ExecuteAsync(new FundOutCommand(
			GameProviderCode: model.GameProviderCode,
			PlayerCode: model.PlayerCode,
			WalletId: model.WalletId,
			CurrencyCode: model.CurrencyCode,
			Amount: model.Amount));

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
