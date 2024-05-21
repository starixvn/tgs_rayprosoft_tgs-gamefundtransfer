using Microsoft.AspNetCore.Mvc;
using Tgs.GameFundTransfer.Core;

namespace Tgs.GameFundTransfer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SnowflakeController : ControllerBase
{
	[HttpGet("DatacenterId")]
	public long GetDatacenterId(
		[FromServices] ISnowflakeWorkUtensil work)
		=> work.GetDatacenterId();

	[HttpGet("Id")]
	public long GetId(
		[FromServices] ISnowflakeWorkUtensil work)
		=> work.GetId();

	[HttpGet("IPAddress")]
	public string GetIPAddress(
		[FromServices] ISnowflakeWorkUtensil work)
		=> work.GetIPAddress().ToString();

	[HttpGet("WorkerId")]
	public long GetWorkerId(
		[FromServices] ISnowflakeWorkUtensil work)
		=> work.GetWorkerId();
}
