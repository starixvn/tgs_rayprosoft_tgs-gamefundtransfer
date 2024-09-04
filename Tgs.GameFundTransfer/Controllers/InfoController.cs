using System.Net.Sockets;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tgs.GameFundTransfer.Core;
using System.Text.Json.Nodes;

namespace Tgs.GameFundTransfer.Controllers;

[Route("[controller]")]
[ApiController]
public class InfoController : ControllerBase
{
	/// <summary>
	/// Gets the asynchronous.
	/// </summary>
	/// <param name="webHostEnvironment">The web host environment.</param>
	/// <param name="configuration">The configuration.</param>
	/// <param name="fundsLockedOptions">The funds locked options.</param>
	/// <param name="ipAddressOptions">The ip address options.</param>
	/// <returns></returns>
	[HttpGet]
	public async Task<object> GetAsync(
		[FromServices] IWebHostEnvironment webHostEnvironment,
		[FromServices] IConfiguration configuration,
		[FromServices] IOptions<FundsLockedOptions> fundsLockedOptions,
		[FromServices] IOptions<IPAddressOptions> ipAddressOptions)
	{
		var hostName = Dns.GetHostName();
		var hostEntry = await Dns.GetHostEntryAsync(hostName).ConfigureAwait(false);
		var hostIp = Array.Find(hostEntry.AddressList,
			x => x.AddressFamily == AddressFamily.InterNetwork);

		return new
		{
			Environment.MachineName,
			HostName = hostName,
			HostIp = hostIp?.ToString() ?? string.Empty,
			Environment = webHostEnvironment.EnvironmentName,
			OsVersion = $"{Environment.OSVersion}",
			Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
			ProcessCount = Environment.ProcessorCount,
			GameTransferConnectionStrings = configuration.GetSection("GameTransferConnectionStrings").Get<Dictionary<string, string>>(),
			FundsLockedOptions = fundsLockedOptions.Value,
			IPAddressOptions = ipAddressOptions.Value
		};
	}
}
