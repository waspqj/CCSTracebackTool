using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CCS_Traceback
{
    public partial class QrStatisticsForm : Form
    {
        private readonly DateTime _start;
        private readonly DateTime _end;
        private readonly List<DetectionRecord> _records;

        public QrStatisticsForm(DateTime start, DateTime end, List<DetectionRecord> records)
        {
            InitializeComponent();
            _start = start;
            _end = end;
            _records = records ?? new List<DetectionRecord>();
            this.Load += QrStatisticsForm_Load;
        }

        private void QrStatisticsForm_Load(object sender, EventArgs e)
        {
            int total = _records.Count;
            int ok = _records.Count(r => r.IsOk);
            int ng = _records.Count(r => r.IsNg);
            this.Text = $"按二维码统计 — 共 {total} 条 (OK {ok} / NG {ng})";

            // 一条记录一行:编号 / 时间 / 二维码 / 状态(NG 时把缺陷类型拼上)
            dgvResults.Rows.Clear();
            foreach (var r in _records)
            {
                string status;
                if (r.IsNg)
                {
                    var defects = r.DefectTypes.Distinct().ToList();
                    string reasonJoined = string.Join(" | ", defects);
                    status = string.IsNullOrEmpty(reasonJoined) ? "NG" : "NG - " + reasonJoined;
                }
                else if (r.IsOk)
                {
                    status = "OK";
                }
                else
                {
                    status = r.ResultRaw ?? "";
                }

                dgvResults.Rows.Add(
                    r.Alias,
                    r.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                    r.QrCode,
                    status);
            }

            if (dgvResults.Rows.Count == 0)
            {
                dgvResults.Rows.Add("—", "—", "(无数据)", "请先在主界面点 \"更新数据\"");
            }
        }
    }
}
