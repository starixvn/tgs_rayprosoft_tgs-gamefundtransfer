namespace Tgs.GameFundTransfer.Core.ApplicationServices;
public record FundProcessingResult(
	Guid BrandId,
	string BrandCode,
	string PlayerCode,
	string Username,
	Guid GameProviderId,
	string GameProviderCode,
	Guid GameProviderFundTransferTransactionId,
	string GameProviderTransactionId);
