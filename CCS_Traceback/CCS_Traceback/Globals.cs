namespace CCS_Traceback
{
    /// <summary>
    /// 全局配置(后续可改成读配置文件 / 设置窗体维护)。
    /// </summary>
    public static class Globals
    {
        /// <summary>
        /// MES 检测日志根目录。目录下(含子目录)所有 *.csv 都会被尝试按本工具的列结构读取。
        /// </summary>
        public static string PathMegLog = @"D:\mes日志";
    }
}
