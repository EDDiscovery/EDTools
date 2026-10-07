namespace Translations
{
    partial class Translations
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
            this.components = new System.ComponentModel.Container();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.insertHereToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteEntryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panelTop = new System.Windows.Forms.Panel();
            this.buttonScanFiles = new System.Windows.Forms.Button();
            this.buttonReload = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.richTextBoxErrors = new System.Windows.Forms.RichTextBox();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.comboBoxSection = new System.Windows.Forms.ComboBox();
            this.dataGridView = new BaseUtils.DataGridViewBaseEnhancements();
            this.ColSource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColSection = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColFound = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColEnglish = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.contextMenuStrip.SuspendLayout();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.insertHereToolStripMenuItem,
            this.deleteEntryToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(162, 48);
            // 
            // insertHereToolStripMenuItem
            // 
            this.insertHereToolStripMenuItem.Name = "insertHereToolStripMenuItem";
            this.insertHereToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.insertHereToolStripMenuItem.Text = "Insert Entry Here";
            this.insertHereToolStripMenuItem.Click += new System.EventHandler(this.insertHereToolStripMenuItem_Click);
            // 
            // deleteEntryToolStripMenuItem
            // 
            this.deleteEntryToolStripMenuItem.Name = "deleteEntryToolStripMenuItem";
            this.deleteEntryToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.deleteEntryToolStripMenuItem.Text = "Delete Entry";
            this.deleteEntryToolStripMenuItem.Click += new System.EventHandler(this.deleteEntryToolStripMenuItem_Click);
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.comboBoxSection);
            this.panelTop.Controls.Add(this.buttonScanFiles);
            this.panelTop.Controls.Add(this.buttonReload);
            this.panelTop.Controls.Add(this.buttonSave);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1509, 40);
            this.panelTop.TabIndex = 1;
            // 
            // buttonScanFiles
            // 
            this.buttonScanFiles.Location = new System.Drawing.Point(425, 11);
            this.buttonScanFiles.Name = "buttonScanFiles";
            this.buttonScanFiles.Size = new System.Drawing.Size(75, 23);
            this.buttonScanFiles.TabIndex = 1;
            this.buttonScanFiles.Text = "Scan Files";
            this.buttonScanFiles.UseVisualStyleBackColor = true;
            this.buttonScanFiles.Click += new System.EventHandler(this.buttonScanFiles_Click);
            // 
            // buttonReload
            // 
            this.buttonReload.Location = new System.Drawing.Point(316, 11);
            this.buttonReload.Name = "buttonReload";
            this.buttonReload.Size = new System.Drawing.Size(75, 23);
            this.buttonReload.TabIndex = 0;
            this.buttonReload.Text = "Reload";
            this.buttonReload.UseVisualStyleBackColor = true;
            this.buttonReload.Click += new System.EventHandler(this.buttonReload_Click);
            // 
            // buttonSave
            // 
            this.buttonSave.Location = new System.Drawing.Point(235, 11);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(75, 23);
            this.buttonSave.TabIndex = 0;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // richTextBoxErrors
            // 
            this.richTextBoxErrors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxErrors.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxErrors.Name = "richTextBoxErrors";
            this.richTextBoxErrors.Size = new System.Drawing.Size(1509, 364);
            this.richTextBoxErrors.TabIndex = 3;
            this.richTextBoxErrors.Text = "";
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 40);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.dataGridView);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.richTextBoxErrors);
            this.splitContainer.Size = new System.Drawing.Size(1509, 871);
            this.splitContainer.SplitterDistance = 503;
            this.splitContainer.TabIndex = 4;
            // 
            // comboBoxSection
            // 
            this.comboBoxSection.FormattingEnabled = true;
            this.comboBoxSection.Location = new System.Drawing.Point(4, 11);
            this.comboBoxSection.Name = "comboBoxSection";
            this.comboBoxSection.Size = new System.Drawing.Size(209, 21);
            this.comboBoxSection.TabIndex = 2;
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView.AutoSortByColumnName = false;
            this.dataGridView.ColumnHeaderMenuStrip = null;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColSource,
            this.ColSection,
            this.ColFound,
            this.ColID,
            this.ColEnglish});
            this.dataGridView.ContextMenuStrip = this.contextMenuStrip;
            this.dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView.Location = new System.Drawing.Point(0, 0);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.RowHeaderMenuStrip = null;
            this.dataGridView.SingleRowSelect = true;
            this.dataGridView.Size = new System.Drawing.Size(1509, 503);
            this.dataGridView.TabIndex = 0;
            this.dataGridView.TopLeftHeaderMenuStrip = null;
            this.dataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_CellClick);
            this.dataGridView.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_CellEndEdit);
            // 
            // ColSource
            // 
            this.ColSource.FillWeight = 50F;
            this.ColSource.HeaderText = "Source";
            this.ColSource.MinimumWidth = 100;
            this.ColSource.Name = "ColSource";
            this.ColSource.ReadOnly = true;
            // 
            // ColSection
            // 
            this.ColSection.FillWeight = 30F;
            this.ColSection.HeaderText = "Section";
            this.ColSection.MinimumWidth = 100;
            this.ColSection.Name = "ColSection";
            this.ColSection.ReadOnly = true;
            // 
            // ColFound
            // 
            this.ColFound.FillWeight = 10F;
            this.ColFound.HeaderText = "Found";
            this.ColFound.MinimumWidth = 25;
            this.ColFound.Name = "ColFound";
            this.ColFound.ReadOnly = true;
            // 
            // ColID
            // 
            this.ColID.FillWeight = 30F;
            this.ColID.HeaderText = "ID";
            this.ColID.MinimumWidth = 100;
            this.ColID.Name = "ColID";
            this.ColID.ReadOnly = true;
            // 
            // ColEnglish
            // 
            this.ColEnglish.HeaderText = "English";
            this.ColEnglish.Name = "ColEnglish";
            // 
            // Translations
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1509, 911);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.panelTop);
            this.Location = new System.Drawing.Point(50, 50);
            this.Name = "Translations";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Translation Viewer";
            this.contextMenuStrip.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private BaseUtils.DataGridViewBaseEnhancements dataGridView;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem insertHereToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteEntryToolStripMenuItem;
        private System.Windows.Forms.Button buttonReload;
        private System.Windows.Forms.Button buttonScanFiles;
        private System.Windows.Forms.RichTextBox richTextBoxErrors;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.ComboBox comboBoxSection;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColSection;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColFound;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColID;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColEnglish;
    }
}

