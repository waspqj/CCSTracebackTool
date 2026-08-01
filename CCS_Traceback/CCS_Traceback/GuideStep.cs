namespace CCS_Traceback
{
    /// <summary>
    /// 引导式高亮的状态机节点。
    /// 启动后从 PickDate 开始,沿链路推进;
    /// 在 DoubleClickNg 完成后自动循环回 PickDate,周而复始。
    /// </summary>
    public enum GuideStep
    {
        /// <summary>引导未启动。</summary>
        Idle = 0,

        /// <summary>1. 选日期 —— 高亮 dtpDate。</summary>
        PickDate,

        /// <summary>2. 点"更新数据" —— 高亮 btnUpdate。</summary>
        ClickUpdate,

        /// <summary>3. 更新数据的弹窗确定 —— 高亮 MessageBox 的 OK 按钮。</summary>
        ConfirmUpdateMsg,

        /// <summary>4. 点"按二维码统计" —— 高亮 btnQrStatistics。</summary>
        ClickQrStatistics,

        /// <summary>5. 双击统计窗某行 —— 高亮统计窗的 dgv。</summary>
        DoubleClickStatRow,

        /// <summary>6. 选二维码 —— 高亮 cboQr(旁路可能瞬间跳过)。</summary>
        PickQr,

        /// <summary>7. 选编号 —— 高亮 cboAlias。</summary>
        PickAlias,

        /// <summary>8. 点"扫描文件" —— 高亮 btnScanFiles。扫描失败时停留此步。</summary>
        ClickScan,

        /// <summary>9. 双击 NG 行 —— 高亮 dgvNgList。完成后自动循环回 PickDate。</summary>
        DoubleClickNg,
    }
}