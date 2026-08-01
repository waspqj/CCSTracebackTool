namespace CCS_Traceback
{
    /// <summary>
    /// QrStatisticsForm 设计器生成的代码块:dgvResults(4 列:编号 / 时间 / 二维码 / 状态)。
    /// 业务逻辑在 QrStatisticsForm.cs。
    /// </summary>
    partial class QrStatisticsForm
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
            this.components = new System.ComponentModel.Container();
            this.dgvResults = new System.Windows.Forms.DataGridView();
            this.colAlias = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colQrCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).BeginInit();
            this.SuspendLayout();
            //
            // dgvResults
            //
            this.dgvResults.AllowUserToAddRows = false;
            this.dgvResults.AllowUserToDeleteRows = false;
            this.dgvResults.AllowUserToResizeRows = false;
            this.dgvResults.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResults.DefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgvResults.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colAlias,
                this.colTime,
                this.colQrCode,
                this.colStatus});
            this.dgvResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvResults.Location = new System.Drawing.Point(0, 0);
            this.dgvResults.MultiSelect = false;
            this.dgvResults.Name = "dgvResults";
            this.dgvResults.ReadOnly = true;
            this.dgvResults.RowHeadersVisible = false;
            this.dgvResults.RowTemplate.Height = 28;
            this.dgvResults.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.dgvResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvResults.Size = new System.Drawing.Size(1100, 650);
            this.dgvResults.TabIndex = 0;
            //
            // colAlias
            //
            this.colAlias.HeaderText = "编号";
            this.colAlias.Name = "colAlias";
            this.colAlias.ReadOnly = true;
            this.colAlias.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colAlias.Width = 140;
            //
            // colTime
            //
            this.colTime.HeaderText = "时间";
            this.colTime.Name = "colTime";
            this.colTime.ReadOnly = true;
            this.colTime.Width = 200;
            //
            // colQrCode
            //
            this.colQrCode.HeaderText = "二维码";
            this.colQrCode.Name = "colQrCode";
            this.colQrCode.ReadOnly = true;
            this.colQrCode.Width = 300;
            //
            // colStatus
            //
            this.colStatus.HeaderText = "状态";
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colStatus.FillWeight = 200F;
            this.colStatus.MinimumWidth = 400;
            //
            // QrStatisticsForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1400, 700);
            this.Controls.Add(this.dgvResults);
            this.MinimumSize = new System.Drawing.Size(800, 400);
            this.Name = "QrStatisticsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "按二维码统计";
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView dgvResults;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAlias;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colQrCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
    }
}