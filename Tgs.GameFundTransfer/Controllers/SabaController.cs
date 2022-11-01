using Microsoft.AspNetCore.Mvc;
using Tgs.GameFundTransfer.ViewModels;
using Tgs.GameInterface.SabaService.Client;
using Tgs.GameInterface.SabaService.Protos;

namespace Tgs.GameFundTransfer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SabaController : ControllerBase
    {
        /// <summary>
        /// Sabas the fund status asynchronous.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        /// <remarks>
        /// <b>Example</b><br />
        ///
        ///     Post
        ///
        ///         {
        ///             "apiUrl": "http://h0i2tsa.bw6688.com/",
        ///             "operatorId": "tg",
        ///             "vendorId": "um5vspnzkx",
        ///             "vendorTransId": "49ED28A987624CA0A3F834C3B8A69AA1"
        ///         }
        /// </remarks>
        [HttpPost("fund/Status", Name = "FundStatus")]
        public ValueTask<FundStatusReply> SabaFundStatusAsync(
            [FromServices] IGameInterfaceSabaClient client,
            SabaFundStatusViewModel source
            )
            => client.FundStatusAsync(
                apiUrl: source.ApiUrl,
                operatorId: source.OperatorId,
                vendorId: source.VendorId,
                vendorTransId: source.VendorTransId,
                cancellationToken: HttpContext.RequestAborted);
    }
}
