using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CCS_Traceback
{
    /// <summary>
    /// 引导式高亮管理器:
    ///   - 把目标控件的 BackColor 改成绿色,闪两下(共 ~1 秒),然后保持绿色
    ///   - 直到该步骤推进(用户完成对应动作)→ Clear() 还原原色 + 高亮新目标
    ///   - 用 P/Invoke 兼顾 MessageBox OK 按钮(只试一次 BM_SETBKCOLOR,失败静默)
    ///
    /// 注意:本类不再使用 overlay Panel,直接改控件自身 BackColor,
    /// 这样高亮跟控件共存,不会遮挡文字 / 图标。
    /// </summary>
    public sealed class GuideHighlighter : IDisposable
    {
        private readonly Form _ownerForm;
        private readonly System.Windows.Forms.Timer _flash;

        private Control _currentTarget;
        private Color _originalColor;
        private GuideStep _currentStep = GuideStep.Idle;

        // 闪烁参数
        private const int FlashIntervalMs = 150;       // 单次闪烁间隔
        private const int FlashHalfCycles = 4;          // 总切换次数 = 闪烁 2 下 (绿→原→绿→原→绿,共 4 切)
        private static readonly Color FlashColor = Color.FromArgb(76, 175, 80);   // 柔和绿 Material Green 500

        private int _flashTick;

        // ------------------- MessageBox 高亮 P/Invoke (仅尝试,失败静默) -------------------
        private const int BM_SETBKCOLOR = 0x00E1;
        private const int IDOK = 1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetDlgCtrlID(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        public GuideHighlighter(Form owner)
        {
            _ownerForm = owner ?? throw new ArgumentNullException(nameof(owner));

            _flash = new System.Windows.Forms.Timer { Interval = FlashIntervalMs };
            _flash.Tick += Flash_Tick;
        }

        /// <summary>当前所在步骤。</summary>
        public GuideStep CurrentStep => _currentStep;

        /// <summary>
        /// 启动引导,默认从 PickDate 开始。
        /// </summary>
        public void Start()
        {
            GoTo(GuideStep.PickDate);
        }

        /// <summary>把引导切到指定步骤。先 Clear 旧的,再 Highlight 新的。</summary>
        public void GoTo(GuideStep step)
        {
            Clear();
            _currentStep = step;
            Control target = null;
            switch (step)
            {
                case GuideStep.PickDate:
                    target = FindByName("dtpDate");
                    break;
                case GuideStep.ClickUpdate:
                    target = FindByName("btnUpdate");
                    break;
                case GuideStep.ClickQrStatistics:
                    target = FindByName("btnQrStatistics");
                    break;
                case GuideStep.PickQr:
                    target = FindByName("cboQr");
                    break;
                case GuideStep.PickAlias:
                    target = FindByName("cboAlias");
                    break;
                case GuideStep.ClickScan:
                    target = FindByName("btnScanFiles");
                    break;
                case GuideStep.DoubleClickNg:
                    target = FindByName("dgvNgList");
                    break;
                // ConfirmUpdateMsg / DoubleClickStatRow / Idle 由调用方单独处理,无控件目标
            }

            if (target != null)
                Highlight(target);

            StepChanged?.Invoke(step, target);
        }

        /// <summary>
        /// 高亮指定控件:记录原色,开始闪烁动画。
        /// 动画结束(2 次闪烁)后保持绿色,直到 Clear 被调用。
        /// </summary>
        public void Highlight(Control target)
        {
            if (target == null || target.IsDisposed || !target.IsHandleCreated)
                return;

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
            {
                _currentTarget.BackColor = _originalColor;
            }
            _currentTarget = null;
            _flashTick = 0;
        }

        /// <summary>
        /// 尝试高亮 MessageBox 的 OK 按钮(系统对话框,只能 P/Invoke 试一下,失败静默)。
        /// 调用时机:MessageBox.Show 之前。
        /// </summary>
        public void HighlightMessageBoxOk(string windowTitle)
        {
            // 后台线程找 OK 按钮,避免阻塞 UI;失败静默。
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    IntPtr okHwnd = FindOkButton(windowTitle);
                    if (okHwnd == IntPtr.Zero) return;
                    System.Threading.Thread.Sleep(80);
                    IntPtr colorRef = (IntPtr)ColorTranslator.ToWin32(FlashColor);
                    SendMessage(okHwnd, BM_SETBKCOLOR, IntPtr.Zero, colorRef);
                }
                catch { /* P/Invoke 失败不打扰用户 */ }
            });
        }

        public void Dispose()
        {
            _flash?.Stop();
            _flash?.Dispose();
            Clear();
        }

        // ------------------- 内部 -------------------

        /// <summary>事件:步骤或目标变化时通知 MainForm。</summary>
        public event Action<GuideStep, Control> StepChanged;

        private Control FindByName(string name)
        {
            var arr = _ownerForm.Controls.Find(name, true);
            return arr.Length > 0 ? arr[0] : null;
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

        private IntPtr FindOkButton(string title)
        {
            IntPtr msgHwnd = FindWindow("#32770", title);
            if (msgHwnd == IntPtr.Zero) return IntPtr.Zero;

            IntPtr found = IntPtr.Zero;
            EnumChildWindows(msgHwnd, (hWnd, lParam) =>
            {
                var cls = new System.Text.StringBuilder(256);
                GetClassName(hWnd, cls, cls.Capacity);
                if (cls.ToString() == "Button")
                {
                    int id = GetDlgCtrlID(hWnd);
                    if (id == IDOK)
                    {
                        found = hWnd;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }
    }
}