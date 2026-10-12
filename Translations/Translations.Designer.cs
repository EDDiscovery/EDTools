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
            this.editEntryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.insertEntryAboveToolstripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.insertEntryBelowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeSectionNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteEntryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.trimTextForSpacesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeBracketsAndSpacesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeAllFieldBuilderControlsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findNextMissingTranslationFromScanToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findNextPunctationMarkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panelTop = new System.Windows.Forms.Panel();
            this.checkBoxEnglishOnly = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxFind = new System.Windows.Forms.TextBox();
            this.textBoxFilter = new System.Windows.Forms.TextBox();
            this.comboBoxSection = new System.Windows.Forms.ComboBox();
            this.buttonNextPunct = new System.Windows.Forms.Button();
            this.buttonNextMissing = new System.Windows.Forms.Button();
            this.buttonScanFiles = new System.Windows.Forms.Button();
            this.buttonReload = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.richTextBoxErrors = new System.Windows.Forms.RichTextBox();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.dataGridView = new BaseUtils.DataGridViewColumnControl();
            this.ColSource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColSection = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColFound = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColEnglish = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.buttonPatch = new System.Windows.Forms.Button();
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
            this.editEntryToolStripMenuItem,
            this.insertEntryAboveToolstripMenuItem,
            this.insertEntryBelowToolStripMenuItem,
            this.changeSectionNameToolStripMenuItem,
            this.deleteEntryToolStripMenuItem,
            this.trimTextForSpacesToolStripMenuItem,
            this.removeBracketsAndSpacesToolStripMenuItem,
            this.removeAllFieldBuilderControlsToolStripMenuItem,
            this.findNextMissingTranslationFromScanToolStripMenuItem,
            this.findNextPunctationMarkToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(282, 224);
            this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
            // 
            // editEntryToolStripMenuItem
            // 
            this.editEntryToolStripMenuItem.Name = "editEntryToolStripMenuItem";
            this.editEntryToolStripMenuItem.Size = new System.Drawing.Size(281, 22);
            this.editEntryToolStripMenuItem.Text = "Edit Entry";
            this.editEntryToolStripMenuItem.Click += new System.EventHandler(this.editEntryToolStripMenuItem_Click);
            // 
            // insertEntryAboveToolstripMenuItem
            // 
            this.insertEntryAboveToolstripMenuItem.Name = "insertEntryAboveToolstripMenuItem";
            this.insertEntryAboveToolstripMenuItem.Size = new System.Drawing.Size(281, 22);
            this.insertEntryAboveToolstripMenuItem.Text = "Insert Entry Above";
            this.insertEntryAboveToolstripMenuItem.Click += new System.EventHandler(this.insertEntryAboveToolStripMenuItem_Click);
            // 
            // insertEntryBelowToolStripMenuItem
            // 
            this.insertEntryBelowToolStripMenuItem.Name = "insertEntryBelowToolStripMenuItem";
            this.insertEntryBelowToolStripMenuItem.Size = new System.Drawing.Size(281, 22);
            this.insertEntryBelowToolStripMenuItem.Text = "Insert Entry Below";
            this.insertEntryBelowToolStripMenuItem.Click += new System.EventHandler(this.insertEntryBelowToolStripMenuItem_Click);
            // 
            // changeSectionNameToolStripMenuItem
            // 
            this.changeSectionNameToolStripMenuItem.Name = "changeSectionNameToolStripMenuItem";
            this.changeSectionNameToolStripMenuItem.Size = new System.Drawing.Size(281, 22);
            this.changeSectionNameToolStripMenuItem.Text = "Change Section Name here";
            this.changeSectionNameToolStripMenuItem.Click += new System.EventHandler(this.changeSectionNameStripMenuItem_Click);
            // 
            // deleteEntryToolStripMenuItem
            // 
            this.deleteEntryToolStripMenuItem.Name = "deleteEntryToolStripMenuItem";
            this.deleteEntryToolStripMenuItem.Size = new System.Drawing.Size(281, 22);
            this.deleteEntryToolStripMenuItem.Text = "Delete Entry";
            this.deleteEntryToolStripMenuItem.Click += new System.EventHandler(this.deleteEntryToolStripMenuItem_Click);
            // 
            // trimTextForSpacesToolStripMenuItem
            // 
            this.trimTextForSpacesToolStripMenuItem.Name = "trimTextForSpacesToolStripMenuItem";
            this.trimTextForSpacesToolStripMenuItem.Size = new System.Drawing.Size(281, 22);
            this.trimTextForSpacesToolStripMenuItem.Text = "Trim Text for spaces and punctuation";
            this.trimTextForSpacesToolStripMenuItem.Click += new System.EventHandler(this.trimTextForSpacesToolStripMenuItem_Click);
            // 
            // removeBracketsAndSpacesToolStripMenuItem
            // 
            this.removeBracketsAndSpacesToolStripMenuItem.Name = "removeBracketsAndSpacesToolStripMenuItem";
            this.removeBracketsAndSpacesToolStripMenuItem.Size = new System.Drawing.Size(281, 22);
            this.removeBracketsAndSpacesToolStripMenuItem.Text = "Remove () <> and spaces";
            this.removeBracketsAndSpacesToolStripMenuItem.Click += new System.EventHandler(this.removeBracketsAndSpacesToolStripMenuItem_Click);
            // 
            // removeAllFieldBuilderControlsToolStripMenuItem
            // 
            this.removeAllFieldBuilderControlsToolStripMenuItem.Name = "removeAllFieldBuilderControlsToolStripMenuItem";
            this.removeAllFieldBuilderControlsToolStripMenuItem.Size = new System.Drawing.Size(281, 22);
            this.removeAllFieldBuilderControlsToolStripMenuItem.Text = "Remove all Field Builder controls";
            this.removeAllFieldBuilderControlsToolStripMenuItem.Click += new System.EventHandler(this.removeAllFieldBuilderControlsToolStripMenuItem_Click);
            // 
            // findNextMissingTranslationFromScanToolStripMenuItem
            // 
            this.findNextMissingTranslationFromScanToolStripMenuItem.Name = "findNextMissingTranslationFromScanToolStripMenuItem";
            this.findNextMissingTranslationFromScanToolStripMenuItem.Size = new System.Drawing.Size(281, 22);
            this.findNextMissingTranslationFromScanToolStripMenuItem.Text = "Find next missing translation from scan";
            this.findNextMissingTranslationFromScanToolStripMenuItem.Click += new System.EventHandler(this.findNextMissingTranslationFromScanToolStripMenuItem_Click);
            // 
            // findNextPunctationMarkToolStripMenuItem
            // 
            this.findNextPunctationMarkToolStripMenuItem.Name = "findNextPunctationMarkToolStripMenuItem";
            this.findNextPunctationMarkToolStripMenuItem.Size = new System.Drawing.Size(281, 22);
            this.findNextPunctationMarkToolStripMenuItem.Text = "Find next punctation mark";
            this.findNextPunctationMarkToolStripMenuItem.Click += new System.EventHandler(this.findNextPunctationMarkToolStripMenuItem_Click);
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.buttonPatch);
            this.panelTop.Controls.Add(this.checkBoxEnglishOnly);
            this.panelTop.Controls.Add(this.label3);
            this.panelTop.Controls.Add(this.label2);
            this.panelTop.Controls.Add(this.label1);
            this.panelTop.Controls.Add(this.textBoxFind);
            this.panelTop.Controls.Add(this.textBoxFilter);
            this.panelTop.Controls.Add(this.comboBoxSection);
            this.panelTop.Controls.Add(this.buttonNextPunct);
            this.panelTop.Controls.Add(this.buttonNextMissing);
            this.panelTop.Controls.Add(this.buttonScanFiles);
            this.panelTop.Controls.Add(this.buttonReload);
            this.panelTop.Controls.Add(this.buttonSave);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1835, 40);
            this.panelTop.TabIndex = 1;
            // 
            // checkBoxEnglishOnly
            // 
            this.checkBoxEnglishOnly.AutoSize = true;
            this.checkBoxEnglishOnly.Location = new System.Drawing.Point(728, 14);
            this.checkBoxEnglishOnly.Name = "checkBoxEnglishOnly";
            this.checkBoxEnglishOnly.Size = new System.Drawing.Size(69, 17);
            this.checkBoxEnglishOnly.TabIndex = 5;
            this.checkBoxEnglishOnly.Text = "Eng Only";
            this.checkBoxEnglishOnly.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(491, 14);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Find:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(253, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Filter:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Sec:";
            // 
            // textBoxFind
            // 
            this.textBoxFind.Location = new System.Drawing.Point(527, 11);
            this.textBoxFind.Name = "textBoxFind";
            this.textBoxFind.Size = new System.Drawing.Size(194, 20);
            this.textBoxFind.TabIndex = 3;
            this.textBoxFind.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBoxFind_KeyUp);
            // 
            // textBoxFilter
            // 
            this.textBoxFilter.Location = new System.Drawing.Point(291, 11);
            this.textBoxFilter.Name = "textBoxFilter";
            this.textBoxFilter.Size = new System.Drawing.Size(194, 20);
            this.textBoxFilter.TabIndex = 3;
            this.textBoxFilter.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBoxFilter_KeyUp);
            // 
            // comboBoxSection
            // 
            this.comboBoxSection.DropDownHeight = 800;
            this.comboBoxSection.FormattingEnabled = true;
            this.comboBoxSection.IntegralHeight = false;
            this.comboBoxSection.Location = new System.Drawing.Point(38, 9);
            this.comboBoxSection.Name = "comboBoxSection";
            this.comboBoxSection.Size = new System.Drawing.Size(209, 21);
            this.comboBoxSection.TabIndex = 2;
            // 
            // buttonNextPunct
            // 
            this.buttonNextPunct.Location = new System.Drawing.Point(991, 12);
            this.buttonNextPunct.Name = "buttonNextPunct";
            this.buttonNextPunct.Size = new System.Drawing.Size(75, 23);
            this.buttonNextPunct.TabIndex = 1;
            this.buttonNextPunct.Text = "Next Punct";
            this.buttonNextPunct.UseVisualStyleBackColor = true;
            this.buttonNextPunct.Click += new System.EventHandler(this.buttonNextPunct_Click);
            // 
            // buttonNextMissing
            // 
            this.buttonNextMissing.Location = new System.Drawing.Point(1171, 12);
            this.buttonNextMissing.Name = "buttonNextMissing";
            this.buttonNextMissing.Size = new System.Drawing.Size(75, 23);
            this.buttonNextMissing.TabIndex = 1;
            this.buttonNextMissing.Text = "Next Missing";
            this.buttonNextMissing.UseVisualStyleBackColor = true;
            this.buttonNextMissing.Click += new System.EventHandler(this.buttonNextMissing_Click);
            // 
            // buttonScanFiles
            // 
            this.buttonScanFiles.Location = new System.Drawing.Point(1090, 11);
            this.buttonScanFiles.Name = "buttonScanFiles";
            this.buttonScanFiles.Size = new System.Drawing.Size(75, 23);
            this.buttonScanFiles.TabIndex = 1;
            this.buttonScanFiles.Text = "Scan Files";
            this.buttonScanFiles.UseVisualStyleBackColor = true;
            this.buttonScanFiles.Click += new System.EventHandler(this.buttonScanFiles_Click);
            // 
            // buttonReload
            // 
            this.buttonReload.Location = new System.Drawing.Point(896, 11);
            this.buttonReload.Name = "buttonReload";
            this.buttonReload.Size = new System.Drawing.Size(75, 23);
            this.buttonReload.TabIndex = 0;
            this.buttonReload.Text = "Reload";
            this.buttonReload.UseVisualStyleBackColor = true;
            this.buttonReload.Click += new System.EventHandler(this.buttonReload_Click);
            // 
            // buttonSave
            // 
            this.buttonSave.Location = new System.Drawing.Point(815, 11);
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
            this.richTextBoxErrors.Size = new System.Drawing.Size(1835, 404);
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
            this.splitContainer.Size = new System.Drawing.Size(1835, 964);
            this.splitContainer.SplitterDistance = 556;
            this.splitContainer.TabIndex = 4;
            // 
            // dataGridView
            // 
            this.dataGridView.AllowRowHeaderVisibleSelection = false;
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dataGridView.AutoSortByColumnName = false;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.ColumnReorder = true;
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
            this.dataGridView.PerColumnWordWrapControl = true;
            this.dataGridView.RowHeaderMenuStrip = null;
            this.dataGridView.RowHeadersWidth = 70;
            this.dataGridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dataGridView.SingleRowSelect = true;
            this.dataGridView.Size = new System.Drawing.Size(1835, 556);
            this.dataGridView.TabIndex = 0;
            this.dataGridView.TopLeftHeaderMenuStrip = null;
            this.dataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_CellClick);
            this.dataGridView.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_CellContentDoubleClick);
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
            // buttonPatch
            // 
            this.buttonPatch.Location = new System.Drawing.Point(1294, 11);
            this.buttonPatch.Name = "buttonPatch";
            this.buttonPatch.Size = new System.Drawing.Size(75, 23);
            this.buttonPatch.TabIndex = 6;
            this.buttonPatch.Text = "Patch";
            this.buttonPatch.UseVisualStyleBackColor = true;
            this.buttonPatch.Click += new System.EventHandler(this.buttonPatch_Click);
            // 
            // Translations
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1835, 1004);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.panelTop);
            this.Location = new System.Drawing.Point(50, 50);
            this.Name = "Translations";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Translation Viewer";
            this.contextMenuStrip.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private BaseUtils.DataGridViewColumnControl dataGridView;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem insertEntryAboveToolstripMenuItem;
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
        private System.Windows.Forms.TextBox textBoxFilter;
        private System.Windows.Forms.ToolStripMenuItem insertEntryBelowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem trimTextForSpacesToolStripMenuItem;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolStripMenuItem removeBracketsAndSpacesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeAllFieldBuilderControlsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem findNextMissingTranslationFromScanToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem findNextPunctationMarkToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changeSectionNameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editEntryToolStripMenuItem;
        private System.Windows.Forms.Button buttonNextPunct;
        private System.Windows.Forms.Button buttonNextMissing;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxFind;
        private System.Windows.Forms.CheckBox checkBoxEnglishOnly;
        private System.Windows.Forms.Button buttonPatch;
    }
}

