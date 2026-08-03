using System;
using System.Collections.Generic;

namespace CCS_Traceback
{
    /// <summary>
    /// 一行 CSV 检测记录(也用于 FileScanner 扫描出的 NG 文件行)的内存表示。
    /// 状态判定:ResultRaw 第一个字符是 N → NG,Y → OK,其他原样保留。
    /// </summary>
    public class DetectionRecord
    {
        public string Alias { get; set; }
        public string QrCode { get; set; }
        public string ResultRaw { get; set; }
        public string Reason { get; set; }
        public DateTime Time { get; set; }
        public string ProcessName { get; set; }
        public string FileReason { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }

        public string StatusRaw => (ResultRaw ?? string.Empty).Trim().ToUpperInvariant();
        public bool IsNg => StatusRaw == "N";
        public bool IsOk => StatusRaw == "Y";

        /// <summary>从 Reason 中按 , / ， / ; / ； 切出所有缺陷类型(去空白、去空)。</summary>
        public IEnumerable<string> DefectTypes
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Reason)) yield break;
                foreach (var part in Reason.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = part.Trim();
                    if (!string.IsNullOrEmpty(t)) yield return t;
                }
            }
        }
    }
}
