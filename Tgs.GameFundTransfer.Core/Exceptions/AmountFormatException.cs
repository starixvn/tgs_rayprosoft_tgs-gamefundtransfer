namespace Tgs.GameFundTransfer.Core.Exceptions;

public class AmountFormatException(decimal amount) : Exception($"The field Amount must be between 0 and 79228162514264337593543950335.({amount})")
{
}
