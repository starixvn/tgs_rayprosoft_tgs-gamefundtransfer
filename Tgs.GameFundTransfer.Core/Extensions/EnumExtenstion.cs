using System.ComponentModel;

namespace Tgs.GameFundTransfer.Core.Extensions;

public static class EnumExtenstion
{
	public static string GetDescription(this Enum value)
	{
		var field = value.GetType().GetField(value.ToString());
		return field?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() is not DescriptionAttribute attribute
			? value.ToString()
			: attribute.Description;
	}
}
