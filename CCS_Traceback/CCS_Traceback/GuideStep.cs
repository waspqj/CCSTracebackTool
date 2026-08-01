namespace CCS_Traceback
{
    /// <summary>
    /// 引导式高亮的 3 步状态机:
    ///   ClickUpdate → ClickQrStatistics → ClickScan
    /// 仅覆盖三个主按钮的高亮时机,无循环无旁路。
    /// </summary>
    public enum GuideStep
    {
        /// <summary>未启动 / 已结束。</summary>
        Idle = 0,

        /// <summary>1. 高亮 btnUpdate("更新数据")。</summary>
        ClickUpdate,

        /// <summary>2. 高亮 btnQrStatistics("按二维码统计")。</summary>
        ClickQrStatistics,

        /// <summary>3. 高亮 btnScanFiles("扫描文件")。</summary>
        ClickScan,
    }
}