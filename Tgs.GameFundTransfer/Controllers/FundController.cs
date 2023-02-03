using Microsoft.AspNetCore.Mvc;
using Tgs.GameFundTransfer.Attributes;
using Tgs.GameFundTransfer.Core.ApplicationServices;
using Tgs.GameFundTransfer.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.GameFundTransfer.Controllers;

[HiddenApi]
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
    [HttpPost(template: "in", Name = "FundIn")]
    public ValueTask FundInAsync(
        [FromServices] ICommandService<FundInCommand> service,
        FundRequestViewModel model)
        => service.ExecuteAsync(new FundInCommand(
            GameProviderId: model.GameProviderId,
            GameProviderCode: model.GameProviderCode,
            BrandCode: model.BrandCode,
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
    [HttpPost(template: "in-result", Name = "FundInResult")]
    public async ValueTask<FundResultViewModel> FundInResultAsync(
        [FromServices] IGetService<FundInQuery, FundReslut> service,
        FundRequestViewModel model)
    {
        var query = new FundInQuery(
            GameProviderId: model.GameProviderId,
            GameProviderCode: model.GameProviderCode,
            BrandCode: model.BrandCode,
            PlayerCode: model.PlayerCode,
            WalletId: model.WalletId,
            CurrencyCode: model.CurrencyCode,
            Amount: model.Amount);
        var reslut = await service
             .GetAsync(
                 query: query,
                 cancellationToken: HttpContext.RequestAborted)
             .ConfigureAwait(false);

        return new FundResultViewModel(
            reslut.State,
            reslut.Message,
            reslut.FundTransactionId,
            reslut.TransactionId,
            reslut.ExternalTransactionId);
    }

    /// <summary>
    /// Funds the out asynchronous.
    /// </summary>
    /// <param name="service">The service.</param>
    /// <param name="model">The model.</param>
    /// <returns></returns>
    [HttpPost(template: "out", Name = "FundOut")]
    public ValueTask FundOutAsync(
        [FromServices] ICommandService<FundOutCommand> service,
        FundRequestViewModel model)
        => service.ExecuteAsync(new FundOutCommand(
            GameProviderId: model.GameProviderId,
            GameProviderCode: model.GameProviderCode,
            BrandCode: model.BrandCode,
            PlayerCode: model.PlayerCode,
            WalletId: model.WalletId,
            CurrencyCode: model.CurrencyCode,
            Amount: model.Amount));
}
