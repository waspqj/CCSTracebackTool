using System;
using System.IO;
using CCS_Traceback;

class Probe
{
    static void Main()
    {
        var reader = new CsvLogReader();
        string root = @"D:\mes日志";

        // 新接口:按日期 + 时段读
        var day = new DateTime(2026, 7, 21);
        var s = day.AddHours(0).AddMinutes(0);
        var e = day.AddHours(23).AddMinutes(59);
        int matched;
        var ranged = reader.ReadRange(root, s, e, out matched);
        Console.WriteLine($"[ReadRange] 匹配文件 {matched},时段内记录 {ranged.Count}");

        // 老接口:全量(用作对比)
        var all = reader.ReadAll(root);
        Console.WriteLine($"[ReadAll]   总记录 {all.Count}");
        int ok = 0, ng = 0, unk = 0;
        foreach (var r in all)
        {
            if (r.IsOk) ok++;
            else if (r.IsNg) ng++;
            else unk++;
        }
        Console.WriteLine($"OK={ok}  NG={ng}  未知={unk}");

        // 抽样:打印前 3 条
        for (int i = 0; i < Math.Min(3, all.Count); i++)
        {
            var r = all[i];
            Console.WriteLine($"--- #{i+1} ---");
            Console.WriteLine($"  Alias={r.Alias}  QrCode={r.QrCode}");
            Console.WriteLine($"  ResultRaw={r.ResultRaw}  Result2={r.Result2Raw}");
            Console.WriteLine($"  Reason={r.Reason}");
            Console.WriteLine($"  Time={r.Time:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  MES1={r.MesReturn1}  MES2={r.MesReturn2}");
            Console.WriteLine($"  Status={r.StatusDisplay}");
        }

        // 列出所有唯一 QR
        var qrs = new System.Collections.Generic.HashSet<string>();
        foreach (var r in all) if (!string.IsNullOrWhiteSpace(r.QrCode)) qrs.Add(r.QrCode);
        Console.WriteLine($"唯一 QR 数: {qrs.Count}");

        // 缺陷类型:全局 SelectMany 拆分 + 去重
        var defects = new System.Collections.Generic.HashSet<string>();
        foreach (var r in all) foreach (var d in r.DefectTypes) defects.Add(d);
        Console.WriteLine($"唯一缺陷类型 ({defects.Count}):");
        foreach (var d in defects) Console.WriteLine($"  · {d}");

        // 验证状态机:直接喂一行含引号 + 逗号的字符串,看切分对不对
        var sample = "1,ABC,N,Y,\"a,b,c\",2026-07-21 14.13.23,,\r\n";
        var parsed = CsvLogReader.ParseCsv(sample);
        Console.WriteLine($"手工样本 rows={parsed.Count}, fields[0]={parsed[0].Count}");
        foreach (var f in parsed[0]) Console.WriteLine($"  [{f}]");
    }
}
