using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CCS_Traceback
{
    public partial class MainForm : Form
    {
        // 当前已加载的全部记录(内存缓存,供筛选 / 统计复用)
        private List<DetectionRecord> _records = new List<DetectionRecord>();

        // cboQr 中的 "(全部)" 用空字符串表示
        private const string AllSentinel = "";

        // 缺陷类型出现次数缓存(供柱状图绘制)
        private Dictionary<string, int> _defectCounts = new Dictionary<string, int>();

        // 柱状图配色(循环使用)
        private static readonly Color[] ChartColors =
        {
            Color.FromArgb(91, 155, 213),   // 蓝
            Color.FromArgb(237, 125, 49),    // 橙
            Color.FromArgb(165, 105, 189),   // 紫
            Color.FromArgb(72, 201, 176),    // 青
            Color.FromArgb(255, 192, 0),     // 黄
            Color.FromArgb(112, 173, 71),    // 绿
            Color.FromArgb(220, 76, 70),     // 红
            Color.FromArgb(147, 169, 188),   // 灰蓝
        };

        /// <summary>
        /// 硬编码的缺陷类型清单(从现有日志中观察到)。后续可改读配置文件 / 数据库。
        /// </summary>
        private static readonly string[] HardcodedDefectTypes =
        {
            "母pin扩孔NG",
            "焊点漏焊",
            "焊点数量错误",
            "焊点焊穿",
            "焊点偏移",
            "镍片藏底",
            "铝排不合格",
        };

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 默认:日期=今天,时段=全天 00:00 ~ 23:59
            dtpDate.Value = DateTime.Today;
            mtbStartTime.Text = "00:00";
            mtbEndTime.Text = "23:59";

            // 左右比例 1:2(Panel1 占总宽的 1/3,Panel2 占 2/3)
            int total = Screen.PrimaryScreen.Bounds.Width;
            int leftWidth = (int)((total - splitMain.SplitterWidth) / 3.0);
            int maxDistance = splitMain.Width - splitMain.SplitterWidth - 4;
            if (leftWidth > 0 && leftWidth <= maxDistance)
            {
                splitMain.SplitterDistance = leftWidth;
            }

            UpdateStatsText(0, 0, 0, 0, 0);
            ShowEmptyNgList();

            // 启动时柱状图就铺满所有硬编码缺陷类型(均为 0)
            BuildDefectCounts();
        }

        /// <summary>
        /// dgvNgList 启动时显示一行占位,提示要先点 "更新数据"。
        /// </summary>
        private void ShowEmptyNgList()
        {
            dgvNgList.Rows.Clear();
            var row = new DataGridViewRow();
            row.CreateCells(dgvNgList);
            row.Cells[0].Value = "—";
            row.Cells[1].Value = "—";
            row.Cells[2].Value = "(无数据)";
            row.Cells[3].Value = "请先点 \"更新数据\" 加载 CSV";
            row.Cells[4].Value = "";
            row.Cells[5].Value = "";
            row.Cells[6].Value = "";
            dgvNgList.Rows.Add(row);
        }

        /// <summary>
        /// 把当前 _records 里的 NG 行喂给 dgvNgList。
        /// </summary>
        private void RefreshNgList()
        {
            dgvNgList.Rows.Clear();
            if (_records == null || _records.Count == 0)
            {
                ShowEmptyNgList();
                return;
            }

            var ngList = _records.Where(r => r.IsNg).ToList();
            if (ngList.Count == 0)
            {
                var row = new DataGridViewRow();
                row.CreateCells(dgvNgList);
                row.Cells[0].Value = "—";
                row.Cells[1].Value = "—";
                row.Cells[2].Value = "OK";
                row.Cells[3].Value = "当前时段无 NG 记录";
                row.Cells[4].Value = "";
                row.Cells[5].Value = "";
                row.Cells[6].Value = "";
                dgvNgList.Rows.Add(row);
                return;
            }

            foreach (var r in ngList)
            {
                string defects = string.Join(" | ", r.DefectTypes);
                dgvNgList.Rows.Add(
                    r.Time.ToString("HH:mm:ss"),
                    r.QrCode,
                    "NG",
                    r.Alias,
                    defects,
                    r.Seq.ToString(),
                    ""); // 图像路径后续接
            }
        }

        /// <summary>
        /// "更新数据" —— 按日期筛文件 + 按时间段过滤行,载入到内存并刷新下拉。
        /// 二维码去重(一个二维码 = 一个产品);编号全集也去重(等选了 QR 再按 QR 联动);
        /// 缺陷类型不去重,按记录顺序铺开。
        /// </summary>
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!TimeSpan.TryParse(mtbStartTime.Text, out TimeSpan startTs))
            {
                MessageBox.Show("开始时间格式无效,请按 HH:mm 输入", "时段无效",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mtbStartTime.Focus();
                return;
            }
            if (!TimeSpan.TryParse(mtbEndTime.Text, out TimeSpan endTs))
            {
                MessageBox.Show("结束时间格式无效,请按 HH:mm 输入", "时段无效",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mtbEndTime.Focus();
                return;
            }

            DateTime date = dtpDate.Value.Date;
            DateTime startTime = date.Add(startTs);
            DateTime endTime = date.Add(endTs);

            if (startTime >= endTime)
            {
                MessageBox.Show("结束时间必须晚于开始时间(不可跨天)", "时段无效",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var reader = new CsvLogReader();
            int matchedFiles;
            _records = reader.ReadRange(Globals.PathMegLog, startTime, endTime, out matchedFiles);

            // 二维码:去重 + 排序
            var qrs = _records.Select(r => r.QrCode)
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Distinct()
                              .OrderBy(s => s)
                              .ToList();
            // 编号:去重 + 排序(整体去重,联动后会被过滤)
            var aliases = _records.Select(r => r.Alias)
                                  .Where(s => !string.IsNullOrWhiteSpace(s))
                                  .Distinct()
                                  .OrderBy(s => s)
                                  .ToList();

            cboQr.Items.Clear();
            cboQr.Items.Add(AllSentinel); // 第 0 项 = "(全部)"
            cboQr.Items.AddRange(qrs.Cast<object>().ToArray());
            cboQr.SelectedIndex = 0;

            // 编号先按当前 QR(= 全部) 全量铺
            RefreshAliasCombo(qrs, aliases);

            // 缺陷类型:硬编码(去重),不从数据里现读
            cboDefect.Items.Clear();
            cboDefect.Items.AddRange(HardcodedDefectTypes.Cast<object>().ToArray());

            int ngCount = _records.Count(r => r.IsNg);
            UpdateStatsText(matchedFiles, _records.Count, qrs.Count, aliases.Count, ngCount);

            // 同步刷新底部 NG 行列表
            RefreshNgList();

            // 刷新缺陷类型柱状图
            BuildDefectCounts();

            MessageBox.Show(
                $"日期 {date:yyyy-MM-dd} 匹配 {matchedFiles} 个 CSV。\n" +
                $"时段 {startTs:hh\\:mm} ~ {endTs:hh\\:mm} 内载入 {_records.Count} 条记录。",
                "更新数据", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 编号下拉按当前二维码联动:选了具体 QR → 只显示该 QR 对应的编号;
        /// 选 "(全部)" → 显示全部编号。
        /// </summary>
        private void cboQr_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_records == null) return;

            string selectedQr = cboQr.SelectedItem as string;
            bool isAll = string.IsNullOrEmpty(selectedQr) || selectedQr == AllSentinel;

            var aliases = _records.Select(r => r.Alias)
                                  .Where(s => !string.IsNullOrWhiteSpace(s));
            if (!isAll)
            {
                aliases = _records.Where(r => r.QrCode == selectedQr)
                                  .Select(r => r.Alias)
                                  .Where(s => !string.IsNullOrWhiteSpace(s));
            }

            var list = aliases.Distinct().OrderBy(s => s).ToList();
            string prevSelected = cboAlias.SelectedItem as string;

            cboAlias.Items.Clear();
            cboAlias.Items.AddRange(list.Cast<object>().ToArray());

            // 尽量保留上次选中的(联动后还有的话)
            if (!string.IsNullOrEmpty(prevSelected) && cboAlias.Items.Contains(prevSelected))
                cboAlias.SelectedItem = prevSelected;
        }

        // 兼容 btnUpdate 里直接铺全部编号的场景
        private void RefreshAliasCombo(List<string> qrs, List<string> aliases)
        {
            cboAlias.Items.Clear();
            cboAlias.Items.AddRange(aliases.Cast<object>().ToArray());
        }

        private void btnQrStatistics_Click(object sender, EventArgs e)
        {
            if (!TimeSpan.TryParse(mtbStartTime.Text, out TimeSpan startTs))
            {
                MessageBox.Show("开始时间格式无效,请按 HH:mm 输入", "时段无效",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mtbStartTime.Focus();
                return;
            }
            if (!TimeSpan.TryParse(mtbEndTime.Text, out TimeSpan endTs))
            {
                MessageBox.Show("结束时间格式无效,请按 HH:mm 输入", "时段无效",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mtbEndTime.Focus();
                return;
            }

            DateTime date = dtpDate.Value.Date;
            DateTime startTime = date.Add(startTs);
            DateTime endTime = date.Add(endTs);

            if (startTime >= endTime)
            {
                MessageBox.Show("结束时间必须晚于开始时间(不可跨天)", "时段无效",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_records.Count == 0)
            {
                MessageBox.Show("还没有数据,请先点 \"更新数据\" 加载 CSV。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 按二维码统计窗口:展示全部已载入记录(不做时间过滤,不去重)
            var form = new QrStatisticsForm(startTime, endTime, _records);
            form.Show();
        }

        private void UpdateStatsText(int matchedFiles, int total, int qrCount, int aliasCount, int ngCount)
        {
            // 缺陷维度的统计:从 _records 里直接算,不依赖 BuildDefectCounts 的调用顺序
            int defectTotal = 0;
            var defectGroups = (_records ?? new List<DetectionRecord>())
                .Where(r => r.IsNg)
                .SelectMany(r => r.DefectTypes)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .GroupBy(t => t)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            defectTotal = defectGroups.Sum(x => x.Count);
            int distinctTypes = defectGroups.Count;
            string topName = "(无)";
            int topCount = 0;
            if (defectGroups.Count > 0)
            {
                topName = defectGroups[0].Name;
                topCount = defectGroups[0].Count;
            }

            double ngRate = total > 0 ? ngCount * 100.0 / total : 0;
            double topRate = defectTotal > 0 ? topCount * 100.0 / defectTotal : 0;

            lblStatsText.Text =
                $"已载入 {total} 条记录\r\n" +
                $"二维码/产品数 {qrCount} 个\r\n" +
                $"NG {ngCount} 行 ({ngRate:F1}%)\r\n" +
                $"\r\n" +
                $"缺陷总次数 {defectTotal}\r\n" +
                $"已出现种类 {distinctTypes}\r\n" +
                $"最高:{topName} ({topCount} / {topRate:F1}%)";
        }

        private void cboAlias_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cboDefect_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void pnlFilters_Paint(object sender, PaintEventArgs e)
        {

        }

        /// <summary>
        /// 遍历 _records 里的 NG 行,按 DetectionRecord.DefectTypes 累加每种缺陷出现次数。
        /// 硬编码列表里的类型一定出现在字典里(初值 0),这样启动时柱状图也能完整铺位。
        /// 硬编码列表里没出现的类型也一并统计。
        /// 统计完后立即触发柱状图重绘。
        /// </summary>
        private void BuildDefectCounts()
        {
            _defectCounts.Clear();
            // 先把硬编码缺陷类型全部置 0,保证柱状图启动时就有完整的位置占位
            foreach (var t in HardcodedDefectTypes)
                _defectCounts[t] = 0;

            if (_records != null)
            {
                foreach (var r in _records.Where(r => r.IsNg))
                {
                    foreach (var t in r.DefectTypes)
                    {
                        if (string.IsNullOrWhiteSpace(t)) continue;
                        _defectCounts[t] = _defectCounts.TryGetValue(t, out int n) ? n + 1 : 1;
                    }
                }
            }

            pnlChartBox.Invalidate();
        }

        private void pnlChartBox_Resize(object sender, EventArgs e)
        {
            pnlChartBox.Invalidate();
        }

        /// <summary>
        /// 让 pnlChartBox 高度跟随滚动容器,AutoScrollMinSize 宽度按固定画布宽度设置。
        /// 横向溢出时容器自动出现水平滚动条。
        /// </summary>
        private void pnlChartPanel_Resize(object sender, EventArgs e)
        {
            int h = pnlChartPanel.ClientSize.Height;
            if (h <= 0) h = pnlChartBox.Height;
            pnlChartBox.Height = h;
            pnlChartPanel.AutoScrollMinSize = new Size(pnlChartBox.Width, 0);
            pnlChartBox.Invalidate();
        }

        /// <summary>
        /// 自绘柱状图:GDI+,不依赖 Chart 控件。
        /// 布局:左侧只留 6px(无 Y 轴刻度),右侧 22px 给最右柱留空,顶部 22px 给柱顶数字,
        /// 底部 38px 给 X 轴类型名。柱宽占槽宽 78%,柱间距紧凑。
        /// 0 计数画 3px 浅灰占位块。
        /// 当最高柱超过 plotRect 高度的 3/4 时,所有柱子按比例整体缩小,让最高柱停在 3/4 处,
        /// 保证数字始终画在柱外。
        /// </summary>
        private void pnlChartBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = pnlChartBox.ClientRectangle;
            const int marginLeft = 6;
            const int marginRight = 22;
            const int marginTop = 22;
            const int marginBottom = 38;

            var plotRect = new Rectangle(
                rect.X + marginLeft,
                rect.Y + marginTop,
                rect.Width - marginLeft - marginRight,
                rect.Height - marginTop - marginBottom);
            if (plotRect.Width < 30 || plotRect.Height < 30) return;

            var items = _defectCounts.OrderByDescending(kv => kv.Value).ToList();
            if (items.Count == 0) return;
            int maxVal = items.Max(kv => kv.Value);
            if (maxVal == 0) maxVal = 1;

            // 最高柱实际高度占比,如果超过 plotRect 的 3/4 就整体缩小,让最高柱停在 3/4 处
            float maxBarRatio = items.Max(kv => kv.Value) / (float)maxVal;
            float barScale = maxBarRatio > 0.75f ? (0.75f / maxBarRatio) : 1f;

            // 水平网格 + 底部 X 轴(去掉纵坐标轴和刻度数字)
            using (var gridPen = new Pen(Color.FromArgb(230, 230, 230), 1))
            using (var axisPen = new Pen(Color.Gray, 1))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y = plotRect.Bottom - (int)(plotRect.Height * i / 4.0);
                    g.DrawLine(gridPen, plotRect.Left, y, plotRect.Right, y);
                }
                g.DrawLine(axisPen, plotRect.Left, plotRect.Bottom, plotRect.Right, plotRect.Bottom);
            }

            // 画柱子:柱宽占槽宽 78%,上限 80px,柱子更胖,间距更紧凑
            int n = items.Count;
            float slotWidth = plotRect.Width / (float)n;
            float barWidth = Math.Min(slotWidth * 0.78f, 80f);

            using (var barFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold))
            using (var axisFont = new Font("Microsoft YaHei UI", 9F))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                for (int i = 0; i < n; i++)
                {
                    var (name, count) = (items[i].Key, items[i].Value);
                    float xCenter = plotRect.Left + slotWidth * i + slotWidth / 2f;

                    Color color;
                    float drawH;
                    if (count == 0)
                    {
                        color = Color.FromArgb(225, 225, 225);
                        drawH = 3f;
                    }
                    else
                    {
                        color = ChartColors[i % ChartColors.Length];
                        float barH = plotRect.Height * (count / (float)maxVal) * barScale;
                        drawH = Math.Max(barH, 4f);
                    }

                    var barRect = new RectangleF(
                        xCenter - barWidth / 2f,
                        plotRect.Bottom - drawH,
                        barWidth,
                        drawH);

                    using (var brush = new SolidBrush(color))
                        g.FillRectangle(brush, barRect);
                    // 边框用同色调加深,避免黑边突兀
                    using (var pen = new Pen(ControlPaint.Dark(color, 0.25f), 1))
                        g.DrawRectangle(pen, barRect.X, barRect.Y, barRect.Width, barRect.Height);

                    // 柱顶数字:永远画柱外黑字(整体缩放已保证不会超出 plotRect)
                    if (count > 0)
                    {
                        g.DrawString(count.ToString(), barFont, Brushes.Black,
                            xCenter, barRect.Y - 16, sf);
                    }

                    // X 轴标签:贴在 X 轴下方 8px,留够缓冲不和图像区重影
                    string label = name.Length > 8 ? name.Substring(0, 7) + "…" : name;
                    g.DrawString(label, axisFont, Brushes.Black,
                        xCenter, plotRect.Bottom + 8, sf);
                }
            }
        }
    }
}
