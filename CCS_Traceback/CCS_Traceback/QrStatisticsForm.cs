using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CCS_Traceback
{
    /// <summary>"按二维码统计" 子窗体:展示主窗已载入的全部记录(不做时间过滤,不去重)。双击行 → 触发主窗 ScanQr(qr)。</summary>
    public partial class QrStatisticsForm : Form
    {
        private readonly DateTime _start;
        private readonly DateTime _end;
        private readonly List<DetectionRecord> _records;

        /// <summary>双击某行时回调,参数:qrCode。由主窗在打开本窗时注入。</summary>
        public Action<string> ScanQr;

        public QrStatisticsForm(DateTime start, DateTime end, List<DetectionRecord> records)
        {
            InitializeComponent();
            _start = start;
            _end = end;
            _records = records ?? new List<DetectionRecord>();
            this.Load += QrStatisticsForm_Load;
            this.dgvResults.CellDoubleClick += dgvResults_CellDoubleClick;
        }

        /// <summary>_records 的填表顺序和 dgvResults 行索引一一对应,所以直接用 e.RowIndex 索引回原始记录。</summary>
        private void dgvResults_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _records.Count) return;
            ScanQr?.Invoke(_records[e.RowIndex].QrCode);
        }

        // 把全部记录铺到 dgvResults(4 列:编号 / 时间 / 二维码 / 状态)。NG 拼缺陷类型,OK "OK",其他原样保留。
        private void QrStatisticsForm_Load(object sender, EventArgs e)
        {
            int total = _records.Count;
            int ok = _records.Count(r => r.IsOk);
            int ng = _records.Count(r => r.IsNg);
            this.Text = $"按二维码统计 — 共 {total} 条 (OK {ok} / NG {ng})";

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
                else if (r.IsOk) status = "OK";
                else status = r.ResultRaw ?? "";

                dgvResults.Rows.Add(r.Alias, r.Time.ToString("yyyy-MM-dd HH:mm:ss"), r.QrCode, status);
            }

            if (dgvResults.Rows.Count == 0)
                dgvResults.Rows.Add("—", "—", "(无数据)", "请先在主界面点 \"更新数据\"");
        }
    }
}
