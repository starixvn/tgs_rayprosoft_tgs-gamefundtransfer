using System.ComponentModel.DataAnnotations;

namespace Tgs.GameFundTransfer.ViewModels
{
    public record FundRequestViewModel
    {
        [Required]
        public Guid GameProviderId { get; init; } = default!;

        [Required]
        public string GameProviderCode { get; init; } = default!;

        [Required]
        public string BrandCode { get; init; } = default!;

        [Required]
        public string PlayerCode { get; init; } = default!;

        [Required]
        public Guid WalletId { get; init; }

        [Required]
        public string CurrencyCode { get; init; } = default!;

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal Amount { get; init; }
    }
}
