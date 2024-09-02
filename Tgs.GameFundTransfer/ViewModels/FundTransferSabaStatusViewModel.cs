using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Tgs.GameFundTransfer.ViewModels;

public record FundTransferSabaStatusViewModel
{
	[Required]
	[DefaultValue(typeof(Uri), "http://h0i2tsa.bw6688.com/")]
	public Uri ApiUrl { get; init; } = default!;
	[Required]
	[DefaultValue(typeof(string), "tg")]
	public string OperatorId { get; init; } = default!;
	[Required]
	[DefaultValue(typeof(string), "um5vspnzkx")]
	public string VendorId { get; init; } = default!;
	[Required]
	[DefaultValue(typeof(string), "32fd88b08401000000010000010a3742")]
	public string OrderId { get; init; } = default!;
}
