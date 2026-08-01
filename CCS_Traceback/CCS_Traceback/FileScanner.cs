using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CCS_Traceback
{
    /// <summary>
    /// 给定一个产品(QR + Alias + 日期),扫 NGImgSave 下的实际存在的 NG 错图文件,
    /// 返回按时间排序的 DetectionRecord 列表(FilePath / FileReason / ProcessName / Time 都填好)。
    ///
    /// 现场目录有 2 派,按"当月目录是否存在"区分:
    ///   A 派(完整 4 层):{NGImgSave}/{yyyy-MM or yyyy_MM}/{yyyy-MM-dd or yyyy_MM_dd}/{QR or "0"}/{processName}/*.bmp|jpg
    ///   B 派(3 层,无月份层):{NGImgSave}/{yyyy-MM-dd or yyyy_MM_dd}/{QR or "0"}/{processName}/*.bmp|jpg
    ///
    /// 日期目录命名兼容横线和下划线 2 种,共 6 种候选。第一个存在的目录胜出。
    /// </summary>
    public static class FileScanner
    {
        private static readonly string[] ImageExts = { ".bmp", ".jpg", ".jpeg", ".png" };

        /// <summary>
        /// 给定根目录 + 日期,挑出第一个实际存在的当天日期目录。
        /// 6 种候选(4 种带月份层 + 2 种不带月份层),都不在返回 null。
        /// 给主窗"占位行的期望路径"展示用,也给 Scan 用。
        /// </summary>
        public static string TryFindDayDir(string ngImgSaveRoot, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(ngImgSaveRoot)) return null;
            string ymDash  = date.ToString("yyyy-MM");
            string ymUs    = date.ToString("yyyy_MM");
            string ymdDash = date.ToString("yyyy-MM-dd");
            string ymdUs   = date.ToString("yyyy_MM_dd");

            var candidates = new[]
            {
                Path.Combine(ngImgSaveRoot, ymDash,  ymdDash),
                Path.Combine(ngImgSaveRoot, ymDash,  ymdUs),
                Path.Combine(ngImgSaveRoot, ymUs,    ymdDash),
                Path.Combine(ngImgSaveRoot, ymUs,    ymdUs),
                Path.Combine(ngImgSaveRoot, ymdDash),
                Path.Combine(ngImgSaveRoot, ymdUs),
            };
            foreach (var c in candidates)
            {
                if (Directory.Exists(c)) return c;
            }
            return null;
        }

        /// <summary>
        /// 扫指定产品当天产生的所有 NG 错图文件。
        /// alias 在文件系统层面无法过滤 —— 由调用方用 CSV 时间戳做反向归属。
        /// </summary>
        public static List<DetectionRecord> Scan(
            string qr, string alias, DateTime date, string ngImgSaveRoot)
        {
            var result = new List<DetectionRecord>();
            if (string.IsNullOrWhiteSpace(ngImgSaveRoot)) return result;
            if (string.IsNullOrWhiteSpace(qr)) return result;

            // 1. 找当天日期目录
            string dayDir = TryFindDayDir(ngImgSaveRoot, date);
            if (dayDir == null) return result;

            // 2. 找当天目录下匹配 QR 的子目录(容错:目录名可能含非法字符或带前后缀)
            string qrDir = FindQrDir(dayDir, qr);
            if (qrDir == null) return result;

            // 3. 每个 processName 子目录里递归一层图像文件
            foreach (var procDir in SafeEnumerateDirectories(qrDir))
            {
                string procName = Path.GetFileName(procDir);
                foreach (var file in SafeEnumerateFiles(procDir))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (!ImageExts.Contains(ext)) continue;

                    var rec = NewFileRecord(qr, alias, date, file);
                    rec.ProcessName = procName;
                    rec.FileReason = ParseNgReason(Path.GetFileNameWithoutExtension(file), procName);
                    rec.ResultRaw = "N";   // 全是 NG
                    result.Add(rec);
                }
            }

            // 4. 按时间排序(无时间戳的排到最后)
            return result
                .OrderBy(r => r.Time == default ? DateTime.MaxValue : r.Time)
                .ToList();
        }

        /// <summary>
        /// 在 dayDir 下找一个与 qr 等价的子目录名 —— NGImgSave 现场不洗 QR,
        /// 可能直接是 qr,也可能在 qr 旁带前后缀;空 QR 用 "0" 表示。
        /// 匹配策略:目录名规范化后 == qr(忽略空 QR 与 "0" 的等价)。
        /// </summary>
        private static string FindQrDir(string dayDir, string qr)
        {
            string normQr = NormalizeDirName(qr);
            string emptySentinel = "0";
            bool wantEmpty = string.IsNullOrWhiteSpace(normQr) || normQr == "_";

            foreach (var d in SafeEnumerateDirectories(dayDir))
            {
                string name = NormalizeDirName(Path.GetFileName(d));
                if (string.IsNullOrEmpty(name)) continue;
                if (wantEmpty)
                {
                    if (name == emptySentinel || string.IsNullOrEmpty(name)) return d;
                    continue;
                }
                if (name == normQr) return d;
            }

            // 兜底:空 QR 但没找到 "0" 目录 → 任何目录都尝试匹配(不常用)
            if (wantEmpty)
            {
                foreach (var d in SafeEnumerateDirectories(dayDir))
                {
                    string name = NormalizeDirName(Path.GetFileName(d));
                    if (!string.IsNullOrEmpty(name)) return d;
                }
            }

            return null;
        }

        /// <summary>
        /// 目录名容错规范化:去前后空白、去末尾成对的括号注记。
        /// 不剔除中间字符 —— QR 自身可能含奇怪字符,我们要的是"原样相等"。
        /// </summary>
        private static string NormalizeDirName(string s)
        {
            if (s == null) return "";
            return s.Trim().TrimEnd('(', ')', '（', '）', '[', ']', '【', '】').Trim();
        }

        /// <summary>
        /// 由 DetectionRecord 实例派生文件行所需的展示字段(时间戳从文件名拿)。
        /// 用扫描时的 date 当基准 + 文件名抽出的时分秒 —— Time.Year 与 CSV 行 Time.Year 一致,
        /// 主窗的 10 分钟差比对才能成立。
        /// 解析失败则退回 date 当天 00:00:00(自然被 10 分钟阈值筛掉,不污染列表)。
        /// </summary>
        private static DetectionRecord NewFileRecord(string qr, string alias, DateTime date, string fullPath)
        {
            string fname = Path.GetFileName(fullPath);
            DateTime? parsed = ParseTimeFromFileName(fname);
            TimeSpan tod = parsed.HasValue ? parsed.Value.TimeOfDay : TimeSpan.Zero;
            return new DetectionRecord
            {
                QrCode = qr,
                Alias = alias,
                ResultRaw = "N",
                Time = date.Date + tod,
                FileName = fname,
                FilePath = fullPath,
            };
        }

        /// <summary>
        /// 文件名解析 → 错误原因段。
        /// 统一锚定最后一个 _ ,按其后剩几位判断时间戳格式:
        ///   - 剩 4 位 → HHmmss_ffff,reason 在 lastUnderscore 之前 7 位之前
        ///   - 剩 6 位 → HHmmss(无毫秒段),reason 在 lastUnderscore 之前
        /// 都不匹配 → 整段兜底当 reason,Time 退回当天日期。
        /// </summary>
        private static string ParseNgReason(string fnameNoExt, string processName)
        {
            if (string.IsNullOrEmpty(fnameNoExt)) return processName;

            // ① 类:Camera9_{i}_{HHmmss}.jpg — 文件名以 Camera9_ 开头,第二段是相机序号,
            // 没有原因段。直接用 processName 兜底。
            if (fnameNoExt.StartsWith("Camera9_", StringComparison.OrdinalIgnoreCase))
                return processName;

            // 现场相机写文件名时常加 {序号}_ 前缀(例 10_焊点焊穿,焊点偏移_030527_4143),
            // 这个序号没业务含义,要剥掉再切。侧面相机无该前缀,跳过即可。
            fnameNoExt = StripLeadingSeq(fnameNoExt);
            if (string.IsNullOrEmpty(fnameNoExt)) return processName;

            // 锚定最后一个 _
            int lastUnderscore = fnameNoExt.LastIndexOf('_');
            if (lastUnderscore < 0)
                return CleanReason(fnameNoExt);   // 没有任何 "_" —— 整段当原因

            string tail = fnameNoExt.Substring(lastUnderscore + 1);

            // 情形 1:HHmmss_ffff —— 尾部恰好 4 位数字
            if (tail.Length == 4 && tail.All(char.IsDigit))
            {
                if (lastUnderscore >= 6)
                {
                    string hms = fnameNoExt.Substring(lastUnderscore - 6, 6);
                    if (hms.All(char.IsDigit))
                    {
                        // reason 是 lastUnderscore - 6 之前的整段(连同 HHmmss 一起剥掉)
                        return CleanReason(fnameNoExt.Substring(0, lastUnderscore - 6));
                    }
                }
            }

            // 情形 2:HHmmss —— 尾部恰好 6 位数字
            if (tail.Length == 6 && tail.All(char.IsDigit))
                return CleanReason(fnameNoExt.Substring(0, lastUnderscore));

            // 都对不上 → 整段兜底
            return CleanReason(fnameNoExt);
        }

        // 去掉原因段尾部残留的英文 / 中文逗号、主程序 TrimEnd(',') 只去英文逗号。
        private static string CleanReason(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.TrimEnd(',', '，', '_', '-').Trim();
        }

        /// <summary>
        /// 剥掉文件名前缀 {N}_(相机写文件时加的流水号,无业务含义)。
        /// 举例 "10_焊点焊穿,焊点偏移_030527_4143" → "焊点焊穿,焊点偏移_030527_4143"。
        /// 没匹配到 {数字}_ 就原样返回(侧面相机这种文件名直接走这条)。
        /// </summary>
        private static string StripLeadingSeq(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            int i = 0;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            if (i > 0 && i < s.Length && s[i] == '_')
                return s.Substring(i + 1);
            return s;
        }

        /// <summary>
        /// 文件名里抽时间戳:
        ///   - 末尾形如 HHmmss_ffff:中间下划线在倒数第 5 位(6 位 HHmmss + 1 位 _ + 4 位 ffff)
        ///   - 退回末尾 6 位纯数字 → HHmmss
        /// 找不到就 null(让 NewFileRecord 退回当天日期)。
        /// </summary>
        private static DateTime? ParseTimeFromFileName(string fname)
        {
            if (string.IsNullOrEmpty(fname)) return null;

            int dot = fname.LastIndexOf('.');
            string body = dot > 0 ? fname.Substring(0, dot) : fname;
            if (string.IsNullOrEmpty(body)) return null;

            // 优先匹配 HHmmss_ffff:整段至少 11 位,倒数第 5 位必须是 _
            if (body.Length >= 11 && body[body.Length - 5] == '_')
            {
                string hms = body.Substring(body.Length - 11, 6);
                string fff = body.Substring(body.Length - 4, 4);
                if (hms.All(char.IsDigit) && fff.All(char.IsDigit)
                    && TryParseHms(hms, out var t))
                {
                    return t;
                }
            }

            // 退回 HHmmss 6 位 —— 取末尾 6 位
            if (body.Length >= 6)
            {
                string seg = body.Substring(body.Length - 6, 6);
                if (seg.All(char.IsDigit) && TryParseHms(seg, out var t))
                    return t;
            }
            return null;
        }

        // HHmmss 解析:基本 + 范围校验。
        private static bool TryParseHms(string s, out DateTime t)
        {
            t = default;
            if (s == null || s.Length != 6) return false;
            if (!int.TryParse(s.Substring(0, 2), out int h)) return false;
            if (!int.TryParse(s.Substring(2, 2), out int m)) return false;
            if (!int.TryParse(s.Substring(4, 2), out int sec)) return false;
            if (h > 23 || m > 59 || sec > 59) return false;
            t = new DateTime(1, 1, 1, h, m, sec);
            return true;
        }

        // 目录 / 文件枚举时遇到权限问题就当空,不让整个扫描崩。
        private static IEnumerable<string> SafeEnumerateDirectories(string dir)
        {
            try { return Directory.EnumerateDirectories(dir).ToList(); }
            catch { return new List<string>(); }
        }

        private static IEnumerable<string> SafeEnumerateFiles(string dir)
        {
            try { return Directory.EnumerateFiles(dir).ToList(); }
            catch { return new List<string>(); }
        }
    }
}