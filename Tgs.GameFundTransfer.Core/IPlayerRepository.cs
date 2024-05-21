namespace Tgs.GameFundTransfer.Core;

public interface IPlayerRepository
{
	ValueTask<Models.PlayerInfo> GetAsync(string playerCode, CancellationToken cancellationToken = default);
}
