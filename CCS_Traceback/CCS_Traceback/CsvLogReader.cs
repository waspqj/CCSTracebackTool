using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CCS_Traceback
{
    /// <summary>
    /// 读取 MES 目录下 CSV 检测日志。
    ///
    /// 列顺序(从 MES 实际表头,本工具只用到前 6 列):
    ///   0  编号
    ///   1  二维码
    ///   2  1结果      ← 决定 NG / OK
    ///   3  1结果      ← 第二路结果(同样字段名,采集两份,本工具不读)
    ///   4  原因       ← 可能含逗号,会被 CSV 引号包裹
    ///   5  时间       ← "yyyy-MM-dd HH.mm.ss" 或 ":"分隔
    ///
    /// 编码:UTF-8 with BOM(实测);无 BOM 时按 UTF-8 兜底。
    /// 行尾:CRLF / LF 都允许。
    /// </summary>
    public class CsvLogReader
    {
        // 时间戳格式:横线 / 斜线、点 / 冒号都允许
        private static readonly string[] TimeFormats =
        {
            "yyyy-MM-dd HH.mm.ss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/MM/dd HH.mm.ss",
            "yyyy/MM/dd HH:mm:ss",
        };

        /// <summary>
        /// 找根目录下(含子目录)文件名含指定日期串(YYYY-MM-DD)的 CSV。
        /// </summary>
        public List<string> FindFilesForDate(string rootDir, DateTime date)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(rootDir) || !Directory.Exists(rootDir))
                return result;
            string dateStr = date.ToString("yyyy-MM-dd");
            string[] files;
            try
            {
                files = Directory.GetFiles(rootDir, "*.csv", SearchOption.AllDirectories);
            }
            catch
            {
                return result;
            }
            foreach (var f in files)
            {
                if (Path.GetFileName(f).Contains(dateStr))
                    result.Add(f);
            }
            return result;
        }

        /// <summary>
        /// 按日期 + 时段读:先按文件名筛出目标日期的文件,再按时间区间过滤行。
        /// 文件内时间递增 → 遇到第一条 Time &gt; endTime 就 break,不再往后读。
        /// </summary>
        public List<DetectionRecord> ReadRange(string rootDir, DateTime startTime, DateTime endTime, out int matchedFiles)
        {
            var all = new List<DetectionRecord>();
            var files = FindFilesForDate(rootDir, startTime.Date);
            matchedFiles = files.Count;
            foreach (var f in files)
            {
                List<DetectionRecord> part;
                try { part = ReadFileInRange(f, startTime, endTime); }
                catch { continue; }   // 单文件失败不影响其它文件
                all.AddRange(part);
            }
            return all;
        }

        /// <summary>
        /// 读单个 CSV,只返回 [startTime, endTime] 区间内的记录。
        /// 文件内时间递增 → 第一个 Time &gt; endTime 出现就停止解析。
        /// </summary>
        public List<DetectionRecord> ReadFileInRange(string path, DateTime startTime, DateTime endTime)
        {
            var list = new List<DetectionRecord>();
            var rows = ParseCsv(ReadAllText(path));
            if (rows.Count == 0) return list;

            // 第 0 行视作表头 —— 不强校验列名,直接跳过
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count < 6) continue;

                // 时间戳解析失败 → 跳过该行(整行残缺)
                string ts = SafeGet(row, 5);
                if (string.IsNullOrWhiteSpace(ts)) continue;
                if (!DateTime.TryParseExact(ts.Trim(), TimeFormats,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t))
                {
                    continue;
                }

                // 时间窗过滤(已排序)
                if (t < startTime) continue;
                if (t > endTime) break;

                list.Add(new DetectionRecord
                {
                    Alias = SafeGet(row, 0),
                    QrCode = SafeGet(row, 1),
                    ResultRaw = SafeGet(row, 2),
                    Reason = SafeGet(row, 4),
                    Time = t,
                });
            }

            return list;
        }

        // 读整个文件 + 剥 UTF-8 BOM。
        private static string ReadAllText(string path)
        {
            byte[] raw = File.ReadAllBytes(path);
            int offset = 0;
            if (raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF)
                offset = 3;
            return Encoding.UTF8.GetString(raw, offset, raw.Length - offset);
        }

        // 越界 / null 兜底,Trim 去两端空白。
        private static string SafeGet(List<string> row, int idx)
        {
            return idx < row.Count ? (row[idx] ?? string.Empty).Trim() : string.Empty;
        }

        /// <summary>
        /// 解析 CSV 文本为行 → 字段(标准 RFC 4180 子集):
        ///   - 双引号包裹字段(字段内允许 , 和 换行)
        ///   - 双引号转义 ""
        ///   - CRLF / LF 行尾
        /// </summary>
        internal static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var curRow = new List<string>();
            var curField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // 在引号内:除转义 "" 之外所有字符原样塞进字段
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // 转义 "" → 一个 "
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            curField.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        curField.Append(c);
                    }
                    continue;
                }

                // 在引号外:处理 4 个特殊字符
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    curRow.Add(curField.ToString());
                    curField.Length = 0;
                }
                else if (c == '\r')
                {
                    // 跳过 \r,等 \n 收尾(也容忍单独 \r —— 老 Mac 文件)
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                        continue;
                    EndRow(ref curRow, ref curField, rows);
                }
                else if (c == '\n')
                {
                    EndRow(ref curRow, ref curField, rows);
                }
                else
                {
                    curField.Append(c);
                }
            }

            // 末尾无换行的情况
            if (curField.Length > 0 || curRow.Count > 0)
            {
                curRow.Add(curField.ToString());
                rows.Add(curRow);
            }

            return rows;
        }

        // 把当前累积的字段 / 行塞进 rows,重置 curRow / curField。
        private static void EndRow(ref List<string> curRow, ref StringBuilder curField, List<List<string>> rows)
        {
            curRow.Add(curField.ToString());
            curField.Length = 0;
            rows.Add(curRow);
            curRow = new List<string>();
        }
    }
}