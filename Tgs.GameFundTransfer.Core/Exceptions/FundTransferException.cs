namespace Tgs.GameFundTransfer.Core.Exceptions;

public class FundTransferException(string gameProviderCode, string playerCode) : Exception($"This player({playerCode}) funds game provider({gameProviderCode}) transfer is locked.");
