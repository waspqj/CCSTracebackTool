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

        // cboQr 的 "(全部)" 用空字符串表示
        private const string AllSentinel = "";

        // 缺陷类型出现次数缓存(供柱状图绘制)
        private Dictionary<string, int> _defectCounts = new Dictionary<string, int>();

        // 设 cboQr 会触发 cboQr_SelectedIndexChanged 重铺 alias 下拉;
        // SelectQrAndAlias 回填时手动选 alias,需要短路联动避免被覆盖。
        private bool _suppressAliasRefresh;

        // 启动时从 ApplicationSetting.json 加载的 MES 日志根目录。
        // 加载失败时为 null,数据按钮保持禁用。
        private string _mesLogRoot;

        // 重复点击 btnUpdate 守卫:三元组完全没变 → 不重复读 CSV、不弹窗。
        // 同时是"按二维码统计"一对一窗口的失效判定 —— 缓存更新后旧窗口视为过期。
        private DateTime? _lastLoadedDate;
        private TimeSpan? _lastLoadedStart;
        private TimeSpan? _lastLoadedEnd;

        // "按二维码统计" 一对一窗口引用;关掉后通过 FormClosed 清回 null。
        private QrStatisticsForm _qrStatsForm;

        // 上次开窗时喂给 QrStatisticsForm 的 _records 引用。
        // 用户重新"更新数据"后 _records 是新 List,即使三元组一样旧窗也是过期数据。
        private object _lastShownRecordsRef;

        // ApplicationSetting.json 反序列化出的完整 DTO。
        // 启动时 MainForm_Load 从 exe 同目录加载,失败则为 null。
        private ApplicationSettingDto _appConfig;

        // 引导式高亮:依次高亮 3 个主按钮(btnUpdate → btnQrStatistics → btnScanFiles),线性流程。
        private GuideHighlighter _guide;

        // 当前显示在 dgvNgList 上方的"多次检测选择"浮层。新扫描前先 dispose 旧的,避免叠加。
        private Panel _activeDetectionPopup;

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

        /// <summary>
        /// 启动初始化:加载配置、定宽、铺底、装引导。
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // 默认:日期=今天,时段=全天 00:00 ~ 23:59
            dtpDate.Value = DateTime.Today;
            mtbStartTime.Text = "00:00";
            mtbEndTime.Text = "23:59";

            // 启动加载 ApplicationSetting.json:失败就禁用三个数据按钮并弹错。
            string jsonPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "ApplicationSetting.json");
            try
            {
                _appConfig = ConfigLoader.Load(jsonPath);
                _mesLogRoot = _appConfig.PathMesLog;
                btnUpdate.Enabled = true;
                btnQrStatistics.Enabled = true;
                if (btnScanFiles != null) btnScanFiles.Enabled = true;
            }
            catch (ConfigLoadException ex)
            {
                _appConfig = null;
                _mesLogRoot = null;
                btnUpdate.Enabled = false;
                btnQrStatistics.Enabled = false;
                if (btnScanFiles != null) btnScanFiles.Enabled = false;
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
            if (leftWidth > 0 && leftWidth <= maxDistance)
            {
                splitMain.SplitterDistance = leftWidth;
            }

            // 铺底:统计区文字、占位行、柱状图(均为 0)
            UpdateStatsText(0, 0, 0, 0, 0);
            ShowEmptyNgList();
            BuildDefectCounts();

            // 启动引导:不主动点,等用户先选日期
            _guide = new GuideHighlighter();
            dtpDate.ValueChanged += DtpDate_ValueChanged;
        }

        // 引导第 1 步:选定日期 → 高亮"更新数据"
        private void DtpDate_ValueChanged(object sender, EventArgs e)
        {
            _guide?.Highlight(btnUpdate);
        }

        /// <summary>
        /// dgvNgList 显示一行占位,提示要先点 "扫描文件"。
        /// </summary>
        private void ShowEmptyNgList(string tip = "请先选二维码+编号再点 \"扫描文件\"")
        {
            dgvNgList.Rows.Clear();
            // 同步清空右侧"图像信息区"
            txtImgQr.Text = "";
            txtImgAlias.Text = "";
            txtImgDefect.Text = "";
            txtImgTime.Text = "";

            var row = new DataGridViewRow();
            row.CreateCells(dgvNgList);
            row.Cells[0].Value = "—";
            row.Cells[1].Value = "—";
            row.Cells[2].Value = "—";
            row.Cells[3].Value = tip;
            row.Cells[4].Value = "";
            dgvNgList.Rows.Add(row);
        }

        // 同一 processName 内从 1 起累加的段内序号 key;没值时兜底"(未分类)"。
        private static string NgTypeKey(DetectionRecord r)
        {
            if (r == null || string.IsNullOrWhiteSpace(r.ProcessName)) return "(未分类)";
            return r.ProcessName.Trim();
        }

        /// <summary>
        /// "更新数据" —— 按日期筛文件 + 按时间段过滤行,载入到内存并刷新下拉与统计。
        ///   - 二维码去重(一个二维码 = 一个产品)
        ///   - 编号去重(等选了 QR 再按 QR 联动)
        ///   - 缺陷类型按硬编码清单铺,不去重
        /// </summary>
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            // 1. 校验时段格式
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

            // 2. 三元组守卫:完全没变 → 不重复读、不弹窗
            if (_lastLoadedDate.HasValue && _lastLoadedStart.HasValue && _lastLoadedEnd.HasValue
                && _lastLoadedDate.Value == date
                && _lastLoadedStart.Value == startTs
                && _lastLoadedEnd.Value == endTs)
            {
                return;
            }

            // 3. 读 CSV 区间内的全部记录
            var reader = new CsvLogReader();
            int matchedFiles;
            _records = reader.ReadRange(_mesLogRoot, startTime, endTime, out matchedFiles);

            // 4. 算出 QR / 编号 / 缺陷类型 3 个下拉
            // 二维码:去重 + 排序
            var qrs = _records.Select(r => r.QrCode)
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Distinct()
                              .OrderBy(s => s)
                              .ToList();
            // 编号:只去重,不排序 —— CSV 来的顺序就是想要的顺序
            var aliases = _records.Select(r => r.Alias)
                                  .Where(s => !string.IsNullOrWhiteSpace(s))
                                  .Distinct()
                                  .ToList();

            cboQr.Items.Clear();
            cboQr.Items.Add(AllSentinel); // 第 0 项 = "(全部)"
            cboQr.Items.AddRange(qrs.Cast<object>().ToArray());
            cboQr.SelectedIndex = 0;

            // 编号先按当前 QR(= 全部) 全量铺
            RefreshAliasCombo(qrs, aliases);

            // 缺陷类型按硬编码清单铺
            cboDefect.Items.Clear();
            cboDefect.Items.AddRange(HardcodedDefectTypes.Cast<object>().ToArray());

            // 5. 刷统计文本 + 柱状图
            int ngCount = _records.Count(r => r.IsNg);
            UpdateStatsText(matchedFiles, _records.Count, qrs.Count, aliases.Count, ngCount);
            BuildDefectCounts();

            // 6. 弹结果
            MessageBox.Show(
                $"日期 {date:yyyy-MM-dd} 匹配 {matchedFiles} 个 CSV。\n" +
                $"时段 {startTs:hh\\:mm} ~ {endTs:hh\\:mm} 内载入 {_records.Count} 条记录。",
                "更新数据", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 7. 引导推进 + 缓存三元组
            if (_records.Count > 0)
            {
                _guide.Highlight(btnQrStatistics);
            }

            _lastLoadedDate = date;
            _lastLoadedStart = startTs;
            _lastLoadedEnd = endTs;
        }

        /// <summary>
        /// 扫描按钮:基于当前 cboQr + cboAlias + dtpDate,扫 NGImgSave 下的 NG 错图,
        /// 用已加载的 CSV 当 alias 过滤 + 时间锚,结果喂给 dgvNgList。
        ///
        /// 关键规则:
        ///   1. 必须先点 "更新数据" 加载 CSV —— alias 过滤依赖 CSV 的时间戳。
        ///   2. 取该 QR + alias + 当天的所有 CSV 检测行(按 Time 升序)。
        ///   3. 每张 NG 图必须 Time 严格早于某 CSV 行,且差 ≤ 10 分钟,才算这次检测的。
        ///   4. 同 processName 内,相邻图差 > 30 秒 → 切新段,段内序号从 1 重起。
        ///   5. 单次检测(csvRows.Count==1)直接渲染。
        ///   6. 多次检测(csvRows.Count>1)→ 在 dgvNgList 中间偏上弹浮层,用户选:
        ///      "本次检测" = 只渲染最近那次;"二维码一键加载" = 全部渲染,最近那次原色、其他灰色。
        ///   7. 每次点扫描都重新走完整流程(包含弹浮层),不缓存三元组。
        /// </summary>
        private void btnScanFiles_Click(object sender, EventArgs e)
        {
            // 1. 前置校验
            if (_appConfig == null)
            {
                MessageBox.Show("请先加载配置(ApplicationSetting.json)。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string qr = cboQr.SelectedItem as string;
            string alias = cboAlias.SelectedItem as string;
            // 选 "(全部)" 时拒绝扫描 —— 必须选到具体 QR + Alias
            if (string.IsNullOrEmpty(qr) || qr == AllSentinel)
            {
                MessageBox.Show("请先在 \"二维码\" 下拉里选中具体二维码,再扫描。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrEmpty(alias))
            {
                MessageBox.Show("请先在 \"编号\" 下拉里选中具体编号,再扫描。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_records == null || _records.Count == 0)
            {
                MessageBox.Show("请先点击 \"更新数据\" 加载 CSV,再用编号过滤扫描 NG 错图。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DateTime date = dtpDate.Value.Date;

            // 2. 挑出本次相关的 CSV 检测行(QR + alias + 当天,按 Time 升序)
            var csvRows = _records
                .Where(r => r.QrCode == qr
                         && r.Alias == alias
                         && r.Time != default
                         && r.Time.Date == date)
                .OrderBy(r => r.Time)
                .ToList();

            dgvNgList.Rows.Clear();
            if (csvRows.Count == 0)
            {
                ShowEmptyNgList("(" + qr + " / " + alias + " 当天无 CSV 检测记录)");
                return;
            }

            // 3. 扫 NGImgSave 拿该 QR 当天所有 processName 下的 NG 图
            //    alias 在文件系统层面没法过滤,靠后面 10 分钟时间窗反向归属
            var allNgFiles = FileScanner.Scan(qr, alias, date, _appConfig.NGImgSave);
            if (allNgFiles.Count == 0)
            {
                ShowEmptyNgList("(该二维码当天没有任何 NG 错图)");
                return;
            }

            // 4. 扫描成功 → 结束引导(每次点扫描都重新选模式,所以不需要缓存)
            _guide?.Clear();

            // 5. 单次检测 / 多次检测 分支
            //    多次检测时先清掉旧的浮层(用户可能切了 QR/Alias 再点扫描)
            if (_activeDetectionPopup != null && !_activeDetectionPopup.IsDisposed)
            {
                _activeDetectionPopup.Dispose();
                _activeDetectionPopup = null;
            }
            if (csvRows.Count > 1)
            {
                _activeDetectionPopup = ShowDetectionModePopup(csvRows, qr, alias, date, allNgFiles);
                return;
            }
            RenderDetections(csvRows, qr, alias, date, allNgFiles, highlightRecent: false);
        }

        /// <summary>
        /// 把 csvRows 对应的 NG 行喂给 dgvNgList。每条 csvRow 独立:
        ///   - 自己的 csvRowSeq 从 1 重起
        ///   - 自己的 segN 在 processName 内从 1 重起
        ///   - 没匹配到 NG 时渲染一行占位
        /// 当 highlightRecent=true 时,csvRows 中 Time 最大的那次(最近那次)的所有行
        /// (NG 行 + 占位)用红底粗体突出,其他保持原色(紫底粗体 / 浅黄斜体)。
        /// </summary>
        private void RenderDetections(
            List<DetectionRecord> csvRows,
            string qr, string alias, DateTime date,
            List<DetectionRecord> allNgFiles,
            bool highlightRecent)
        {
            const int SEG_GAP_SECONDS = 30;
            const int MAX_BEFORE_MINUTES = 10;

            // 按 Time 升序,最后一个就是最近那次(同 ReferenceEquals 判定高亮归属)
            var recent = csvRows[csvRows.Count - 1];

            foreach (var csvRow in csvRows)
            {
                bool isRecent = highlightRecent && ReferenceEquals(csvRow, recent);

                // 本次检测的 NG:严格早于 CSV.Time,且差 ≤ 10 分钟
                // (NewFileRecord 里若文件名解析失败,Time 会落到当天 00:00:00 —— 自然被阈值筛掉)
                var matched = allNgFiles
                    .Where(f => f.Time < csvRow.Time
                             && (csvRow.Time - f.Time).TotalMinutes <= MAX_BEFORE_MINUTES)
                    .ToList();

                if (matched.Count == 0)
                {
                    bool useOriginal = csvRows.Count == 1 || isRecent;
                    AddNgPlaceholder(csvRow, qr, useOriginal);
                    continue;
                }

                // 同 processName 内按时间排序,30 秒阈值切段;
                // 但每台相机只保留"段末时间最靠 csvRow.Time 的前一段"(其他段归不到本次检测)。
                var segments = new List<(string ProcName, int SegN, DateTime Time, DetectionRecord Rec)>();
                foreach (var procGroup in matched.GroupBy(f => NgTypeKey(f)))
                {
                    var sorted = procGroup.OrderBy(f => f.Time).ToList();

                    // 第一步:30 秒切段
                    var cutSegs = new List<List<DetectionRecord>>();
                    var cur = new List<DetectionRecord>();
                    DateTime? lastTime = null;
                    foreach (var f in sorted)
                    {
                        if (lastTime.HasValue
                            && (f.Time - lastTime.Value).TotalSeconds > SEG_GAP_SECONDS)
                        {
                            if (cur.Count > 0) cutSegs.Add(cur);
                            cur = new List<DetectionRecord>();
                        }
                        cur.Add(f);
                        lastTime = f.Time;
                    }
                    if (cur.Count > 0) cutSegs.Add(cur);

                    // 第二步:挑"段末时间最靠 csvRow.Time 的前一段"
                    var closestSeg = cutSegs
                        .OrderByDescending(s => s.Max(x => x.Time))
                        .FirstOrDefault();
                    if (closestSeg == null) continue;

                    // 段内从 1 起编号
                    int segN = 0;
                    foreach (var f in closestSeg.OrderBy(f => f.Time))
                    {
                        segN++;
                        segments.Add((procGroup.Key, segN, f.Time, f));
                    }
                }

                // 渲染 NG 行
                int csvRowSeq = 0;
                foreach (var seg in segments.OrderBy(s => s.Time))
                {
                    csvRowSeq++;
                    dgvNgList.Rows.Add(
                        csvRowSeq.ToString(),
                        $"{seg.ProcName}_{seg.SegN}",
                        seg.Time.ToString("HH:mm:ss.fff"),
                        seg.Rec.FileReason ?? "",
                        seg.Rec.FilePath ?? "");
                    int idx = dgvNgList.Rows.Count - 1;
                    dgvNgList.Rows[idx].Tag = seg.Rec;    // 双击时反查 QR/Alias
                    // 配色规则:
                    //   - 单 csvRow("本次检测"按钮):全部原色
                    //   - 多 csvRow("二维码一键加载"按钮):最近那次原色、其他灰色
                    bool useOriginal = csvRows.Count == 1 || isRecent;
                    dgvNgList.Rows[idx].DefaultCellStyle.BackColor = useOriginal
                        ? Color.Lavender
                        : Color.LightGray;
                    dgvNgList.Rows[idx].DefaultCellStyle.Font =
                        new Font(dgvNgList.Font, FontStyle.Bold);
                }
            }
        }

        /// <summary>
        /// "二维码一键加载" 路径:把该 QR 当天所有 NG 图(不过 10 分钟窗、不切段、不选最近段)
        /// 按时间排序平铺到 dgvNgList,每行:序号(全局连续) / ProcessName / NG 原始时间 / 缺陷 / 文件路径。
        ///
        /// 时间锚 = 最近那次检测(csvRows.Last()):
        ///   - 锚点前 10 分钟内的 NG → 原色(Lavender,等同"本次检测的 NG")
        ///   - 其他 NG(早于锚点 10 分钟以上 / 晚于锚点) → 灰色(LightGray)
        ///
        /// 注意:加载的是 NG 文件夹里所有 NG 图,不是 csvRows 范围内的 NG 集合。
        /// </summary>
        private void RenderAllNgFiles(
            List<DetectionRecord> csvRows,
            string qr, string alias, DateTime date,
            List<DetectionRecord> allNgFiles)
        {
            const int MAX_BEFORE_MINUTES = 10;
            var anchor = csvRows[csvRows.Count - 1];   // 最近那次检测 = 时间锚

            int seq = 0;
            foreach (var f in allNgFiles.OrderBy(f => f.Time))
            {
                // 锚点前 10 分钟内的 NG → 这次检测的(原色);其他(早于 >10 分钟 / 晚于) → 灰色
                bool inThisSegment = f.Time <= anchor.Time
                    && (anchor.Time - f.Time).TotalMinutes <= MAX_BEFORE_MINUTES;

                seq++;
                dgvNgList.Rows.Add(
                    seq.ToString(),
                    f.ProcessName ?? "",
                    f.Time.ToString("HH:mm:ss.fff"),
                    f.FileReason ?? "",
                    f.FilePath ?? "");
                int idx = dgvNgList.Rows.Count - 1;
                dgvNgList.Rows[idx].Tag = f;            // 双击时反查 NG 文件
                dgvNgList.Rows[idx].DefaultCellStyle.BackColor = inThisSegment
                    ? Color.Lavender
                    : Color.LightGray;
                dgvNgList.Rows[idx].DefaultCellStyle.Font =
                    new Font(dgvNgList.Font, FontStyle.Bold);
            }
        }

        /// <summary>
        /// 给指定 csvRow 添加一行占位 —— 本次检测跑完但没匹配到任何 NG。
        /// expectedDir 用 scanner 的 dayDir + QR 拼出来,双击用 Explorer 打开排查。
        /// useOriginal=true 用浅黄底(原色),false 用灰色。
        /// </summary>
        private void AddNgPlaceholder(DetectionRecord csvRow, string qr, bool useOriginal)
        {
            string dayDir = FileScanner.TryFindDayDir(_appConfig.NGImgSave, csvRow.Time);
            string expectedDir = dayDir != null
                ? Path.Combine(dayDir, qr)
                : Path.Combine(_appConfig.NGImgSave,
                               csvRow.Time.ToString("yyyy-MM"),
                               csvRow.Time.ToString("yyyy-MM-dd"),
                               qr);

            dgvNgList.Rows.Add(
                "",                       // 序号(空,占位不参与计数)
                "(无 NG)",                // 检测类型
                csvRow.Time.ToString("HH:mm:ss"),
                "本次检测没有找到NG错图",
                expectedDir);
            int phIdx = dgvNgList.Rows.Count - 1;
            dgvNgList.Rows[phIdx].Tag = csvRow;
            // 配色与 NG 行保持一致
            dgvNgList.Rows[phIdx].DefaultCellStyle.BackColor = useOriginal
                ? Color.LightYellow
                : Color.LightGray;
            dgvNgList.Rows[phIdx].DefaultCellStyle.Font =
                new Font(dgvNgList.Font, FontStyle.Italic);
        }

        /// <summary>
        /// 多次检测时,在 dgvNgList 中间偏上位置弹一个浮层,让用户选:
        ///   1. 单次检测 —— 只渲染最近那次(csvRows.Last())的 NG
        ///   2. 一键修改 —— 渲染所有 csvRow 的 NG,最近那次红底粗体突出
        /// 浮层用代码动态创建,不依赖 Designer;点选后自动 dispose。
        /// 返回浮层引用给调用方,方便后续清理。
        /// </summary>
        private Panel ShowDetectionModePopup(
            List<DetectionRecord> csvRows,
            string qr, string alias, DateTime date,
            List<DetectionRecord> allNgFiles)
        {
            var popup = new Panel
            {
                Size = new Size(460, 150),
                BackColor = Color.LightYellow,
                BorderStyle = BorderStyle.FixedSingle,
                Parent = dgvNgList.Parent,
            };

            // 定位到 dgvNgList 中间偏上(从上沿往下 40px,水平居中)
            popup.Location = new Point(
                dgvNgList.Left + (dgvNgList.Width - popup.Width) / 2,
                dgvNgList.Top + 40);
            popup.BringToFront();

            var lblDesc = new Label
            {
                Text = $"该 QR/Alias 当天共检测 {csvRows.Count} 次,请选择查看方式:",
                Location = new Point(12, 10),
                AutoSize = true,
                Font = new Font(dgvNgList.Font, FontStyle.Bold),
            };
            popup.Controls.Add(lblDesc);

            var btnSingle = new Button
            {
                Text = "1. 本次检测",
                Location = new Point(12, 50),
                Size = new Size(210, 50),
                BackColor = Color.FromArgb(91, 155, 213),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            btnSingle.FlatAppearance.BorderSize = 0;
            btnSingle.Click += (s, e) =>
            {
                dgvNgList.Rows.Clear();
                RenderDetections(new List<DetectionRecord> { csvRows[csvRows.Count - 1] },
                                 qr, alias, date, allNgFiles, highlightRecent: false);
                popup.Dispose();
                _activeDetectionPopup = null;
            };
            popup.Controls.Add(btnSingle);

            var btnAll = new Button
            {
                Text = "2. 二维码一键加载",
                Location = new Point(232, 50),
                Size = new Size(210, 50),
                BackColor = Color.FromArgb(237, 125, 49),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            btnAll.FlatAppearance.BorderSize = 0;
            btnAll.Click += (s, e) =>
            {
                dgvNgList.Rows.Clear();
                RenderAllNgFiles(csvRows, qr, alias, date, allNgFiles);
                popup.Dispose();
                _activeDetectionPopup = null;
            };
            popup.Controls.Add(btnAll);

            return popup;
        }

        /// <summary>
        /// dgvNgList 双击某行:把"文件路径"列对应的文件装到 pnlImageBox 预览。
        /// .jpg/.bmp/.png/.jpeg → picImagePreview;.txt → txtImageTextPreview。
        /// 占位行(路径是期望目录)→ 用 Explorer 打开目录。
        /// </summary>
        private void dgvNgList_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvNgList.Rows[e.RowIndex];

            // 1. 先把右侧"图像信息区"四个文本框填上对应值
            //    QR/编号走 row.Tag 里的 DetectionRecord;缺陷类型/时间走 Cells
            PopulateImageInfo(row);

            // 2. 按路径分发
            string path = row.Cells["colNgPath"].Value as string;
            if (string.IsNullOrWhiteSpace(path))
            {
                ClearImagePreview();
                return;
            }

            // 占位行的"期望路径"是个目录(可能还不存在),双击直接用 Explorer 打开,
            // 比弹"文件不存在"提示对用户排查为什么这次没存 NG 更实用。
            if (Directory.Exists(path))
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
                }
                catch { /* 静默:Explorer 启动失败就不动 */ }
                return;
            }

            if (!File.Exists(path))
            {
                MessageBox.Show("文件不存在:\n" + path,
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ClearImagePreview();
                return;
            }

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".jpg" || ext == ".bmp" || ext == ".png" || ext == ".jpeg")
                ShowImagePreview(path);
            else if (ext == ".txt")
                ShowTextPreview(path);
            else
            {
                MessageBox.Show("不支持的文件类型: " + ext,
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearImagePreview();
            }
        }

        /// <summary>
        /// 双击 NG 行时把右侧"图像信息区"四个文本框填上对应值。
        ///   - 二维码 / 编号:从 row.Tag 里的 DetectionRecord 读
        ///   - 缺陷类型 / 时间:直接从 Cells 读(占位行 / 实际行都已就位)
        /// 顺带把 ForeColor 强制设成 Red —— ReadOnly TextBox 在某些系统主题下
        /// 会被主题样式覆盖 Designer.cs 里设的颜色,代码里再设一次更稳。
        /// </summary>
        private void PopulateImageInfo(DataGridViewRow row)
        {
            var rec = row.Tag as DetectionRecord;
            string qr     = rec?.QrCode ?? "";
            string alias  = rec?.Alias  ?? "";
            string reason = (row.Cells["colNgReason"].Value as string) ?? "";
            string time   = (row.Cells["colNgTime"].Value   as string) ?? "";

            txtImgQr.Text     = qr;
            txtImgAlias.Text  = alias;
            txtImgDefect.Text = reason;
            txtImgTime.Text   = time;

            // 运行时强制红字(应对某些主题把 ReadOnly TextBox 的颜色重置)
            var red = Color.Red;
            txtImgQr.ForeColor     = red;
            txtImgAlias.ForeColor  = red;
            txtImgDefect.ForeColor = red;
            txtImgTime.ForeColor   = red;
        }

        // 把图像文件装到 picImagePreview。先 Dispose 旧的,否则文件被锁住。
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

        // 清空预览区:回到"显示占位文字"状态。
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

        /// <summary>
        /// 编号下拉按当前二维码联动:
        ///   - 选了具体 QR → 只显示该 QR 对应的编号
        ///   - 选 "(全部)" → 显示全部编号
        /// 尽量保留上次选中的 alias(联动后还有的话)。
        /// </summary>
        private void cboQr_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_records == null || _suppressAliasRefresh) return;

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

            var list = aliases.Distinct().ToList();
            string prevSelected = cboAlias.SelectedItem as string;

            cboAlias.Items.Clear();
            cboAlias.Items.AddRange(list.Cast<object>().ToArray());

            if (!string.IsNullOrEmpty(prevSelected) && cboAlias.Items.Contains(prevSelected))
                cboAlias.SelectedItem = prevSelected;
        }

        // 兼容 btnUpdate 里直接铺全部编号的场景(后续 cboQr_SelectedIndexChanged 才会做联动过滤)
        private void RefreshAliasCombo(List<string> qrs, List<string> aliases)
        {
            cboAlias.Items.Clear();
            cboAlias.Items.AddRange(aliases.Cast<object>().ToArray());
        }

        /// <summary>
        /// QrStatisticsForm 双击某一行时通过 RowActivated 事件进来。
        /// 把主窗体 cboQr 切到传入的二维码(联动重铺 alias),再选中指定编号,
        /// 最后把主窗体顶上来 —— 子窗体保持打开。
        /// </summary>
        public void SelectQrAndAlias(string qr, string alias)
        {
            // 1. 前置校验
            if (_records == null || _records.Count == 0)
            {
                MessageBox.Show("请先在主界面点 \"更新数据\" 加载 CSV。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int qrIdx = cboQr.Items.IndexOf(qr);
            if (qrIdx < 0)
            {
                MessageBox.Show($"二维码 \"{qr}\" 不在当前已加载集合里。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. 短路 cboQr 联动 + 手动选 QR/alias
            //    设 cboQr 会触发 cboQr_SelectedIndexChanged 重铺 alias,
            //    用 _suppressAliasRefresh 短路后我们手动选 alias。
            _suppressAliasRefresh = true;
            cboQr.SelectedIndex = qrIdx;
            // 联动重铺后的 alias 下拉里可能不包含传入的 alias(理论上都在,但兜个底)
            if (!string.IsNullOrEmpty(alias) && cboAlias.Items.Contains(alias))
                cboAlias.SelectedItem = alias;
            _suppressAliasRefresh = false;

            // 3. 主窗体顶上来 —— 子窗体保持打开
            //    关键:主窗体被最小化时 Activate() 不会还原,必须先把 WindowState 设回 Normal。
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
            this.Activate();
            this.BringToFront();

            // 4. 引导第 3 步:在统计窗里双击了某行 → 高亮"扫描文件"
            _guide?.Highlight(btnScanFiles);
        }

        /// <summary>
        /// "按二维码统计" 一对一窗口:同一份数据 + 同一时段 → 激活已有窗口、不弹新窗;
        /// 数据更新了 → 关旧开新。
        /// </summary>
        private void btnQrStatistics_Click(object sender, EventArgs e)
        {
            // 1. 时段校验(同 btnUpdate)
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

            // 2. 已开着窗口的复用判定
            //    判定"同一份数据"必须同时满足:
            //      1) date/start/end 三元组一致
            //      2) _records 引用没变(用户重新"更新数据"后即使三元组一样旧窗也是过期)
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
                    // 同一份数据 → 激活已有窗口、不弹新窗。
                    // 关键:窗口被最小化时 Activate() 不会还原,必须先还原 WindowState。
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

            // 3. 开新窗:展示全部已载入记录(不做时间过滤,不去重);
            //    双击其中行 → RowActivated 回调 SelectQrAndAlias。
            var form = new QrStatisticsForm(startTime, endTime, _records);
            form.RowActivated += SelectQrAndAlias;
            form.FormClosed += QrStatsForm_FormClosed;
            _qrStatsForm = form;
            _lastShownRecordsRef = _records;
            form.Show();
            // 引导:不在此处亮 btnScanFiles,等用户在统计窗里双击行再亮(SelectQrAndAlias 里)
        }

        // 子窗体被关掉时清回 _qrStatsForm 引用,这样下次再点按钮可以开新窗。
        private void QrStatsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (ReferenceEquals(sender, _qrStatsForm))
                _qrStatsForm = null;
        }

        /// <summary>
        /// 把"已载入 / NG / 缺陷维度"的统计文本写到右侧 lblStatsText。
        /// 缺陷维度直接从 _records 现算,不依赖 BuildDefectCounts 的调用顺序。
        /// </summary>
        private void UpdateStatsText(int matchedFiles, int total, int qrCount, int aliasCount, int ngCount)
        {
            // 缺陷维度:从 _records 里现算
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

        /// <summary>
        /// 遍历 _records 里的 NG 行,按 DefectTypes 累加每种缺陷出现次数到 _defectCounts。
        /// 硬编码清单里的类型先置 0,这样启动时柱状图就能完整铺位;
        /// 硬编码里没有的类型(数据里出现的)也一并统计。
        /// 统计完后触发柱状图重绘。
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

        // 画布尺寸变了就重绘
        private void pnlChartBox_Resize(object sender, EventArgs e)
        {
            pnlChartBox.Invalidate();
        }

        /// <summary>
        /// 让 pnlChartBox 高度跟随滚动容器,AutoScrollMinSize 宽度按画布宽度设置。
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
        /// 自绘柱状图(GDI+,不依赖 Chart 控件):
        ///   - 左侧 6px / 右侧 22px(留空给最右柱) / 顶部 22px(柱顶数字) / 底部 38px(X 轴名)
        ///   - 柱宽占槽宽 78%,上限 80px;0 计数画 3px 浅灰占位块
        ///   - 最高柱超过 plotRect 3/4 时整体按比例缩小,让最高柱停在 3/4 处,数字始终画在柱外
        /// </summary>
        private void pnlChartBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. 算绘图区域
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

            // 2. 准备数据 + 缩放比例
            var items = _defectCounts.OrderByDescending(kv => kv.Value).ToList();
            if (items.Count == 0) return;
            int maxVal = items.Max(kv => kv.Value);
            if (maxVal == 0) maxVal = 1;

            // 最高柱实际高度占比,超过 3/4 就整体缩小,让最高柱停在 3/4 处
            float maxBarRatio = items.Max(kv => kv.Value) / (float)maxVal;
            float barScale = maxBarRatio > 0.75f ? (0.75f / maxBarRatio) : 1f;

            // 3. 水平网格 + 底部 X 轴(去掉纵坐标轴和刻度数字)
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

            // 4. 画柱:柱宽占槽宽 78%,上限 80px
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

                    // 颜色 + 高度:0 计数画 3px 浅灰占位块,否则按 maxVal 比例
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

                    // 柱顶数字:画柱外黑字(整体缩放已保证不会超出 plotRect)
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