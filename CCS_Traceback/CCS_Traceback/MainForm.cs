using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CCS_Traceback
{
    /// <summary>
    /// 主窗体:CCS 检测回溯工具的入口。
    /// 串联 4 个核心动作:选日期 → 加载 CSV(更新数据) → 看按二维码统计 → 扫 NG 错图。
    /// 同时维护引导式高亮(GuideHighlighter)、柱状图自绘、双击 NG 行预览图像。
    /// </summary>
    public partial class MainForm : Form
    {
        private List<DetectionRecord> _records = new List<DetectionRecord>();
        private List<string> _allQrs = new List<string>();                       // _records 里 distinct 的 QR(去重 + 排序),供 txtQrSearch 搜索
        private Dictionary<string, int> _defectCounts = new Dictionary<string, int>();   // 缺陷类型出现次数(柱状图用)
        private string _mesLogRoot;                                              // 启动时从 ApplicationSetting.json 加载,失败为 null

        // 重复点击 btnUpdate 守卫:三元组完全没变 → 不重复读 CSV、不弹窗;同时是"按二维码统计"一对一窗口的失效判定
        private DateTime? _lastLoadedDate;
        private TimeSpan? _lastLoadedStart;
        private TimeSpan? _lastLoadedEnd;

        private QrStatisticsForm _qrStatsForm;          // "按二维码统计" 一对一窗口引用,FormClosed 清回 null
        private object _lastShownRecordsRef;            // 上次开窗时喂给 QrStatisticsForm 的 _records 引用
        private ApplicationSettingDto _appConfig;       // 启动时 MainForm_Load 从 exe 同目录加载,失败则为 null
        private GuideHighlighter _guide;                // 引导式高亮:依次高亮 btnUpdate → btnQrStatistics

        // 柱状图配色(循环使用)
        private static readonly Color[] ChartColors =
        {
            Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(165, 105, 189),
            Color.FromArgb(72, 201, 176), Color.FromArgb(255, 192, 0), Color.FromArgb(112, 173, 71),
            Color.FromArgb(220, 76, 70),  Color.FromArgb(147, 169, 188),
        };

        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>启动初始化:加载配置、定宽、铺底、装引导。</summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // 默认:日期=今天,时段=全天 00:00 ~ 23:59
            dtpDate.Value = DateTime.Today;
            mtbStartTime.Text = "00:00";
            mtbEndTime.Text = "23:59";

            string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationSetting.json");
            try
            {
                _appConfig = ConfigLoader.Load(jsonPath);
                _mesLogRoot = _appConfig.PathMesLog;
                btnUpdate.Enabled = true;
                btnQrStatistics.Enabled = true;
            }
            catch (ConfigLoadException ex)
            {
                _appConfig = null;
                _mesLogRoot = null;
                btnUpdate.Enabled = false;
                btnQrStatistics.Enabled = false;
                MessageBox.Show(
                    $"启动失败:找不到或无法解析 exe 同目录下的 ApplicationSetting.json。\n\n" +
                    $"期望路径:\n  {jsonPath}\n\n" +
                    $"错误详情:\n  {ex.Message}",
                    "加载配置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // 左右比例 1:2(Panel1 占总宽的 1/3,Panel2 占 2/3)
            int total = Screen.PrimaryScreen.Bounds.Width;
            int leftWidth = (int)((total - splitMain.SplitterWidth) / 3.0);
            int maxDistance = splitMain.Width - splitMain.SplitterWidth - 4;
            if (leftWidth > 0 && leftWidth <= maxDistance) splitMain.SplitterDistance = leftWidth;

            // 铺底:统计区文字、占位行、柱状图(均为 0)
            UpdateStatsText(0, 0, 0, 0, 0);
            ShowEmptyNgList();
            BuildDefectCounts();

            // 启动引导:不主动点,等用户先选日期
            _guide = new GuideHighlighter();
            dtpDate.ValueChanged += DtpDate_ValueChanged;
        }

        // 引导第 1 步:选定日期 → 高亮"更新数据"
        private void DtpDate_ValueChanged(object sender, EventArgs e) { _guide?.Highlight(btnUpdate); }

        /// <summary>dgvNgList 显示一行占位,提示要先点 "扫描文件"。同步清空右侧"图像信息区"。</summary>
        private void ShowEmptyNgList(string tip = "请先选二维码+编号再点 \"扫描文件\"")
        {
            dgvNgList.Rows.Clear();
            txtImgQr.Text = ""; txtImgAlias.Text = ""; txtImgDefect.Text = ""; txtImgTime.Text = "";

            var row = new DataGridViewRow();
            row.CreateCells(dgvNgList);
            row.Cells[0].Value = "—"; row.Cells[1].Value = "—"; row.Cells[2].Value = "—";
            row.Cells[3].Value = tip; row.Cells[4].Value = "";
            dgvNgList.Rows.Add(row);
        }

        /// <summary>"更新数据" —— 按日期筛文件 + 按时间段过滤行,载入到内存并刷新下拉与统计。</summary>
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            // 1. 校验时段格式
            if (!TimeSpan.TryParse(mtbStartTime.Text, out TimeSpan startTs))
            { MessageBox.Show("开始时间格式无效,请按 HH:mm 输入", "时段无效", MessageBoxButtons.OK, MessageBoxIcon.Warning); mtbStartTime.Focus(); return; }
            if (!TimeSpan.TryParse(mtbEndTime.Text, out TimeSpan endTs))
            { MessageBox.Show("结束时间格式无效,请按 HH:mm 输入", "时段无效", MessageBoxButtons.OK, MessageBoxIcon.Warning); mtbEndTime.Focus(); return; }

            DateTime date = dtpDate.Value.Date;
            DateTime startTime = date.Add(startTs);
            DateTime endTime = date.Add(endTs);

            if (startTime >= endTime)
            { MessageBox.Show("结束时间必须晚于开始时间(不可跨天)", "时段无效", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            // 2. 三元组守卫:完全没变 → 不重复读、不弹窗
            if (_lastLoadedDate.HasValue && _lastLoadedStart.HasValue && _lastLoadedEnd.HasValue
                && _lastLoadedDate.Value == date
                && _lastLoadedStart.Value == startTs
                && _lastLoadedEnd.Value == endTs)
                return;

            // 3. 读 CSV 区间内的全部记录
            int matchedFiles;
            _records = new CsvLogReader().ReadRange(_mesLogRoot, startTime, endTime, out matchedFiles);

            // 4. 算出 distinct QR 列表(去重 + 排序),缓存到 _allQrs 并立刻刷到 dgvQrMatches
            _allQrs = _records.Select(r => r.QrCode)
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Distinct()
                              .OrderBy(s => s)
                              .ToList();
            RefreshQrMatches((txtQrSearch.Text ?? "").Trim());

            // 5. 刷统计文本 + 柱状图
            int ngCount = _records.Count(r => r.IsNg);
            int distinctAlias = _records.Select(r => r.Alias)
                                        .Where(s => !string.IsNullOrWhiteSpace(s))
                                        .Distinct()
                                        .Count();
            UpdateStatsText(matchedFiles, _records.Count, _allQrs.Count, distinctAlias, ngCount);
            BuildDefectCounts();

            // 6. 弹结果
            MessageBox.Show(
                $"日期 {date:yyyy-MM-dd} 匹配 {matchedFiles} 个 CSV。\n" +
                $"时段 {startTs:hh\\:mm} ~ {endTs:hh\\:mm} 内载入 {_records.Count} 条记录。",
                "更新数据", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 7. 引导推进 + 缓存三元组
            if (_records.Count > 0) _guide.Highlight(btnQrStatistics);
            _lastLoadedDate = date;
            _lastLoadedStart = startTs;
            _lastLoadedEnd = endTs;
        }

        /// <summary>
        /// 基于传入的 qr + 当前 dtpDate,直接读 NGImgSave 下该 QR 当天所有 NG 图,
        /// 按时间顺序平铺到 dgvNgList。给"搜索结果表双击一行"和"统计窗双击一行"共用。
        /// </summary>
        private void ScanNgForCurrentQr(string qr)
        {
            if (_appConfig == null)
            { MessageBox.Show("请先加载配置(ApplicationSetting.json)。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (string.IsNullOrWhiteSpace(qr))
            { MessageBox.Show("二维码不能为空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            DateTime date = dtpDate.Value.Date;
            var allNgFiles = FileScanner.Scan(qr, "", date, _appConfig.NGImgSave);
            dgvNgList.Rows.Clear();
            if (allNgFiles.Count == 0) { ShowEmptyNgList("(该二维码当天没有任何 NG 错图)"); return; }

            _guide?.Clear();
            RenderAllNgFiles(allNgFiles);
        }

        /// <summary>把 NG 文件列表按时间顺序平铺到 dgvNgList(序号 / 流程名_文件头流水号 / 时间 / 缺陷 / 路径)。</summary>
        private void RenderAllNgFiles(List<DetectionRecord> allNgFiles)
        {
            int seq = 0;
            foreach (var f in allNgFiles.OrderBy(f => f.Time))
            {
                // 数字后缀来自 NG 文件名开头的流水号(相机写文件时加的 {N}_ 前缀);没匹配到退回到只显示 ProcessName
                string leadingSeq = ExtractLeadingSeq(f.FileName);
                string displayName = string.IsNullOrEmpty(leadingSeq)
                    ? f.ProcessName ?? ""
                    : $"{f.ProcessName}_{leadingSeq}";

                seq++;
                // NG 文件 → 拼原始 4 位毫秒(RawMillis);CSV 记录或退回 HHmmss 6 位的 → fff 3 位
                string msText = f.RawMillis ?? f.Time.ToString("fff");
                string timeText = f.Time.ToString("HH:mm:ss") + "." + msText;
                dgvNgList.Rows.Add(
                    seq.ToString(),
                    displayName,
                    timeText,
                    f.FileReason ?? "",
                    f.FilePath ?? "");
                dgvNgList.Rows[dgvNgList.Rows.Count - 1].Tag = f;   // 双击时反查 NG 文件
            }
        }

        /// <summary>提取 NG 文件名开头的流水号(对应 FileScanner.StripLeadingSeq 剥掉的那段)。例 "10_焊点焊穿_030527_4143.jpg" → "10"。</summary>
        private static string ExtractLeadingSeq(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            int dot = fileName.LastIndexOf('.');
            string body = dot > 0 ? fileName.Substring(0, dot) : fileName;
            if (string.IsNullOrEmpty(body)) return "";
            int i = 0;
            while (i < body.Length && char.IsDigit(body[i])) i++;
            if (i > 0 && i < body.Length && body[i] == '_') return body.Substring(0, i);
            return "";
        }

        /// <summary>dgvNgList 双击某行:把"文件路径"列对应的文件装到 pnlImageBox 预览。.jpg/.bmp/.png/.jpeg → picImagePreview;.txt → txtImageTextPreview。</summary>
        private void dgvNgList_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvNgList.Rows[e.RowIndex];
            PopulateImageInfo(row);

            string path = row.Cells["colNgPath"].Value as string;
            if (string.IsNullOrWhiteSpace(path)) { ClearImagePreview(); return; }

            if (Directory.Exists(path))
            {
                try { System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\""); }
                catch { }   // 静默:Explorer 启动失败就不动
                return;
            }

            if (!File.Exists(path))
            { MessageBox.Show("文件不存在:\n" + path, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); ClearImagePreview(); return; }

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".jpg" || ext == ".bmp" || ext == ".png" || ext == ".jpeg") ShowImagePreview(path);
            else if (ext == ".txt") ShowTextPreview(path);
            else { MessageBox.Show("不支持的文件类型: " + ext, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); ClearImagePreview(); }
        }

        /// <summary>
        /// 双击 NG 行时把右侧"图像信息区"四个文本框填上对应值(QR/编号走 row.Tag,缺陷类型/时间走 Cells)。
        /// 顺带把 ForeColor 强制设成 Red —— ReadOnly TextBox 在某些系统主题下会被主题样式覆盖。
        /// </summary>
        private void PopulateImageInfo(DataGridViewRow row)
        {
            var rec = row.Tag as DetectionRecord;
            txtImgQr.Text     = rec?.QrCode ?? "";
            txtImgAlias.Text  = rec?.FileName ?? "";
            txtImgDefect.Text = (row.Cells["colNgReason"].Value as string) ?? "";
            txtImgTime.Text   = (row.Cells["colNgTime"].Value   as string) ?? "";

            // 运行时强制红字(应对某些主题把 ReadOnly TextBox 的颜色重置)
            var red = Color.Red;
            txtImgQr.ForeColor = red; txtImgAlias.ForeColor = red; txtImgDefect.ForeColor = red; txtImgTime.ForeColor = red;
        }

        // 把图像文件装到 picImagePreview(Cyotek ImageBox)。Dispose 旧图后装新图 + ZoomToFit。
        private void ShowImagePreview(string path)
        {
            try
            {
                if (picImagePreview.Image != null)
                {
                    picImagePreview.Image.Dispose();
                    picImagePreview.Image = null;
                }
                picImagePreview.Image = Image.FromFile(path);
                picImagePreview.ZoomToFit();
                picImagePreview.Visible = true;
                txtImageTextPreview.Visible = false;
                lblImagePlaceholder.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法加载图像: " + ex.Message,
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ClearImagePreview();
            }
        }

        // 把文本文件读到 txtImageTextPreview。
        private void ShowTextPreview(string path)
        {
            try
            {
                txtImageTextPreview.Text = File.ReadAllText(path, System.Text.Encoding.UTF8);
                txtImageTextPreview.Visible = true;
                picImagePreview.Visible = false;
                lblImagePlaceholder.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法读取文本: " + ex.Message,
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ClearImagePreview();
            }
        }

        // 清空预览区:释放 Image + 回到"显示占位文字"状态。
        private void ClearImagePreview()
        {
            if (picImagePreview.Image != null)
            {
                picImagePreview.Image.Dispose();
                picImagePreview.Image = null;
            }
            picImagePreview.Visible = false;
            txtImageTextPreview.Visible = false;
            lblImagePlaceholder.Visible = true;
        }

        /// <summary>QR 搜索框每次按键 → 按子串(不区分大小写)从 _allQrs 里筛出匹配的,同时算出每个 QR 的出现条数。关键字为空 → 展示全部 distinct QR。</summary>
        private void txtQrSearch_TextChanged(object sender, EventArgs e) { RefreshQrMatches((txtQrSearch.Text ?? "").Trim()); }

        private void RefreshQrMatches(string keyword)
        {
            dgvQrMatches.Rows.Clear();

            IEnumerable<string> qrs = _allQrs;
            if (!string.IsNullOrEmpty(keyword))
                qrs = _allQrs.Where(q => q != null && q.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var qr in qrs)
            {
                if (string.IsNullOrEmpty(qr)) continue;
                int cnt = _records.Count(r => r.QrCode == qr);
                dgvQrMatches.Rows.Add(qr, cnt.ToString());
                dgvQrMatches.Rows[dgvQrMatches.Rows.Count - 1].Tag = qr;   // 双击反查用
            }
        }

        /// <summary>dgvQrMatches 双击某一行 → 直接扫描该 QR 当天所有 NG 图。</summary>
        private void dgvQrMatches_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvQrMatches.Rows[e.RowIndex];
            string qr = row.Tag as string ?? row.Cells["colMatchQr"].Value as string;
            if (string.IsNullOrEmpty(qr)) return;
            ScanNgForCurrentQr(qr);
        }

        /// <summary>"按二维码统计" 一对一窗口:同一份数据 + 同一时段 → 激活已有窗口、不弹新窗;数据更新了 → 关旧开新。</summary>
        private void btnQrStatistics_Click(object sender, EventArgs e)
        {
            // 1. 时段校验(同 btnUpdate)
            if (!TimeSpan.TryParse(mtbStartTime.Text, out TimeSpan startTs))
            { MessageBox.Show("开始时间格式无效,请按 HH:mm 输入", "时段无效", MessageBoxButtons.OK, MessageBoxIcon.Warning); mtbStartTime.Focus(); return; }
            if (!TimeSpan.TryParse(mtbEndTime.Text, out TimeSpan endTs))
            { MessageBox.Show("结束时间格式无效,请按 HH:mm 输入", "时段无效", MessageBoxButtons.OK, MessageBoxIcon.Warning); mtbEndTime.Focus(); return; }

            DateTime date = dtpDate.Value.Date;
            DateTime startTime = date.Add(startTs);
            DateTime endTime = date.Add(endTs);

            if (startTime >= endTime)
            { MessageBox.Show("结束时间必须晚于开始时间(不可跨天)", "时段无效", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (_records.Count == 0)
            { MessageBox.Show("还没有数据,请先点 \"更新数据\" 加载 CSV。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            // 2. 已开着窗口的复用判定:date/start/end 三元组一致 + _records 引用没变
            if (_qrStatsForm != null && !_qrStatsForm.IsDisposed)
            {
                bool sameAsLastLoaded =
                    _lastLoadedDate.HasValue && _lastLoadedStart.HasValue && _lastLoadedEnd.HasValue
                    && _lastLoadedDate.Value == date
                    && _lastLoadedStart.Value == startTs
                    && _lastLoadedEnd.Value == endTs
                    && ReferenceEquals(_lastShownRecordsRef, _records);

                if (sameAsLastLoaded)
                {
                    // 同一份数据 → 激活已有窗口。窗口被最小化时 Activate() 不会还原,必须先还原 WindowState。
                    if (_qrStatsForm.WindowState == FormWindowState.Minimized)
                        _qrStatsForm.WindowState = FormWindowState.Normal;
                    _qrStatsForm.Activate();
                    _qrStatsForm.BringToFront();
                    return;
                }

                // 数据更新了 → 旧窗口关掉,允许下面开新的
                _qrStatsForm.FormClosed -= QrStatsForm_FormClosed;
                _qrStatsForm.Close();
                _qrStatsForm = null;
            }

            // 3. 开新窗:展示全部已载入记录(不做时间过滤,不去重);双击行 → ScanQr 回调把 QR 交给 ScanNgForCurrentQr
            var form = new QrStatisticsForm(startTime, endTime, _records);
            form.ScanQr = qr => ScanNgForCurrentQr(qr);
            form.FormClosed += QrStatsForm_FormClosed;
            _qrStatsForm = form;
            _lastShownRecordsRef = _records;
            form.Show();
        }

        // 子窗体被关掉时清回 _qrStatsForm 引用,这样下次再点按钮可以开新窗。
        private void QrStatsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (ReferenceEquals(sender, _qrStatsForm)) _qrStatsForm = null;
        }

        /// <summary>把"已载入 / NG / 缺陷维度"的统计文本写到右侧 lblStatsText。缺陷维度直接从 _records 现算。</summary>
        private void UpdateStatsText(int matchedFiles, int total, int qrCount, int aliasCount, int ngCount)
        {
            var defectGroups = (_records ?? new List<DetectionRecord>())
                .Where(r => r.IsNg)
                .SelectMany(r => r.DefectTypes)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .GroupBy(t => t)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            int defectTotal = defectGroups.Sum(x => x.Count);
            int distinctTypes = defectGroups.Count;
            string topName = defectGroups.Count > 0 ? defectGroups[0].Name : "(无)";
            int topCount = defectGroups.Count > 0 ? defectGroups[0].Count : 0;
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

        /// <summary>遍历 _records 里的 NG 行,按 DefectTypes 累加每种缺陷出现次数到 _defectCounts。完全数据驱动。</summary>
        private void BuildDefectCounts()
        {
            _defectCounts.Clear();
            if (_records != null)
            {
                foreach (var r in _records.Where(r => r.IsNg))
                    foreach (var t in r.DefectTypes)
                    {
                        if (string.IsNullOrWhiteSpace(t)) continue;
                        _defectCounts[t] = _defectCounts.TryGetValue(t, out int n) ? n + 1 : 1;
                    }
            }
            pnlChartBox.Invalidate();
        }

        // 画布尺寸变了就重绘
        private void pnlChartBox_Resize(object sender, EventArgs e) { pnlChartBox.Invalidate(); }

        /// <summary>让 pnlChartBox 高度跟随滚动容器,横向溢出时容器自动出现水平滚动条。</summary>
        private void pnlChartPanel_Resize(object sender, EventArgs e)
        {
            int h = pnlChartPanel.ClientSize.Height;
            if (h <= 0) h = pnlChartBox.Height;
            pnlChartBox.Height = h;
            pnlChartPanel.AutoScrollMinSize = new Size(pnlChartBox.Width, 0);
            pnlChartBox.Invalidate();
        }

        /// <summary>
        /// 自绘柱状图(GDI+,不依赖 Chart 控件):边距 6/22/22/38;柱宽 78% 槽宽,上限 80px;0 计数画 3px 浅灰占位块;
        /// 最高柱超过 plotRect 3/4 时整体按比例缩小,让最高柱停在 3/4 处,数字始终画在柱外。
        /// </summary>
        private void pnlChartBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. 算绘图区域
            var rect = pnlChartBox.ClientRectangle;
            const int marginLeft = 6, marginRight = 22, marginTop = 22, marginBottom = 38;
            var plotRect = new Rectangle(
                rect.X + marginLeft, rect.Y + marginTop,
                rect.Width - marginLeft - marginRight, rect.Height - marginTop - marginBottom);
            if (plotRect.Width < 30 || plotRect.Height < 30) return;

            // 2. 准备数据 + 缩放比例
            var items = _defectCounts.OrderByDescending(kv => kv.Value).ToList();
            if (items.Count == 0) return;
            int maxVal = items.Max(kv => kv.Value);
            if (maxVal == 0) maxVal = 1;
            float maxBarRatio = items.Max(kv => kv.Value) / (float)maxVal;
            float barScale = maxBarRatio > 0.75f ? (0.75f / maxBarRatio) : 1f;

            // 3. 水平网格 + 底部 X 轴
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

            // 4. 画柱
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

                    // 0 计数画 3px 浅灰占位块,否则按 maxVal 比例
                    Color color;
                    float drawH;
                    if (count == 0) { color = Color.FromArgb(225, 225, 225); drawH = 3f; }
                    else { color = ChartColors[i % ChartColors.Length]; drawH = Math.Max(plotRect.Height * (count / (float)maxVal) * barScale, 4f); }

                    var barRect = new RectangleF(xCenter - barWidth / 2f, plotRect.Bottom - drawH, barWidth, drawH);
                    using (var brush = new SolidBrush(color)) g.FillRectangle(brush, barRect);
                    // 边框用同色调加深,避免黑边突兀
                    using (var pen = new Pen(ControlPaint.Dark(color, 0.25f), 1)) g.DrawRectangle(pen, barRect.X, barRect.Y, barRect.Width, barRect.Height);

                    // 柱顶数字:画柱外黑字(整体缩放已保证不会超出 plotRect)
                    if (count > 0) g.DrawString(count.ToString(), barFont, Brushes.Black, xCenter, barRect.Y - 16, sf);

                    // X 轴标签:贴在 X 轴下方 8px,留够缓冲不和图像区重影
                    string label = name.Length > 8 ? name.Substring(0, 7) + "…" : name;
                    g.DrawString(label, axisFont, Brushes.Black, xCenter, plotRect.Bottom + 8, sf);
                }
            }
        }
    }
}