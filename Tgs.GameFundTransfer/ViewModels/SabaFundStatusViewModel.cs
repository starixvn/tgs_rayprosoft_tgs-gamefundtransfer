using System.ComponentModel.DataAnnotations;

namespace Tgs.GameFundTransfer.ViewModels;

public record SabaFundStatusViewModel
{
    [Required]
    public Uri ApiUrl { get; init; } = default!;
    [Required]
    public string OperatorId { get; init; } = default!;
    [Required]
    public string VendorId { get; init; } = default!;
    [Required]
    public string VendorTransId { get; init; } = default!;
}
