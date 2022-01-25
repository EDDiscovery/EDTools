namespace EDDNRecorder
{
    partial class EDDNRecorder
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonLive = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.checkBoxWrapBody = new System.Windows.Forms.CheckBox();
            this.checkBoxFollow = new System.Windows.Forms.CheckBox();
            this.buttonBeta = new System.Windows.Forms.Button();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonLive
            // 
            this.buttonLive.Location = new System.Drawing.Point(3, 3);
            this.buttonLive.Name = "buttonLive";
            this.buttonLive.Size = new System.Drawing.Size(108, 32);
            this.buttonLive.TabIndex = 0;
            this.buttonLive.Text = "Record Live";
            this.buttonLive.UseVisualStyleBackColor = true;
            this.buttonLive.Click += new System.EventHandler(this.buttonLive_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.checkBoxWrapBody);
            this.panel1.Controls.Add(this.checkBoxFollow);
            this.panel1.Controls.Add(this.buttonBeta);
            this.panel1.Controls.Add(this.buttonLive);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1469, 41);
            this.panel1.TabIndex = 2;
            // 
            // checkBoxWrapBody
            // 
            this.checkBoxWrapBody.AutoSize = true;
            this.checkBoxWrapBody.Location = new System.Drawing.Point(252, 12);
            this.checkBoxWrapBody.Name = "checkBoxWrapBody";
            this.checkBoxWrapBody.Size = new System.Drawing.Size(79, 17);
            this.checkBoxWrapBody.TabIndex = 2;
            this.checkBoxWrapBody.Text = "Wrap Body";
            this.checkBoxWrapBody.UseVisualStyleBackColor = true;
            this.checkBoxWrapBody.CheckedChanged += new System.EventHandler(this.checkBoxWrapBody_CheckedChanged);
            // 
            // checkBoxFollow
            // 
            this.checkBoxFollow.AutoSize = true;
            this.checkBoxFollow.Location = new System.Drawing.Point(358, 12);
            this.checkBoxFollow.Name = "checkBoxFollow";
            this.checkBoxFollow.Size = new System.Drawing.Size(56, 17);
            this.checkBoxFollow.TabIndex = 1;
            this.checkBoxFollow.Text = "Follow";
            this.checkBoxFollow.UseVisualStyleBackColor = true;
            // 
            // buttonBeta
            // 
            this.buttonBeta.Location = new System.Drawing.Point(117, 3);
            this.buttonBeta.Name = "buttonBeta";
            this.buttonBeta.Size = new System.Drawing.Size(108, 32);
            this.buttonBeta.TabIndex = 0;
            this.buttonBeta.Text = "RecordBeta";
            this.buttonBeta.UseVisualStyleBackColor = true;
            this.buttonBeta.Click += new System.EventHandler(this.buttonBeta_Click);
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AllowUserToResizeRows = false;
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3,
            this.Column4,
            this.Column5,
            this.Column6});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 41);
            this.dgv.Name = "dgv";
            this.dgv.RowHeadersVisible = false;
            this.dgv.Size = new System.Drawing.Size(1469, 706);
            this.dgv.TabIndex = 3;
            // 
            // Column1
            // 
            this.Column1.FillWeight = 25F;
            this.Column1.HeaderText = "Time";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            // 
            // Column2
            // 
            this.Column2.FillWeight = 15F;
            this.Column2.HeaderText = "Schema";
            this.Column2.Name = "Column2";
            this.Column2.ReadOnly = true;
            // 
            // Column3
            // 
            this.Column3.FillWeight = 25F;
            this.Column3.HeaderText = "Software";
            this.Column3.Name = "Column3";
            this.Column3.ReadOnly = true;
            // 
            // Column4
            // 
            this.Column4.FillWeight = 10F;
            this.Column4.HeaderText = "Version";
            this.Column4.Name = "Column4";
            this.Column4.ReadOnly = true;
            // 
            // Column5
            // 
            this.Column5.FillWeight = 20F;
            this.Column5.HeaderText = "UploaderID";
            this.Column5.Name = "Column5";
            this.Column5.ReadOnly = true;
            // 
            // Column6
            // 
            this.Column6.HeaderText = "Body";
            this.Column6.Name = "Column6";
            this.Column6.ReadOnly = true;
            // 
            // EDDNRecorder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1469, 747);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.panel1);
            this.Name = "EDDNRecorder";
            this.Text = "EDDN Recorder";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonLive;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.CheckBox checkBoxFollow;
        private System.Windows.Forms.CheckBox checkBoxWrapBody;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column4;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column5;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column6;
        private System.Windows.Forms.Button buttonBeta;
    }
}

