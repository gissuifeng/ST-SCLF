namespace SpacetimeSCLFlowAnalysis
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.btnSCLFPathBrowse = new System.Windows.Forms.Button();
            this.cmbFlowTable = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.cmbTimeUnit = new System.Windows.Forms.ComboBox();
            this.cmbFlowOrigin = new System.Windows.Forms.ComboBox();
            this.cmbFlowOriginTime = new System.Windows.Forms.ComboBox();
            this.cmbFlowDest = new System.Windows.Forms.ComboBox();
            this.cmbFlowDestTime = new System.Windows.Forms.ComboBox();
            this.cmbFlowValue = new System.Windows.Forms.ComboBox();
            this.cmbOriginFeatures = new System.Windows.Forms.ComboBox();
            this.cmbOriginField = new System.Windows.Forms.ComboBox();
            this.txtThreshold = new System.Windows.Forms.TextBox();
            this.txtTimeDistance = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtOutPath = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cmbDestFeatures = new System.Windows.Forms.ComboBox();
            this.cmbDestField = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.btnFlowTable = new System.Windows.Forms.Button();
            this.btnOriginFeatures = new System.Windows.Forms.Button();
            this.btnDestFeatures = new System.Windows.Forms.Button();
            this.label14 = new System.Windows.Forms.Label();
            this.txtOutName = new System.Windows.Forms.TextBox();
            this.lbl_outname_error = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.lbl_threshold_error = new System.Windows.Forms.Label();
            this.lbl_distance_error = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSCLFPathBrowse
            // 
            this.btnSCLFPathBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSCLFPathBrowse.Font = new System.Drawing.Font("宋体", 9F);
            this.btnSCLFPathBrowse.Image = ((System.Drawing.Image)(resources.GetObject("btnSCLFPathBrowse.Image")));
            this.btnSCLFPathBrowse.Location = new System.Drawing.Point(595, 617);
            this.btnSCLFPathBrowse.Name = "btnSCLFPathBrowse";
            this.btnSCLFPathBrowse.Size = new System.Drawing.Size(30, 32);
            this.btnSCLFPathBrowse.TabIndex = 27;
            this.btnSCLFPathBrowse.UseVisualStyleBackColor = true;
            this.btnSCLFPathBrowse.Click += new System.EventHandler(this.btnSCLFPath_Click);
            // 
            // cmbFlowTable
            // 
            this.cmbFlowTable.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbFlowTable.Font = new System.Drawing.Font("宋体", 9F);
            this.cmbFlowTable.FormattingEnabled = true;
            this.cmbFlowTable.Location = new System.Drawing.Point(15, 35);
            this.cmbFlowTable.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbFlowTable.Name = "cmbFlowTable";
            this.cmbFlowTable.Size = new System.Drawing.Size(570, 23);
            this.cmbFlowTable.TabIndex = 1;
            this.cmbFlowTable.DropDown += new System.EventHandler(this.cmbFlowTable_DropDown);
            this.cmbFlowTable.SelectedIndexChanged += new System.EventHandler(this.cmbFlowTable_SelectedIndexChanged);
            this.cmbFlowTable.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cmbFlowTable_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 9F);
            this.label1.Location = new System.Drawing.Point(15, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(135, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Input Flow Table";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 9F);
            this.label2.Location = new System.Drawing.Point(15, 75);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(143, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "Flow Origin Field";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("宋体", 9F);
            this.label4.Location = new System.Drawing.Point(325, 75);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(223, 15);
            this.label4.TabIndex = 6;
            this.label4.Text = "Flow Origin Time (Optional)";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 9F);
            this.label3.Location = new System.Drawing.Point(15, 140);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(183, 15);
            this.label3.TabIndex = 4;
            this.label3.Text = "Flow Destination Field";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("宋体", 9F);
            this.label5.Location = new System.Drawing.Point(325, 140);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(263, 15);
            this.label5.TabIndex = 8;
            this.label5.Text = "Flow Destination Time (Optional)";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("宋体", 9F);
            this.label6.Location = new System.Drawing.Point(15, 205);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(167, 15);
            this.label6.TabIndex = 10;
            this.label6.Text = "Flow Attribute Value";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("宋体", 9F);
            this.label7.Location = new System.Drawing.Point(15, 270);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(175, 15);
            this.label7.TabIndex = 12;
            this.label7.Text = "Input Origin Featrues";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("宋体", 9F);
            this.label8.Location = new System.Drawing.Point(15, 335);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(135, 15);
            this.label8.TabIndex = 14;
            this.label8.Text = "UID Origin Field";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("宋体", 9F);
            this.label11.Location = new System.Drawing.Point(15, 530);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(223, 15);
            this.label11.TabIndex = 20;
            this.label11.Text = "SCLF Pattern Rate Threshold";
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("宋体", 9F);
            this.label12.Location = new System.Drawing.Point(325, 530);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(263, 15);
            this.label12.TabIndex = 22;
            this.label12.Text = "SCLFlow Time distance (Optional)";
            // 
            // cmbTimeUnit
            // 
            this.cmbTimeUnit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbTimeUnit.Enabled = false;
            this.cmbTimeUnit.Font = new System.Drawing.Font("宋体", 9F);
            this.cmbTimeUnit.FormattingEnabled = true;
            this.cmbTimeUnit.Items.AddRange(new object[] {
            "seconds",
            "minutes",
            "hours",
            "days",
            "months",
            "years"});
            this.cmbTimeUnit.Location = new System.Drawing.Point(545, 555);
            this.cmbTimeUnit.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbTimeUnit.Name = "cmbTimeUnit";
            this.cmbTimeUnit.Size = new System.Drawing.Size(80, 23);
            this.cmbTimeUnit.TabIndex = 24;
            this.cmbTimeUnit.Text = "days";
            this.cmbTimeUnit.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cmbTimeUnit_KeyPress);
            // 
            // cmbFlowOrigin
            // 
            this.cmbFlowOrigin.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbFlowOrigin.Font = new System.Drawing.Font("宋体", 9F);
            this.cmbFlowOrigin.FormattingEnabled = true;
            this.cmbFlowOrigin.Location = new System.Drawing.Point(15, 100);
            this.cmbFlowOrigin.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbFlowOrigin.Name = "cmbFlowOrigin";
            this.cmbFlowOrigin.Size = new System.Drawing.Size(280, 23);
            this.cmbFlowOrigin.TabIndex = 3;
            this.cmbFlowOrigin.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cmbFlowOrigin_KeyPress);
            // 
            // cmbFlowOriginTime
            // 
            this.cmbFlowOriginTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbFlowOriginTime.Font = new System.Drawing.Font("宋体", 9F);
            this.cmbFlowOriginTime.FormattingEnabled = true;
            this.cmbFlowOriginTime.Location = new System.Drawing.Point(325, 100);
            this.cmbFlowOriginTime.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbFlowOriginTime.Name = "cmbFlowOriginTime";
            this.cmbFlowOriginTime.Size = new System.Drawing.Size(300, 23);
            this.cmbFlowOriginTime.TabIndex = 7;
            this.cmbFlowOriginTime.SelectedIndexChanged += new System.EventHandler(this.cmbFlowOriginTime_SelectedIndexChanged);
            this.cmbFlowOriginTime.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cmbFlowOriginTime_KeyPress);
            // 
            // cmbFlowDest
            // 
            this.cmbFlowDest.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbFlowDest.Font = new System.Drawing.Font("宋体", 9F);
            this.cmbFlowDest.FormattingEnabled = true;
            this.cmbFlowDest.Location = new System.Drawing.Point(15, 165);
            this.cmbFlowDest.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbFlowDest.Name = "cmbFlowDest";
            this.cmbFlowDest.Size = new System.Drawing.Size(280, 23);
            this.cmbFlowDest.TabIndex = 5;
            this.cmbFlowDest.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cmbFlowDest_KeyPress);
            // 
            // cmbFlowDestTime
            // 
            this.cmbFlowDestTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbFlowDestTime.Font = new System.Drawing.Font("宋体", 9F);
            this.cmbFlowDestTime.FormattingEnabled = true;
            this.cmbFlowDestTime.Location = new System.Drawing.Point(325, 165);
            this.cmbFlowDestTime.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbFlowDestTime.Name = "cmbFlowDestTime";
            this.cmbFlowDestTime.Size = new System.Drawing.Size(300, 23);
            this.cmbFlowDestTime.TabIndex = 9;
            this.cmbFlowDestTime.SelectedIndexChanged += new System.EventHandler(this.cmbFlowDestTime_SelectedIndexChanged);
            this.cmbFlowDestTime.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cmbFlowDestTime_KeyPress);
            // 
            // cmbFlowValue
            // 
            this.cmbFlowValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbFlowValue.Font = new System.Drawing.Font("宋体", 9F);
            this.cmbFlowValue.FormattingEnabled = true;
            this.cmbFlowValue.Location = new System.Drawing.Point(15, 230);
            this.cmbFlowValue.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbFlowValue.Name = "cmbFlowValue";
            this.cmbFlowValue.Size = new System.Drawing.Size(610, 23);
            this.cmbFlowValue.TabIndex = 11;
            this.cmbFlowValue.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cmbFlowAttrValue_KeyPress);
            // 
            // cmbOriginFeatures
            // 
            this.cmbOriginFeatures.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbOriginFeatures.Font = new System.Drawing.Font("宋体", 9F);
            this.cmbOriginFeatures.FormattingEnabled = true;
            this.cmbOriginFeatures.Location = new System.Drawing.Point(15, 295);
            this.cmbOriginFeatures.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbOriginFeatures.Name = "cmbOriginFeatures";
            this.cmbOriginFeatures.Size = new System.Drawing.Size(570, 23);
            this.cmbOriginFeatures.TabIndex = 13;
            this.cmbOriginFeatures.DropDown += new System.EventHandler(this.cmbOriginFeatures_DropDown);
            this.cmbOriginFeatures.SelectedIndexChanged += new System.EventHandler(this.cmbOriginFeatures_SelectedIndexChanged);
            this.cmbOriginFeatures.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cmbOriginFeatures_KeyPress);
            // 
            // cmbOriginField
            // 
            this.cmbOriginField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbOriginField.Font = new System.Drawing.Font("宋体", 9F);
            this.cmbOriginField.FormattingEnabled = true;
            this.cmbOriginField.Location = new System.Drawing.Point(15, 360);
            this.cmbOriginField.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbOriginField.Name = "cmbOriginField";
            this.cmbOriginField.Size = new System.Drawing.Size(610, 23);
            this.cmbOriginField.TabIndex = 15;
            this.cmbOriginField.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cmbOriginField_KeyPress);
            // 
            // txtThreshold
            // 
            this.txtThreshold.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtThreshold.Font = new System.Drawing.Font("宋体", 9F);
            this.txtThreshold.Location = new System.Drawing.Point(15, 555);
            this.txtThreshold.Name = "txtThreshold";
            this.txtThreshold.Size = new System.Drawing.Size(280, 25);
            this.txtThreshold.TabIndex = 21;
            this.txtThreshold.Text = "0.6";
            this.txtThreshold.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtThreshold.TextChanged += new System.EventHandler(this.txtThreshold_TextChanged);
            // 
            // txtTimeDistance
            // 
            this.txtTimeDistance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtTimeDistance.Enabled = false;
            this.txtTimeDistance.Font = new System.Drawing.Font("宋体", 9F);
            this.txtTimeDistance.Location = new System.Drawing.Point(325, 555);
            this.txtTimeDistance.Name = "txtTimeDistance";
            this.txtTimeDistance.Size = new System.Drawing.Size(210, 25);
            this.txtTimeDistance.TabIndex = 23;
            this.txtTimeDistance.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtTimeDistance.TextChanged += new System.EventHandler(this.txtTimeDistance_TextChanged);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("宋体", 9F);
            this.label13.Location = new System.Drawing.Point(15, 595);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(223, 15);
            this.label13.TabIndex = 25;
            this.label13.Text = "SCLFlow Pattern Output Path";
            // 
            // txtOutPath
            // 
            this.txtOutPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutPath.Font = new System.Drawing.Font("宋体", 9F);
            this.txtOutPath.Location = new System.Drawing.Point(15, 620);
            this.txtOutPath.Name = "txtOutPath";
            this.txtOutPath.Size = new System.Drawing.Size(570, 25);
            this.txtOutPath.TabIndex = 26;
            this.txtOutPath.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtOutPath_KeyPress);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Font = new System.Drawing.Font("Tahoma", 7F);
            this.btnOK.Location = new System.Drawing.Point(391, 730);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(110, 28);
            this.btnOK.TabIndex = 30;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Font = new System.Drawing.Font("Tahoma", 7F);
            this.btnCancel.Location = new System.Drawing.Point(515, 730);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(110, 28);
            this.btnCancel.TabIndex = 31;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // cmbDestFeatures
            // 
            this.cmbDestFeatures.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbDestFeatures.Font = new System.Drawing.Font("宋体", 9F);
            this.cmbDestFeatures.FormattingEnabled = true;
            this.cmbDestFeatures.Location = new System.Drawing.Point(15, 425);
            this.cmbDestFeatures.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbDestFeatures.Name = "cmbDestFeatures";
            this.cmbDestFeatures.Size = new System.Drawing.Size(570, 23);
            this.cmbDestFeatures.TabIndex = 17;
            this.cmbDestFeatures.DropDown += new System.EventHandler(this.cmbDestFeatures_DropDown);
            this.cmbDestFeatures.SelectedIndexChanged += new System.EventHandler(this.cmbDestFeatures_SelectedIndexChanged);
            // 
            // cmbDestField
            // 
            this.cmbDestField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbDestField.Font = new System.Drawing.Font("宋体", 9F);
            this.cmbDestField.FormattingEnabled = true;
            this.cmbDestField.Location = new System.Drawing.Point(15, 490);
            this.cmbDestField.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbDestField.Name = "cmbDestField";
            this.cmbDestField.Size = new System.Drawing.Size(610, 23);
            this.cmbDestField.TabIndex = 19;
            this.cmbDestField.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cmbDestField_KeyPress);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("宋体", 9F);
            this.label10.Location = new System.Drawing.Point(15, 465);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(175, 15);
            this.label10.TabIndex = 18;
            this.label10.Text = "UID Destination Field";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("宋体", 9F);
            this.label9.Location = new System.Drawing.Point(15, 400);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(215, 15);
            this.label9.TabIndex = 16;
            this.label9.Text = "Input Destination Featrues";
            // 
            // btnFlowTable
            // 
            this.btnFlowTable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFlowTable.Font = new System.Drawing.Font("宋体", 9F);
            this.btnFlowTable.Image = ((System.Drawing.Image)(resources.GetObject("btnFlowTable.Image")));
            this.btnFlowTable.Location = new System.Drawing.Point(595, 31);
            this.btnFlowTable.Name = "btnFlowTable";
            this.btnFlowTable.Size = new System.Drawing.Size(30, 32);
            this.btnFlowTable.TabIndex = 30;
            this.btnFlowTable.UseCompatibleTextRendering = true;
            this.btnFlowTable.UseVisualStyleBackColor = true;
            this.btnFlowTable.Click += new System.EventHandler(this.btnFlowTable_Click);
            // 
            // btnOriginFeatures
            // 
            this.btnOriginFeatures.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOriginFeatures.Font = new System.Drawing.Font("宋体", 9F);
            this.btnOriginFeatures.Image = ((System.Drawing.Image)(resources.GetObject("btnOriginFeatures.Image")));
            this.btnOriginFeatures.Location = new System.Drawing.Point(595, 291);
            this.btnOriginFeatures.Name = "btnOriginFeatures";
            this.btnOriginFeatures.Size = new System.Drawing.Size(30, 32);
            this.btnOriginFeatures.TabIndex = 31;
            this.btnOriginFeatures.UseVisualStyleBackColor = true;
            this.btnOriginFeatures.Click += new System.EventHandler(this.btnOriginFeatures_Click);
            // 
            // btnDestFeatures
            // 
            this.btnDestFeatures.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDestFeatures.Font = new System.Drawing.Font("宋体", 9F);
            this.btnDestFeatures.Image = ((System.Drawing.Image)(resources.GetObject("btnDestFeatures.Image")));
            this.btnDestFeatures.Location = new System.Drawing.Point(595, 420);
            this.btnDestFeatures.Name = "btnDestFeatures";
            this.btnDestFeatures.Size = new System.Drawing.Size(30, 32);
            this.btnDestFeatures.TabIndex = 32;
            this.btnDestFeatures.UseVisualStyleBackColor = true;
            this.btnDestFeatures.Click += new System.EventHandler(this.btnDestFeatures_Click);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("宋体", 9F);
            this.label14.Location = new System.Drawing.Point(15, 660);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(223, 15);
            this.label14.TabIndex = 28;
            this.label14.Text = "SCLFlow Pattern Output Name";
            // 
            // txtOutName
            // 
            this.txtOutName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutName.Font = new System.Drawing.Font("宋体", 9F);
            this.txtOutName.Location = new System.Drawing.Point(15, 685);
            this.txtOutName.Name = "txtOutName";
            this.txtOutName.Size = new System.Drawing.Size(610, 25);
            this.txtOutName.TabIndex = 29;
            this.txtOutName.TextChanged += new System.EventHandler(this.txtOutName_TextChanged);
            // 
            // lbl_outname_error
            // 
            this.lbl_outname_error.AutoSize = true;
            this.lbl_outname_error.Font = new System.Drawing.Font("宋体", 9F);
            this.lbl_outname_error.Location = new System.Drawing.Point(15, 685);
            this.lbl_outname_error.Name = "lbl_outname_error";
            this.lbl_outname_error.Size = new System.Drawing.Size(0, 15);
            this.lbl_outname_error.TabIndex = 0;
            // 
            // toolTip1
            // 
            this.toolTip1.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Error;
            // 
            // lbl_threshold_error
            // 
            this.lbl_threshold_error.AutoSize = true;
            this.lbl_threshold_error.Location = new System.Drawing.Point(15, 555);
            this.lbl_threshold_error.Name = "lbl_threshold_error";
            this.lbl_threshold_error.Size = new System.Drawing.Size(0, 15);
            this.lbl_threshold_error.TabIndex = 0;
            // 
            // lbl_distance_error
            // 
            this.lbl_distance_error.AutoSize = true;
            this.lbl_distance_error.Location = new System.Drawing.Point(325, 555);
            this.lbl_distance_error.Name = "lbl_distance_error";
            this.lbl_distance_error.Size = new System.Drawing.Size(0, 15);
            this.lbl_distance_error.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(642, 773);
            this.Controls.Add(this.lbl_distance_error);
            this.Controls.Add(this.lbl_threshold_error);
            this.Controls.Add(this.lbl_outname_error);
            this.Controls.Add(this.txtOutName);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.btnDestFeatures);
            this.Controls.Add(this.btnOriginFeatures);
            this.Controls.Add(this.btnFlowTable);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.cmbDestField);
            this.Controls.Add(this.cmbDestFeatures);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtOutPath);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.txtTimeDistance);
            this.Controls.Add(this.txtThreshold);
            this.Controls.Add(this.cmbOriginField);
            this.Controls.Add(this.cmbOriginFeatures);
            this.Controls.Add(this.cmbFlowValue);
            this.Controls.Add(this.cmbFlowDestTime);
            this.Controls.Add(this.cmbFlowDest);
            this.Controls.Add(this.cmbFlowOriginTime);
            this.Controls.Add(this.cmbFlowOrigin);
            this.Controls.Add(this.cmbTimeUnit);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbFlowTable);
            this.Controls.Add(this.btnSCLFPathBrowse);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(660, 820);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Spatio-temporal sel-co-location Flow Pattern Analysis";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSCLFPathBrowse;
        private System.Windows.Forms.ComboBox cmbFlowTable;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ComboBox cmbTimeUnit;
        private System.Windows.Forms.ComboBox cmbFlowOrigin;
        private System.Windows.Forms.ComboBox cmbFlowOriginTime;
        private System.Windows.Forms.ComboBox cmbFlowDest;
        private System.Windows.Forms.ComboBox cmbFlowDestTime;
        private System.Windows.Forms.ComboBox cmbFlowValue;
        private System.Windows.Forms.ComboBox cmbOriginFeatures;
        private System.Windows.Forms.ComboBox cmbOriginField;
        private System.Windows.Forms.TextBox txtThreshold;
        private System.Windows.Forms.TextBox txtTimeDistance;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtOutPath;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ComboBox cmbDestFeatures;
        private System.Windows.Forms.ComboBox cmbDestField;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btnFlowTable;
        private System.Windows.Forms.Button btnOriginFeatures;
        private System.Windows.Forms.Button btnDestFeatures;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox txtOutName;
        private System.Windows.Forms.Label lbl_outname_error;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lbl_threshold_error;
        private System.Windows.Forms.Label lbl_distance_error;
    }
}