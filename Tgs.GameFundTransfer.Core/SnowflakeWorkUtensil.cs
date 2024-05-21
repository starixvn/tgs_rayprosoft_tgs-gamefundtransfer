using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snowflake.Core;

namespace Tgs.GameFundTransfer.Core;

internal class SnowflakeWorkUtensil : ISnowflakeWorkUtensil
{
	private readonly IdWorker _idWorker;
	private readonly IPAddress _iPAddress;
	private readonly ILogger<SnowflakeWorkUtensil> _logger;

	public SnowflakeWorkUtensil(
		ILogger<SnowflakeWorkUtensil> logger,
		IOptions<IPAddressOptions> options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		_iPAddress = IPAddress.Parse(options.Value.IPAddress);

		var address = _iPAddress.IsIPv6Teredo
			? (long)BitConverter.ToUInt64(IPAddress.Parse(options.Value.IPAddress).GetAddressBytes())
			: BitConverter.ToUInt32(IPAddress.Parse(options.Value.IPAddress).GetAddressBytes());

		var workerId = (address % 255 & 30) + 1;
		var datacenterId = ((address >> 5) % 255 & 30) + 1;
		_logger.LogInformation("IpAddress:{ipAddress} | Address:{address} | WorkerId:{workerId} | DatacenterId:{datacenterId}",
			_iPAddress,
			address,
			workerId,
			datacenterId);
		_idWorker = new IdWorker(workerId, datacenterId);
	}

	public long GetDatacenterId()
		=> _idWorker.DatacenterId;

	public long GetId()
		=> _idWorker.NextId();

	public IPAddress GetIPAddress()
				=> _iPAddress;

	public long GetWorkerId()
		=> _idWorker.WorkerId;
}
