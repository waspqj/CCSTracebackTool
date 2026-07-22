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
    /// 列顺序(从 MES 实际表头):
    ///   0  编号
    ///   1  二维码
    ///   2  1结果      ← 决定 NG / OK
    ///   3  1结果      ← 第二路结果(同样字段名,采集两份)
    ///   4  原因       ← 可能含逗号,会被 CSV 引号包裹
    ///   5  时间       ← "yyyy-MM-dd HH.mm.ss"
    ///   6  MES返回(1)
    ///   7  MES返回(1)
    ///
    /// 编码:UTF-8 with BOM(实测)。无 BOM 时按 UTF-8 兜底解析。
    /// 行尾:CRLF,允许 LF。
    /// </summary>
    public class CsvLogReader
    {
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
        /// 由于文件内时间是递增的,遇到第一条 Time &gt; endTime 就 break,不再往后读。
        /// 返回 (匹配的记录, 匹配的文件数)。
        /// </summary>
        public List<DetectionRecord> ReadRange(string rootDir, DateTime startTime, DateTime endTime, out int matchedFiles)
        {
            var all = new List<DetectionRecord>();
            matchedFiles = 0;
            var files = FindFilesForDate(rootDir, startTime.Date);
            matchedFiles = files.Count;
            foreach (var f in files)
            {
                List<DetectionRecord> part;
                try { part = ReadFileInRange(f, startTime, endTime); }
                catch { continue; }
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

            byte[] raw = File.ReadAllBytes(path);
            int offset = 0;
            if (raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF)
                offset = 3;
            string text = Encoding.UTF8.GetString(raw, offset, raw.Length - offset);

            var rows = ParseCsv(text);
            if (rows.Count == 0) return list;

            int seq = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count < 6) continue;

                string ts = SafeGet(row, 5);
                if (string.IsNullOrWhiteSpace(ts)) continue;
                if (!DateTime.TryParseExact(ts.Trim(), TimeFormats,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t))
                {
                    continue;
                }

                if (t < startTime) continue;   // 还没到开始时间
                if (t > endTime) break;        // 已过结束时间,后续也不再进来(递增)

                seq++;
                var rec = new DetectionRecord
                {
                    Seq = seq,
                    Alias = SafeGet(row, 0),
                    QrCode = SafeGet(row, 1),
                    ResultRaw = SafeGet(row, 2),
                    Result2Raw = SafeGet(row, 3),
                    Reason = SafeGet(row, 4),
                    Time = t,
                    MesReturn1 = SafeGet(row, 6),
                    MesReturn2 = SafeGet(row, 7),
                };
                list.Add(rec);
            }

            return list;
        }

        /// <summary>
        /// 读根目录下(含子目录)的所有 *.csv,返回全部记录。
        /// 单个文件解析失败时跳过并继续,不会让整个调用崩。
        /// </summary>
        public List<DetectionRecord> ReadAll(string rootDir)
        {
            var all = new List<DetectionRecord>();
            if (string.IsNullOrWhiteSpace(rootDir) || !Directory.Exists(rootDir))
                return all;

            string[] files;
            try
            {
                files = Directory.GetFiles(rootDir, "*.csv", SearchOption.AllDirectories);
            }
            catch
            {
                return all;
            }

            foreach (var file in files)
            {
                List<DetectionRecord> part;
                try
                {
                    part = ReadFile(file);
                }
                catch
                {
                    // 单文件失败不影响其它文件
                    continue;
                }
                all.AddRange(part);
            }

            return all;
        }

        /// <summary>
        /// 读单个 CSV。文件没有表头或表头列不足 6 时整体跳过。
        /// </summary>
        public List<DetectionRecord> ReadFile(string path)
        {
            var list = new List<DetectionRecord>();

            byte[] raw = File.ReadAllBytes(path);
            int offset = 0;
            if (raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF)
                offset = 3;
            string text = Encoding.UTF8.GetString(raw, offset, raw.Length - offset);

            var rows = ParseCsv(text);
            if (rows.Count == 0) return list;

            // 第 0 行视作表头 —— 不强校验列名,直接跳过
            int seq = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count < 6) continue;

                seq++;
                var rec = new DetectionRecord
                {
                    Seq = seq,
                    Alias = SafeGet(row, 0),
                    QrCode = SafeGet(row, 1),
                    ResultRaw = SafeGet(row, 2),
                    Result2Raw = SafeGet(row, 3),
                    Reason = SafeGet(row, 4),
                    MesReturn1 = SafeGet(row, 6),
                    MesReturn2 = SafeGet(row, 7),
                };

                string ts = SafeGet(row, 5);
                if (!string.IsNullOrWhiteSpace(ts) &&
                    DateTime.TryParseExact(ts.Trim(), TimeFormats,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t))
                {
                    rec.Time = t;
                }

                list.Add(rec);
            }

            return list;
        }

        private static string SafeGet(List<string> row, int idx)
        {
            return idx < row.Count ? (row[idx] ?? string.Empty).Trim() : string.Empty;
        }

        /// <summary>
        /// 解析 CSV 文本为行 → 字段。支持:
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
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // 转义 ""
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
                }
                else
                {
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
                        // 跳过,等 \n 收尾(也容忍单独 \r)
                        if (i + 1 < text.Length && text[i + 1] == '\n')
                            continue;
                        curRow.Add(curField.ToString());
                        curField.Length = 0;
                        rows.Add(curRow);
                        curRow = new List<string>();
                    }
                    else if (c == '\n')
                    {
                        curRow.Add(curField.ToString());
                        curField.Length = 0;
                        rows.Add(curRow);
                        curRow = new List<string>();
                    }
                    else
                    {
                        curField.Append(c);
                    }
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
    }
}
