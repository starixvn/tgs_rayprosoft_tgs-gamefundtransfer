namespace Tgs.GameFundTransfer.ViewModels
{
    public record FundRequestViewModel(
        Guid GameProviderId,
        string GameProviderCode,
        string BrandCode,
        string PlayerCode,
        Guid WalletId,
        string CurrencyCode,
        decimal Amount);
}
