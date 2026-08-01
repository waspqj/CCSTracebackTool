using System;
using System.Drawing;
using System.Windows.Forms;

namespace CCS_Traceback
{
    /// <summary>
    /// 引导式高亮:目标控件的 BackColor 闪绿 2 下(共 ~1 秒),然后保持绿色,
    /// 直到 Clear() 被调用(切到下一目标时由调用方 Clear 还原原色)。
    ///
    /// 只服务于 3 个主按钮(btnUpdate / btnQrStatistics / btnScanFiles)的提示,
    /// 不弹不闪其它控件。
    /// </summary>
    public sealed class GuideHighlighter : IDisposable
    {
        private readonly System.Windows.Forms.Timer _flash;
        private Control _currentTarget;
        private Color _originalColor;
        private int _flashTick;

        // 闪烁参数
        private const int FlashIntervalMs = 150;
        private const int FlashHalfCycles = 4;
        private static readonly Color FlashColor = Color.FromArgb(76, 175, 80);   // Material Green 500

        public GuideHighlighter()
        {
            _flash = new System.Windows.Forms.Timer { Interval = FlashIntervalMs };
            _flash.Tick += Flash_Tick;
        }

        /// <summary>
        /// 高亮指定控件:记录原色,闪绿 2 下后保持绿色。
        /// </summary>
        public void Highlight(Control target)
        {
            Clear();
            if (target == null || target.IsDisposed || !target.IsHandleCreated) return;
            _currentTarget = target;
            _originalColor = target.BackColor;
            _flashTick = 0;
            // 第一帧立即设绿(不等 Timer)
            target.BackColor = FlashColor;
            _flash.Start();
        }

        /// <summary>清除当前高亮:还原 BackColor 到原色,停 Timer。</summary>
        public void Clear()
        {
            _flash.Stop();
            if (_currentTarget != null && !_currentTarget.IsDisposed)
                _currentTarget.BackColor = _originalColor;
            _currentTarget = null;
            _flashTick = 0;
        }

        public void Dispose()
        {
            _flash?.Stop();
            _flash?.Dispose();
            Clear();
        }

        private void Flash_Tick(object sender, EventArgs e)
        {
            if (_currentTarget == null || _currentTarget.IsDisposed)
            {
                Clear();
                return;
            }

            _flashTick++;
            if (_flashTick >= FlashHalfCycles)
            {
                // 闪烁结束,稳定在绿色
                _currentTarget.BackColor = FlashColor;
                _flash.Stop();
                return;
            }

            // 切换:奇数 → 原色,偶数 → 绿色(已经第 1 帧设绿)
            _currentTarget.BackColor = (_flashTick % 2 == 0) ? FlashColor : _originalColor;
        }
    }
}