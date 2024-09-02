using System.ComponentModel;

using System.ComponentModel.DataAnnotations;

namespace Tgs.GameFundTransfer.Common.ViewModels;
public record FundTransferInOneClickViewModel
{
	[Required]
	[DefaultValue(typeof(string), "CQ9")]
	public string GameProviderCode { get; init; } = default!;
	[Required]
	[DefaultValue(typeof(string), "4ucmqplxb029")]
	public string PlayerCode { get; init; } = default!;
}
