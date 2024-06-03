using System.ComponentModel.DataAnnotations;

namespace Tgs.GameFundTransfer.ViewModels;

public record FundOutRequestViewModel
{
	[Required]
	public string GameProviderCode { get; init; } = default!;

	[Required]
	public string PlayerCode { get; init; } = default!;

	[Required]
	public Guid WalletId { get; init; }

	[Required]
	public string CurrencyCode { get; init; } = default!;

	[Required]
	[Range(minimum: 0, maximum: 10000000.00)]
	public decimal Amount { get; init; }
}
