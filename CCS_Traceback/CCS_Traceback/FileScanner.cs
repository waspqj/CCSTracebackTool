using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CCS_Traceback
{
    /// <summary>
    /// 给定一个产品(QR + Alias + 日期),扫 NGImgSave 下的实际存在的 NG 错图:
    ///
    ///   {NGImgSave}
    ///     └─ {yyyy-MM}                ← 当月目录
    ///         └─ {yyyy-MM-dd}         ← 当天目录
    ///             └─ {QR 或 "0"}      ← 产品二维码目录
    ///                 └─ {processName} ← 流程名目录
    ///                     └─ *.bmp / *.jpg
    ///
    /// 4 种 NG 来源目录层一致,文件名模板不一致,但都按"最后一个 _ 切原因段"思路解析。
    /// QR 在 NGImgSave 里不会被清洗,目录名可能含 \ / : * ? " < > | 等 → 用 DirectoryInfo 拿目录名,
    /// 比较时容错忽略这些字符。
    /// </summary>
    public static class FileScanner
    {
        private static readonly string[] ImageExts = { ".bmp", ".jpg", ".jpeg", ".png" };

        /// <summary>
        /// 把 QR 字符串清洗成"可作文件名 / 可作目录名"的形态。
        /// 这里不再用它去对 NGImgSave 路径 —— NGImgSave 现场不洗 QR,目录名容错读取。
        /// SafeQr 仍保留以备其它路径(若以后再扫其它目录)使用。
        /// </summary>
        public static string SafeQr(string qr)
        {
            if (string.IsNullOrWhiteSpace(qr)) return "_";
            var invalid = Path.GetInvalidFileNameChars();
            var chars = qr.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0
                    || chars[i] == ' '
                    || chars[i] == ':')
                {
                    chars[i] = '_';
                }
            }
            return new string(chars);
        }

        /// <summary>
        /// 扫指定产品当天产生的所有 NG 错图文件。
        /// </summary>
        /// <param name="qr">二维码原文</param>
        /// <param name="alias">编号</param>
        /// <param name="date">当天日期</param>
        /// <param name="ngImgSaveRoot">NG 错图根目录</param>
        public static List<DetectionRecord> Scan(
            string qr, string alias, DateTime date, string ngImgSaveRoot)
        {
            var result = new List<DetectionRecord>();
            if (string.IsNullOrWhiteSpace(ngImgSaveRoot)) return result;
            if (string.IsNullOrWhiteSpace(qr)) return result;

            string monthStr = date.ToString("yyyy-MM");
            string dateStr = date.ToString("yyyy-MM-dd");

            // NGImgSave/{yyyy-MM}/{yyyy-MM-dd}/  存在才往下走
            string dayDir = Path.Combine(ngImgSaveRoot, monthStr, dateStr);
            if (!Directory.Exists(dayDir)) return result;

            // 找当天目录下匹配 QR 的子目录(容错:目录名可能含非法字符或带前后缀)
            string qrDir = FindQrDir(dayDir, qr);
            if (qrDir == null) return result;

            // 每个 processName 子目录里递归一层文件
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

            // 按时间排序(无时间戳的排在最后)
            result = result
                .OrderBy(r => r.Time == default ? DateTime.MaxValue : r.Time)
                .ToList();

            return result;
        }

        /// <summary>
        /// 在 dayDir 下找一个与 qr 等价的子目录名 —— NGImgSave 现场不洗 QR,
        /// 可能直接是 qr,也可能在 qr 旁带前后缀,也可能用 "0" 表示空 QR。
        /// 匹配策略:目录名经过去前后空格 + 去尾部单边括号后 == qr(忽略空 QR 与 "0" 的等价)。
        /// </summary>
        private static string FindQrDir(string dayDir, string qr)
        {
            string normQr = NormalizeDirName(qr);
            string emptySentinel = "0";
            // 空 QR / 用户没填 → 找 "0" 这个目录
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
                    if (string.IsNullOrEmpty(name)) return d;
                }
            }

            return null;
        }

        /// <summary>
        /// 目录名容错规范化:去前后空白、去末尾成对的括号注记、统一大小写。
        /// 不剔除中间字符 —— QR 自身可能含奇怪字符,我们要的是"原样相等"。
        /// </summary>
        private static string NormalizeDirName(string s)
        {
            if (s == null) return "";
            return s.Trim().TrimEnd('(', ')', '（', '）', '[', ']', '【', '】').Trim();
        }

        /// <summary>
        /// 由基本 DetectionRecord 实例派生文件行所需的展示字段(时间戳从文件名拿)。
        /// </summary>
        private static DetectionRecord NewFileRecord(string qr, string alias, DateTime date, string fullPath)
        {
            string fname = Path.GetFileName(fullPath);
            return new DetectionRecord
            {
                QrCode = qr,
                Alias = alias,
                ResultRaw = "N",
                Time = ParseTimeFromFileName(fname) ?? date,  // 拿不到时分秒就退回日期 00:00
                FileName = fname,
                FilePath = fullPath,
            };
        }

        // 文件名解析:4 类模板统一按"最后一个 _ 切 → 前段是原因"。
        // ① 类 Camera9_{i}_{HHmmss}.jpg 文件名里没有原因段 → 用 processName 兜底。
        //
        // 时间戳段允许的形式(末尾连续数字段):
        //   - HHmmss_ffff  → 1452 → 用 LastIndexOf('_') 切后段,后段是 6+4=10 数字
        //   - HHmmss       → 6 位数字
        // 解析失败(切完不是数字)就把整段当原因,Time 退回当天。
        //
        // ② / ③ / ④ 类的原因段可能残留下英文逗号 "," 或中文逗号 "," → TrimEnd 两次。
        private static string ParseNgReason(string fnameNoExt, string processName)
        {
            if (string.IsNullOrEmpty(fnameNoExt)) return processName;

            // ① 类:Camera9_{i}_{HHmmss}.jpg — 文件名以 Camera9_ 开头,第二段是相机序号,
            // 没有原因段。直接用 processName 兜底。
            if (fnameNoExt.StartsWith("Camera9_", StringComparison.OrdinalIgnoreCase))
            {
                return processName;
            }

            int lastUnderscore = fnameNoExt.LastIndexOf('_');
            if (lastUnderscore <= 0)
            {
                // 没有任何 "_" —— 整段当原因
                return CleanReason(fnameNoExt);
            }

            string timePart = fnameNoExt.Substring(lastUnderscore + 1);
            // 校验:切完后段是 HHmmss 6 位纯数字 → 前面那段就是原因
            if (timePart.Length == 6 && timePart.All(char.IsDigit))
            {
                return CleanReason(fnameNoExt.Substring(0, lastUnderscore));
            }

            // 切完后段不是 6 位数字 → 怀疑是 HHmmss_ffff 这种"含 _ 的时间戳段"
            // 整段文件名已经按 LastIndexOf 切过一次,这里再往前找一个 _ 试切
            int prevUnderscore = fnameNoExt.LastIndexOf('_', lastUnderscore - 1);
            if (prevUnderscore > 0)
            {
                // 再切一次后:长度 = 6 + 1 + 4 = 11,且中间一位是 _
                if (lastUnderscore - prevUnderscore == 7
                    && fnameNoExt[prevUnderscore + 6] == '_')
                {
                    string hms = fnameNoExt.Substring(prevUnderscore + 1, 6);
                    string fff = fnameNoExt.Substring(prevUnderscore + 7, 4);
                    if (hms.All(char.IsDigit) && fff.All(char.IsDigit))
                    {
                        return CleanReason(fnameNoExt.Substring(0, prevUnderscore));
                    }
                }
            }

            // 都不匹配 → 整段当原因兜底
            return CleanReason(fnameNoExt);
        }

        /// <summary>
        /// 去掉原因段尾部残留的英文 / 中文逗号(主程序 TrimEnd(',') 只去英文逗号)。
        /// </summary>
        private static string CleanReason(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.TrimEnd(',', '，', '_', '-').Trim();
        }

        // 文件名里抽时间戳:
        //   - 末尾 6 位纯数字 → HHmmss
        //   - 末尾形如 HHmmss_ffff(共 11 位)→ 取这 6+4
        // 找不到就 null(让 NewFileRecord 退回当天日期)。
        private static DateTime? ParseTimeFromFileName(string fname)
        {
            if (string.IsNullOrEmpty(fname)) return null;

            int dot = fname.LastIndexOf('.');
            string body = dot > 0 ? fname.Substring(0, dot) : fname;
            if (string.IsNullOrEmpty(body)) return null;

            // 优先匹配 HHmmss_ffff:整段至少 11 位,倒数第 7 位必须是 _
            if (body.Length >= 11 && body[body.Length - 7] == '_')
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