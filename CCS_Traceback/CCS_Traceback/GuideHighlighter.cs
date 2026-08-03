using System;
using System.Drawing;
using System.Windows.Forms;

namespace CCS_Traceback
{
    /// <summary>
    /// 引导式高亮:目标控件 BackColor 闪绿 2 下(共 ~1 秒)后保持绿色,Clear() 还原原色。
    /// 只服务于 2 个主按钮(btnUpdate / btnQrStatistics)的提示。
    /// </summary>
    public sealed class GuideHighlighter : IDisposable
    {
        private readonly System.Windows.Forms.Timer _flash = new System.Windows.Forms.Timer { Interval = FlashIntervalMs };
        private Control _currentTarget;
        private Color _originalColor;
        private int _flashTick;

        private const int FlashIntervalMs = 150;
        private const int FlashHalfCycles = 4;     // 4 个半周期 = 2 次完整闪烁
        private static readonly Color FlashColor = Color.FromArgb(76, 175, 80);   // Material Green 500

        public GuideHighlighter() { _flash.Tick += Flash_Tick; }

        public void Highlight(Control target)
        {
            Clear();
            if (target == null || target.IsDisposed || !target.IsHandleCreated) return;
            _currentTarget = target;
            _originalColor = target.BackColor;
            _flashTick = 0;
            target.BackColor = FlashColor;   // 第一帧立即设绿(不等 Timer)
            _flash.Start();
        }

        public void Clear()
        {
            _flash.Stop();
            if (_currentTarget != null && !_currentTarget.IsDisposed)
                _currentTarget.BackColor = _originalColor;
            _currentTarget = null;
            _flashTick = 0;
        }

        public void Dispose() { _flash?.Stop(); _flash?.Dispose(); Clear(); }

        // 闪烁节奏:奇数 tick 切回原色,偶数 tick 设绿;达到 FlashHalfCycles 后稳定在绿色。
        private void Flash_Tick(object sender, EventArgs e)
        {
            if (_currentTarget == null || _currentTarget.IsDisposed) { Clear(); return; }
            _flashTick++;
            if (_flashTick >= FlashHalfCycles)
            {
                _currentTarget.BackColor = FlashColor;
                _flash.Stop();
                return;
            }
            _currentTarget.BackColor = (_flashTick % 2 == 0) ? FlashColor : _originalColor;
        }
    }
}
