using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Tgs.GameFundTransfer.Common.ViewModels;
public record FundTransferViewModel
{
	[Required]
	[Range(minimum: 0, maximum: 10000000.00)]
	[DefaultValue(0)]
	public decimal Amount { get; init; }
}
