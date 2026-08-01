using System;
using System.Windows.Forms;

namespace CCS_Traceback
{
    /// <summary>
    /// 应用程序入口:启用视觉样式 + 启动 MainForm。
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}