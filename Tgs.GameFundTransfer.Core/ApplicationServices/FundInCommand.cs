namespace Tgs.GameFundTransfer.Core.ApplicationServices
{
    public record FundInCommand(
        Guid GameProviderId,
        string GameProviderCode,
        string BrandCode,
        string PlayerCode,
        Guid WalletId,
        string CurrencyCode,
        decimal Amount);
}
