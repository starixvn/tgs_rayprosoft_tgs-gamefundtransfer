using System.Net;

namespace Tgs.GameFundTransfer.Core;

public interface ISnowflakeWorkUtensil
{
	long GetDatacenterId();

	long GetId();

	IPAddress GetIPAddress();

	long GetWorkerId();
}
