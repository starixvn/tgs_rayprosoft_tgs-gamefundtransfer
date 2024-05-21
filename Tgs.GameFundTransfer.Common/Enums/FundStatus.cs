namespace Tgs.GameFundTransfer.Common.Enums;

public enum FundStatus
{
	/// <summary>
	/// The processing
	/// 轉帳處理中
	/// </summary>
	Processing = 1,

	/// <summary>
	/// The game provider fund failed
	/// 遊戲供應商資金失敗
	/// </summary>
	GameProviderFundFailed = 2,

	/// <summary>
	/// The main wallet failed
	/// 主錢包資金失敗
	/// </summary>
	MainWalletFundFailed = 3,

	/// <summary>
	/// The success
	/// 完成
	/// </summary>
	Success = 4,

	/// <summary>
	/// The main wallet fund refund success
	/// 主金包資金退款成功
	/// </summary>
	MainWalletFundRefundSuccess = 100,

	/// <summary>
	/// The main wallet fund refund failed
	/// 主錢包資金退款失敗
	/// </summary>
	MainWalletFundRefundFailed = 110,
}
