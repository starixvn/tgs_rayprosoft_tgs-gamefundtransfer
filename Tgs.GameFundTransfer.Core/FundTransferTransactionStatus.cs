namespace Tgs.GameFundTransfer.Core
{
    public enum FundTransferTransactionStatus
    {
        /// <summary>
        /// The wait for response
        /// 建立訂單等待
        /// </summary>
        WaitForResponse = 1,

        /// <summary>
        /// The failure on game provider
        /// 遊戲供應失敗
        /// </summary>
        FailureOnGameProvider,

        /// <summary>
        /// The failure on main wallet
        /// 主錢包失敗
        /// </summary>
        FailureOnMainWallet,

        /// <summary>
        /// The success
        /// 完成
        /// </summary>
        Success,

        /// <summary>
        /// The manuel fund in
        /// 手動補點回主錢包
        /// </summary>
        ManuelFundIn,
    }
}
