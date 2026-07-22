namespace CCS_Traceback
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.dgvNgList = new System.Windows.Forms.DataGridView();
            this.colNgTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNgQr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNgStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNgSeq = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNgScan = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNgAlias = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNgPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblNgList = new System.Windows.Forms.Label();
            this.pnlFilters = new System.Windows.Forms.Panel();
            this.lblFilterDefect = new System.Windows.Forms.Label();
            this.cboDefect = new System.Windows.Forms.ComboBox();
            this.lblFilterAlias = new System.Windows.Forms.Label();
            this.cboAlias = new System.Windows.Forms.ComboBox();
            this.lblFilterQr = new System.Windows.Forms.Label();
            this.cboQr = new System.Windows.Forms.ComboBox();
            this.btnQrStatistics = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.pnlTime = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.lblTo = new System.Windows.Forms.Label();
            this.mtbEndTime = new System.Windows.Forms.MaskedTextBox();
            this.lblFrom = new System.Windows.Forms.Label();
            this.mtbStartTime = new System.Windows.Forms.MaskedTextBox();
            this.lblDate = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.tlpRight = new System.Windows.Forms.TableLayoutPanel();
            this.pnlImageInfo = new System.Windows.Forms.Panel();
            this.txtImgDefect = new System.Windows.Forms.TextBox();
            this.lblImgDefect = new System.Windows.Forms.Label();
            this.txtImgAlias = new System.Windows.Forms.TextBox();
            this.lblImgAlias = new System.Windows.Forms.Label();
            this.txtImgQr = new System.Windows.Forms.TextBox();
            this.lblImgQr = new System.Windows.Forms.Label();
            this.pnlImageBox = new System.Windows.Forms.Panel();
            this.lblImagePlaceholder = new System.Windows.Forms.Label();
            this.pnlStats = new System.Windows.Forms.Panel();
            this.lblStatsText = new System.Windows.Forms.Label();
            this.pnlChartPanel = new System.Windows.Forms.Panel();
            this.pnlChartBox = new System.Windows.Forms.Panel();
            this.lblChartPlaceholder = new System.Windows.Forms.Label();
            this.lblChartTitle = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvNgList)).BeginInit();
            this.pnlFilters.SuspendLayout();
            this.pnlTime.SuspendLayout();
            this.tlpRight.SuspendLayout();
            this.pnlImageInfo.SuspendLayout();
            this.pnlImageBox.SuspendLayout();
            this.pnlStats.SuspendLayout();
            this.pnlChartPanel.SuspendLayout();
            this.pnlChartBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitMain.Location = new System.Drawing.Point(0, 0);
            this.splitMain.Margin = new System.Windows.Forms.Padding(4);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.dgvNgList);
            this.splitMain.Panel1.Controls.Add(this.lblNgList);
            this.splitMain.Panel1.Controls.Add(this.pnlFilters);
            this.splitMain.Panel1.Controls.Add(this.btnQrStatistics);
            this.splitMain.Panel1.Controls.Add(this.btnUpdate);
            this.splitMain.Panel1.Controls.Add(this.pnlTime);
            this.splitMain.Panel1.Padding = new System.Windows.Forms.Padding(15, 13, 15, 13);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.tlpRight);
            this.splitMain.Panel2.Padding = new System.Windows.Forms.Padding(15, 13, 15, 13);
            this.splitMain.Size = new System.Drawing.Size(2370, 1177);
            this.splitMain.SplitterDistance = 770;
            this.splitMain.SplitterWidth = 8;
            this.splitMain.TabIndex = 0;
            // 
            // dgvNgList
            // 
            this.dgvNgList.AllowUserToAddRows = false;
            this.dgvNgList.AllowUserToDeleteRows = false;
            this.dgvNgList.AllowUserToResizeRows = false;
            this.dgvNgList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvNgList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvNgList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colNgTime,
            this.colNgQr,
            this.colNgStatus,
            this.colNgSeq,
            this.colNgScan,
            this.colNgAlias,
            this.colNgPath});
            this.dgvNgList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvNgList.Location = new System.Drawing.Point(15, 539);
            this.dgvNgList.Margin = new System.Windows.Forms.Padding(0);
            this.dgvNgList.MultiSelect = false;
            this.dgvNgList.Name = "dgvNgList";
            this.dgvNgList.ReadOnly = true;
            this.dgvNgList.RowHeadersVisible = false;
            this.dgvNgList.RowHeadersWidth = 62;
            this.dgvNgList.RowTemplate.Height = 24;
            this.dgvNgList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvNgList.Size = new System.Drawing.Size(740, 625);
            this.dgvNgList.TabIndex = 5;
            // 
            // colNgTime
            // 
            this.colNgTime.HeaderText = "时间";
            this.colNgTime.MinimumWidth = 8;
            this.colNgTime.Name = "colNgTime";
            this.colNgTime.ReadOnly = true;
            // 
            // colNgQr
            // 
            this.colNgQr.HeaderText = "二维码";
            this.colNgQr.MinimumWidth = 8;
            this.colNgQr.Name = "colNgQr";
            this.colNgQr.ReadOnly = true;
            // 
            // colNgStatus
            // 
            this.colNgStatus.HeaderText = "判定";
            this.colNgStatus.MinimumWidth = 8;
            this.colNgStatus.Name = "colNgStatus";
            this.colNgStatus.ReadOnly = true;
            // 
            // colNgSeq
            // 
            this.colNgSeq.HeaderText = "序号";
            this.colNgSeq.MinimumWidth = 8;
            this.colNgSeq.Name = "colNgSeq";
            this.colNgSeq.ReadOnly = true;
            // 
            // colNgScan
            // 
            this.colNgScan.HeaderText = "扫描条号";
            this.colNgScan.MinimumWidth = 8;
            this.colNgScan.Name = "colNgScan";
            this.colNgScan.ReadOnly = true;
            // 
            // colNgAlias
            // 
            this.colNgAlias.HeaderText = "编号";
            this.colNgAlias.MinimumWidth = 8;
            this.colNgAlias.Name = "colNgAlias";
            this.colNgAlias.ReadOnly = true;
            // 
            // colNgPath
            // 
            this.colNgPath.HeaderText = "图像路径";
            this.colNgPath.MinimumWidth = 8;
            this.colNgPath.Name = "colNgPath";
            this.colNgPath.ReadOnly = true;
            // 
            // lblNgList
            // 
            this.lblNgList.AutoSize = true;
            this.lblNgList.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblNgList.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblNgList.Location = new System.Drawing.Point(15, 493);
            this.lblNgList.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.lblNgList.Name = "lblNgList";
            this.lblNgList.Padding = new System.Windows.Forms.Padding(0, 10, 0, 5);
            this.lblNgList.Size = new System.Drawing.Size(137, 46);
            this.lblNgList.TabIndex = 4;
            this.lblNgList.Text = "NG 行列表:";
            // 
            // pnlFilters
            // 
            this.pnlFilters.Controls.Add(this.lblFilterDefect);
            this.pnlFilters.Controls.Add(this.cboDefect);
            this.pnlFilters.Controls.Add(this.lblFilterAlias);
            this.pnlFilters.Controls.Add(this.cboAlias);
            this.pnlFilters.Controls.Add(this.lblFilterQr);
            this.pnlFilters.Controls.Add(this.cboQr);
            this.pnlFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFilters.Location = new System.Drawing.Point(15, 213);
            this.pnlFilters.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.pnlFilters.Name = "pnlFilters";
            this.pnlFilters.Size = new System.Drawing.Size(740, 280);
            this.pnlFilters.TabIndex = 3;
            this.pnlFilters.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlFilters_Paint);
            // 
            // lblFilterDefect
            // 
            this.lblFilterDefect.AutoSize = true;
            this.lblFilterDefect.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblFilterDefect.Location = new System.Drawing.Point(5, 184);
            this.lblFilterDefect.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFilterDefect.Name = "lblFilterDefect";
            this.lblFilterDefect.Size = new System.Drawing.Size(116, 31);
            this.lblFilterDefect.TabIndex = 4;
            this.lblFilterDefect.Text = "缺陷类型:";
            // 
            // cboDefect
            // 
            this.cboDefect.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cboDefect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDefect.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.cboDefect.Location = new System.Drawing.Point(5, 220);
            this.cboDefect.Margin = new System.Windows.Forms.Padding(4);
            this.cboDefect.Name = "cboDefect";
            this.cboDefect.Size = new System.Drawing.Size(726, 39);
            this.cboDefect.TabIndex = 5;
            // 
            // lblFilterAlias
            // 
            this.lblFilterAlias.AutoSize = true;
            this.lblFilterAlias.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblFilterAlias.Location = new System.Drawing.Point(5, 96);
            this.lblFilterAlias.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFilterAlias.Name = "lblFilterAlias";
            this.lblFilterAlias.Size = new System.Drawing.Size(68, 31);
            this.lblFilterAlias.TabIndex = 2;
            this.lblFilterAlias.Text = "编号:";
            // 
            // cboAlias
            // 
            this.cboAlias.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cboAlias.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboAlias.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.cboAlias.Location = new System.Drawing.Point(5, 132);
            this.cboAlias.Margin = new System.Windows.Forms.Padding(4);
            this.cboAlias.Name = "cboAlias";
            this.cboAlias.Size = new System.Drawing.Size(726, 39);
            this.cboAlias.TabIndex = 3;
            // 
            // lblFilterQr
            // 
            this.lblFilterQr.AutoSize = true;
            this.lblFilterQr.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblFilterQr.Location = new System.Drawing.Point(5, 8);
            this.lblFilterQr.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFilterQr.Name = "lblFilterQr";
            this.lblFilterQr.Size = new System.Drawing.Size(92, 31);
            this.lblFilterQr.TabIndex = 0;
            this.lblFilterQr.Text = "二维码:";
            // 
            // cboQr
            // 
            this.cboQr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cboQr.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboQr.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.cboQr.Location = new System.Drawing.Point(5, 44);
            this.cboQr.Margin = new System.Windows.Forms.Padding(4);
            this.cboQr.Name = "cboQr";
            this.cboQr.Size = new System.Drawing.Size(726, 39);
            this.cboQr.TabIndex = 1;
            this.cboQr.SelectedIndexChanged += new System.EventHandler(this.cboQr_SelectedIndexChanged);
            // 
            // btnQrStatistics
            // 
            this.btnQrStatistics.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnQrStatistics.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.btnQrStatistics.Location = new System.Drawing.Point(15, 163);
            this.btnQrStatistics.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.btnQrStatistics.Name = "btnQrStatistics";
            this.btnQrStatistics.Size = new System.Drawing.Size(740, 50);
            this.btnQrStatistics.TabIndex = 2;
            this.btnQrStatistics.Text = "按二维码统计";
            this.btnQrStatistics.UseVisualStyleBackColor = true;
            this.btnQrStatistics.Click += new System.EventHandler(this.btnQrStatistics_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnUpdate.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.btnUpdate.Location = new System.Drawing.Point(15, 113);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(740, 50);
            this.btnUpdate.TabIndex = 1;
            this.btnUpdate.Text = "更新数据";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // pnlTime
            // 
            this.pnlTime.Controls.Add(this.label1);
            this.pnlTime.Controls.Add(this.lblTo);
            this.pnlTime.Controls.Add(this.mtbEndTime);
            this.pnlTime.Controls.Add(this.lblFrom);
            this.pnlTime.Controls.Add(this.mtbStartTime);
            this.pnlTime.Controls.Add(this.lblDate);
            this.pnlTime.Controls.Add(this.dtpDate);
            this.pnlTime.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTime.Location = new System.Drawing.Point(15, 13);
            this.pnlTime.Margin = new System.Windows.Forms.Padding(0);
            this.pnlTime.Name = "pnlTime";
            this.pnlTime.Size = new System.Drawing.Size(740, 100);
            this.pnlTime.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(334, 66);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 18);
            this.label1.TabIndex = 6;
            this.label1.Text = "--";
            // 
            // lblTo
            // 
            this.lblTo.AutoSize = true;
            this.lblTo.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblTo.Location = new System.Drawing.Point(402, 56);
            this.lblTo.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.lblTo.Name = "lblTo";
            this.lblTo.Size = new System.Drawing.Size(44, 31);
            this.lblTo.TabIndex = 4;
            this.lblTo.Text = "到:";
            // 
            // mtbEndTime
            // 
            this.mtbEndTime.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F);
            this.mtbEndTime.Location = new System.Drawing.Point(455, 52);
            this.mtbEndTime.Margin = new System.Windows.Forms.Padding(4);
            this.mtbEndTime.Mask = "00:00";
            this.mtbEndTime.Name = "mtbEndTime";
            this.mtbEndTime.Size = new System.Drawing.Size(258, 43);
            this.mtbEndTime.TabIndex = 5;
            this.mtbEndTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblFrom
            // 
            this.lblFrom.AutoSize = true;
            this.lblFrom.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblFrom.Location = new System.Drawing.Point(29, 56);
            this.lblFrom.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Size = new System.Drawing.Size(44, 31);
            this.lblFrom.TabIndex = 2;
            this.lblFrom.Text = "从:";
            // 
            // mtbStartTime
            // 
            this.mtbStartTime.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F);
            this.mtbStartTime.Location = new System.Drawing.Point(82, 52);
            this.mtbStartTime.Margin = new System.Windows.Forms.Padding(4);
            this.mtbStartTime.Mask = "00:00";
            this.mtbStartTime.Name = "mtbStartTime";
            this.mtbStartTime.Size = new System.Drawing.Size(209, 43);
            this.mtbStartTime.TabIndex = 3;
            this.mtbStartTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblDate
            // 
            this.lblDate.AutoSize = true;
            this.lblDate.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblDate.Location = new System.Drawing.Point(5, 8);
            this.lblDate.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(68, 31);
            this.lblDate.TabIndex = 0;
            this.lblDate.Text = "日期:";
            // 
            // dtpDate
            // 
            this.dtpDate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dtpDate.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.dtpDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDate.Location = new System.Drawing.Point(82, 4);
            this.dtpDate.Margin = new System.Windows.Forms.Padding(4);
            this.dtpDate.Name = "dtpDate";
            this.dtpDate.Size = new System.Drawing.Size(631, 38);
            this.dtpDate.TabIndex = 1;
            // 
            // tlpRight
            // 
            this.tlpRight.ColumnCount = 1;
            this.tlpRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpRight.Controls.Add(this.pnlImageInfo, 0, 0);
            this.tlpRight.Controls.Add(this.pnlImageBox, 0, 1);
            this.tlpRight.Controls.Add(this.pnlStats, 0, 2);
            this.tlpRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpRight.Location = new System.Drawing.Point(15, 13);
            this.tlpRight.Margin = new System.Windows.Forms.Padding(4);
            this.tlpRight.Name = "tlpRight";
            this.tlpRight.RowCount = 3;
            this.tlpRight.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 294F));
            this.tlpRight.Size = new System.Drawing.Size(1562, 1151);
            this.tlpRight.TabIndex = 0;
            // 
            // pnlImageInfo
            // 
            this.pnlImageInfo.Controls.Add(this.txtImgDefect);
            this.pnlImageInfo.Controls.Add(this.lblImgDefect);
            this.pnlImageInfo.Controls.Add(this.txtImgAlias);
            this.pnlImageInfo.Controls.Add(this.lblImgAlias);
            this.pnlImageInfo.Controls.Add(this.txtImgQr);
            this.pnlImageInfo.Controls.Add(this.lblImgQr);
            this.pnlImageInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlImageInfo.Location = new System.Drawing.Point(0, 0);
            this.pnlImageInfo.Margin = new System.Windows.Forms.Padding(0);
            this.pnlImageInfo.Name = "pnlImageInfo";
            this.pnlImageInfo.Size = new System.Drawing.Size(1562, 39);
            this.pnlImageInfo.TabIndex = 0;
            // 
            // txtImgDefect
            // 
            this.txtImgDefect.Location = new System.Drawing.Point(729, 6);
            this.txtImgDefect.Margin = new System.Windows.Forms.Padding(4);
            this.txtImgDefect.Name = "txtImgDefect";
            this.txtImgDefect.ReadOnly = true;
            this.txtImgDefect.Size = new System.Drawing.Size(89, 28);
            this.txtImgDefect.TabIndex = 5;
            // 
            // lblImgDefect
            // 
            this.lblImgDefect.AutoSize = true;
            this.lblImgDefect.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblImgDefect.Location = new System.Drawing.Point(596, 3);
            this.lblImgDefect.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblImgDefect.Name = "lblImgDefect";
            this.lblImgDefect.Size = new System.Drawing.Size(116, 31);
            this.lblImgDefect.TabIndex = 4;
            this.lblImgDefect.Text = "缺陷类型:";
            // 
            // txtImgAlias
            // 
            this.txtImgAlias.Location = new System.Drawing.Point(457, 6);
            this.txtImgAlias.Margin = new System.Windows.Forms.Padding(4);
            this.txtImgAlias.Name = "txtImgAlias";
            this.txtImgAlias.ReadOnly = true;
            this.txtImgAlias.Size = new System.Drawing.Size(102, 28);
            this.txtImgAlias.TabIndex = 3;
            // 
            // lblImgAlias
            // 
            this.lblImgAlias.AutoSize = true;
            this.lblImgAlias.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblImgAlias.Location = new System.Drawing.Point(365, 0);
            this.lblImgAlias.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblImgAlias.Name = "lblImgAlias";
            this.lblImgAlias.Size = new System.Drawing.Size(68, 31);
            this.lblImgAlias.TabIndex = 2;
            this.lblImgAlias.Text = "编号:";
            // 
            // txtImgQr
            // 
            this.txtImgQr.Location = new System.Drawing.Point(159, 3);
            this.txtImgQr.Margin = new System.Windows.Forms.Padding(4);
            this.txtImgQr.Name = "txtImgQr";
            this.txtImgQr.ReadOnly = true;
            this.txtImgQr.Size = new System.Drawing.Size(179, 28);
            this.txtImgQr.TabIndex = 1;
            // 
            // lblImgQr
            // 
            this.lblImgQr.AutoSize = true;
            this.lblImgQr.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblImgQr.Location = new System.Drawing.Point(34, 3);
            this.lblImgQr.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblImgQr.Name = "lblImgQr";
            this.lblImgQr.Size = new System.Drawing.Size(92, 31);
            this.lblImgQr.TabIndex = 0;
            this.lblImgQr.Text = "二维码:";
            // 
            // pnlImageBox
            // 
            this.pnlImageBox.BackColor = System.Drawing.Color.Black;
            this.pnlImageBox.Controls.Add(this.lblImagePlaceholder);
            this.pnlImageBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlImageBox.Location = new System.Drawing.Point(0, 47);
            this.pnlImageBox.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.pnlImageBox.Name = "pnlImageBox";
            this.pnlImageBox.Size = new System.Drawing.Size(1562, 810);
            this.pnlImageBox.TabIndex = 1;
            // 
            // lblImagePlaceholder
            // 
            this.lblImagePlaceholder.AutoSize = true;
            this.lblImagePlaceholder.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F);
            this.lblImagePlaceholder.ForeColor = System.Drawing.Color.Gray;
            this.lblImagePlaceholder.Location = new System.Drawing.Point(19, 15);
            this.lblImagePlaceholder.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblImagePlaceholder.Name = "lblImagePlaceholder";
            this.lblImagePlaceholder.Size = new System.Drawing.Size(201, 36);
            this.lblImagePlaceholder.TabIndex = 0;
            this.lblImagePlaceholder.Text = "[图像显示区域]";
            // 
            // pnlStats
            // 
            this.pnlStats.Controls.Add(this.lblStatsText);
            this.pnlStats.Controls.Add(this.pnlChartPanel);
            this.pnlStats.Controls.Add(this.lblChartTitle);
            this.pnlStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlStats.Location = new System.Drawing.Point(0, 865);
            this.pnlStats.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.pnlStats.Name = "pnlStats";
            this.pnlStats.Size = new System.Drawing.Size(1562, 286);
            this.pnlStats.TabIndex = 2;
            // 
            // lblStatsText
            // 
            this.lblStatsText.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblStatsText.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblStatsText.Location = new System.Drawing.Point(1258, 0);
            this.lblStatsText.Margin = new System.Windows.Forms.Padding(4);
            this.lblStatsText.Name = "lblStatsText";
            this.lblStatsText.Size = new System.Drawing.Size(304, 286);
            this.lblStatsText.TabIndex = 2;
            this.lblStatsText.Text = "时间范围:\r\n找到 __ 个CSV文件\r\n共 __ 行，NG  __ 行";
            // 
            // pnlChartPanel
            // 
            this.pnlChartPanel.AutoScroll = true;
            this.pnlChartPanel.AutoScrollMinSize = new System.Drawing.Size(1500, 0);
            this.pnlChartPanel.Controls.Add(this.pnlChartBox);
            this.pnlChartPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlChartPanel.Location = new System.Drawing.Point(0, 0);
            this.pnlChartPanel.Margin = new System.Windows.Forms.Padding(0);
            this.pnlChartPanel.Name = "pnlChartPanel";
            this.pnlChartPanel.Size = new System.Drawing.Size(1562, 286);
            this.pnlChartPanel.TabIndex = 1;
            this.pnlChartPanel.Resize += new System.EventHandler(this.pnlChartPanel_Resize);
            // 
            // pnlChartBox
            // 
            this.pnlChartBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlChartBox.Controls.Add(this.lblChartPlaceholder);
            this.pnlChartBox.Location = new System.Drawing.Point(0, 0);
            this.pnlChartBox.Margin = new System.Windows.Forms.Padding(0);
            this.pnlChartBox.Name = "pnlChartBox";
            this.pnlChartBox.Size = new System.Drawing.Size(1500, 286);
            this.pnlChartBox.TabIndex = 0;
            this.pnlChartBox.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlChartBox_Paint);
            this.pnlChartBox.Resize += new System.EventHandler(this.pnlChartBox_Resize);
            // 
            // lblChartPlaceholder
            // 
            this.lblChartPlaceholder.AutoSize = true;
            this.lblChartPlaceholder.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblChartPlaceholder.ForeColor = System.Drawing.Color.Gray;
            this.lblChartPlaceholder.Location = new System.Drawing.Point(180, 84);
            this.lblChartPlaceholder.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblChartPlaceholder.Name = "lblChartPlaceholder";
            this.lblChartPlaceholder.Size = new System.Drawing.Size(0, 31);
            this.lblChartPlaceholder.TabIndex = 0;
            this.lblChartPlaceholder.Visible = false;
            // 
            // lblChartTitle
            // 
            this.lblChartTitle.AutoSize = true;
            this.lblChartTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.lblChartTitle.Location = new System.Drawing.Point(4, 6);
            this.lblChartTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblChartTitle.Name = "lblChartTitle";
            this.lblChartTitle.Size = new System.Drawing.Size(164, 31);
            this.lblChartTitle.TabIndex = 0;
            this.lblChartTitle.Text = "缺陷类型统计:";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2370, 1177);
            this.Controls.Add(this.splitMain);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(1151, 704);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CCS 检测回溯工具";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel1.PerformLayout();
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvNgList)).EndInit();
            this.pnlFilters.ResumeLayout(false);
            this.pnlFilters.PerformLayout();
            this.pnlTime.ResumeLayout(false);
            this.pnlTime.PerformLayout();
            this.tlpRight.ResumeLayout(false);
            this.pnlImageInfo.ResumeLayout(false);
            this.pnlImageInfo.PerformLayout();
            this.pnlImageBox.ResumeLayout(false);
            this.pnlImageBox.PerformLayout();
            this.pnlStats.ResumeLayout(false);
            this.pnlStats.PerformLayout();
            this.pnlChartPanel.ResumeLayout(false);
            this.pnlChartBox.ResumeLayout(false);
            this.pnlChartBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel pnlTime;
        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.DateTimePicker dtpDate;
        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.MaskedTextBox mtbStartTime;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.MaskedTextBox mtbEndTime;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnQrStatistics;
        private System.Windows.Forms.Panel pnlFilters;
        private System.Windows.Forms.Label lblFilterQr;
        private System.Windows.Forms.ComboBox cboQr;
        private System.Windows.Forms.Label lblFilterAlias;
        private System.Windows.Forms.ComboBox cboAlias;
        private System.Windows.Forms.Label lblFilterDefect;
        private System.Windows.Forms.ComboBox cboDefect;
        private System.Windows.Forms.Label lblNgList;
        private System.Windows.Forms.DataGridView dgvNgList;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNgTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNgQr;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNgStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNgSeq;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNgScan;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNgAlias;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNgPath;
        private System.Windows.Forms.TableLayoutPanel tlpRight;
        private System.Windows.Forms.Panel pnlImageInfo;
        private System.Windows.Forms.Label lblImgQr;
        private System.Windows.Forms.TextBox txtImgQr;
        private System.Windows.Forms.Label lblImgAlias;
        private System.Windows.Forms.TextBox txtImgAlias;
        private System.Windows.Forms.Label lblImgDefect;
        private System.Windows.Forms.TextBox txtImgDefect;
        private System.Windows.Forms.Panel pnlImageBox;
        private System.Windows.Forms.Label lblImagePlaceholder;
        private System.Windows.Forms.Panel pnlStats;
        private System.Windows.Forms.Label lblChartTitle;
        private System.Windows.Forms.Panel pnlChartPanel;
        private System.Windows.Forms.Panel pnlChartBox;
        private System.Windows.Forms.Label lblChartPlaceholder;
        private System.Windows.Forms.Label lblStatsText;
        private System.Windows.Forms.Label label1;
    }
}