using System;
using System.Collections.Generic;

namespace CCS_Traceback
{
    /// <summary>
    /// 一行 CSV 检测记录(也用于 FileScanner 扫描出的 NG 文件行)的内存表示。
    ///
    /// CSV 行:Alias / QrCode / ResultRaw / Reason / Time 由 CsvLogReader 填;
    /// NG 文件行:QrCode / Alias / Time / FileName / FilePath / FileReason / ProcessName 由 FileScanner 填。
    ///
    /// 状态判定:ResultRaw 第一个字符是 N → NG,Y → OK,其他原样保留。
    /// </summary>
    public class DetectionRecord
    {
        /// <summary>编号 —— 当天内唯一,用来区分同一产品的多次检测。</summary>
        public string Alias { get; set; }

        /// <summary>二维码内容。</summary>
        public string QrCode { get; set; }

        /// <summary>第 1 个结果的原始值(原始大小写都可能出现)。</summary>
        public string ResultRaw { get; set; }

        /// <summary>原因 / 缺陷描述(可能为空,可能含 , 等)。</summary>
        public string Reason { get; set; }

        /// <summary>检测时间。</summary>
        public DateTime Time { get; set; }

        /// <summary>规范化后的大写结果串(N / Y / 其他)。</summary>
        public string StatusRaw => (ResultRaw ?? string.Empty).Trim().ToUpperInvariant();

        public bool IsNg => StatusRaw == "N";
        public bool IsOk => StatusRaw == "Y";

        /// <summary>
        /// 从 Reason 中按分隔符切出来的所有缺陷类型(去空白、去空)。
        /// Reason 字段实际写法是 "缺陷A,缺陷B,缺陷C,"(逗号分隔,可能有中英文/分号/尾部空)。
        /// </summary>
        public IEnumerable<string> DefectTypes
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Reason))
                    yield break;
                foreach (var part in Reason.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = part.Trim();
                    if (!string.IsNullOrEmpty(t))
                        yield return t;
                }
            }
        }

        /// <summary>
        /// 由 FileScanner 从 NGImgSave 路径倒数第二层拿到的 processName,例如 "连接器" / "公pin" / "铝排"。
        /// CSV 行这个字段为 null。
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// 由 FileScanner 从 NG 错图文件名按最后一个 _ 拆得到的错误原因段,
        /// 例如 "连接器错装_螺母漏装"。① 类(Camera9_{i}_{HHmmss}.jpg) 文件名里没有原因,
        /// 这种情形由调用方把 processName 写到 FileReason。
        /// CSV 行这个字段为空。
        /// </summary>
        public string FileReason { get; set; }

        /// <summary>文件绝对路径(扫描后填)。CSV 行这个字段为空。</summary>
        public string FilePath { get; set; }

        /// <summary>仅文件名(不含目录),供表格展示。CSV 行这个字段为空。</summary>
        public string FileName { get; set; }
    }
}