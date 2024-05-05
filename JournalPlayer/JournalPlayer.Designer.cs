
namespace JournalPlayer
{
    partial class JournalPlayerForm
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
            this.textBoxSourceFolder = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxDestFolder = new System.Windows.Forms.TextBox();
            this.richTextBoxCurrentEntry = new System.Windows.Forms.RichTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.dateTimePickerStartDate = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.richTextBoxNextEntry = new System.Windows.Forms.RichTextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxJournalFile = new System.Windows.Forms.TextBox();
            this.buttonStep = new System.Windows.Forms.Button();
            this.buttonClearDestFolder = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.button100ms = new System.Windows.Forms.Button();
            this.button250ms = new System.Windows.Forms.Button();
            this.button500ms = new System.Windows.Forms.Button();
            this.button50ms = new System.Windows.Forms.Button();
            this.button1s = new System.Windows.Forms.Button();
            this.buttonFSDJump = new System.Windows.Forms.Button();
            this.buttonStartJump = new System.Windows.Forms.Button();
            this.textBoxOutputFile = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.buttonSelectSourceFolder = new System.Windows.Forms.Button();
            this.buttonSelectDestFolder = new System.Windows.Forms.Button();
            this.checkBoxUseCurrentTime = new System.Windows.Forms.CheckBox();
            this.textBoxPattern = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.dateTimePickerEndDate = new System.Windows.Forms.DateTimePicker();
            this.textBoxGotoLineNo = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBoxSourceFolder
            // 
            this.textBoxSourceFolder.Location = new System.Drawing.Point(111, 30);
            this.textBoxSourceFolder.Name = "textBoxSourceFolder";
            this.textBoxSourceFolder.Size = new System.Drawing.Size(625, 20);
            this.textBoxSourceFolder.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 147);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(92, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Destination Folder";
            // 
            // textBoxDestFolder
            // 
            this.textBoxDestFolder.Location = new System.Drawing.Point(111, 144);
            this.textBoxDestFolder.Name = "textBoxDestFolder";
            this.textBoxDestFolder.Size = new System.Drawing.Size(625, 20);
            this.textBoxDestFolder.TabIndex = 2;
            // 
            // richTextBoxCurrentEntry
            // 
            this.richTextBoxCurrentEntry.Location = new System.Drawing.Point(111, 567);
            this.richTextBoxCurrentEntry.Name = "richTextBoxCurrentEntry";
            this.richTextBoxCurrentEntry.Size = new System.Drawing.Size(781, 200);
            this.richTextBoxCurrentEntry.TabIndex = 6;
            this.richTextBoxCurrentEntry.Text = "";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 570);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(105, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Current Journal Entry";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 33);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(73, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Source Folder";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(4, 75);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(56, 13);
            this.label5.TabIndex = 0;
            this.label5.Text = "From Time";
            // 
            // dateTimePickerStartDate
            // 
            this.dateTimePickerStartDate.CustomFormat = "dd/MM/yyyy  hh:mm:ss";
            this.dateTimePickerStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerStartDate.Location = new System.Drawing.Point(111, 69);
            this.dateTimePickerStartDate.Name = "dateTimePickerStartDate";
            this.dateTimePickerStartDate.Size = new System.Drawing.Size(202, 20);
            this.dateTimePickerStartDate.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 347);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Next Journal Entry";
            // 
            // richTextBoxNextEntry
            // 
            this.richTextBoxNextEntry.Location = new System.Drawing.Point(111, 344);
            this.richTextBoxNextEntry.Name = "richTextBoxNextEntry";
            this.richTextBoxNextEntry.Size = new System.Drawing.Size(781, 200);
            this.richTextBoxNextEntry.TabIndex = 5;
            this.richTextBoxNextEntry.Text = "";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(4, 260);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(56, 13);
            this.label6.TabIndex = 0;
            this.label6.Text = "Go to Line";
            // 
            // textBoxJournalFile
            // 
            this.textBoxJournalFile.Location = new System.Drawing.Point(111, 187);
            this.textBoxJournalFile.Name = "textBoxJournalFile";
            this.textBoxJournalFile.Size = new System.Drawing.Size(687, 20);
            this.textBoxJournalFile.TabIndex = 4;
            // 
            // buttonStep
            // 
            this.buttonStep.Location = new System.Drawing.Point(7, 228);
            this.buttonStep.Name = "buttonStep";
            this.buttonStep.Size = new System.Drawing.Size(75, 23);
            this.buttonStep.TabIndex = 3;
            this.buttonStep.Text = "Step";
            this.buttonStep.UseVisualStyleBackColor = true;
            this.buttonStep.Click += new System.EventHandler(this.buttonStep_Click);
            // 
            // buttonClearDestFolder
            // 
            this.buttonClearDestFolder.Location = new System.Drawing.Point(823, 141);
            this.buttonClearDestFolder.Name = "buttonClearDestFolder";
            this.buttonClearDestFolder.Size = new System.Drawing.Size(75, 23);
            this.buttonClearDestFolder.TabIndex = 7;
            this.buttonClearDestFolder.Text = "Clear Folder";
            this.buttonClearDestFolder.UseVisualStyleBackColor = true;
            this.buttonClearDestFolder.Click += new System.EventHandler(this.buttonClearDestFolder_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(111, 228);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 3;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // button100ms
            // 
            this.button100ms.Location = new System.Drawing.Point(273, 228);
            this.button100ms.Name = "button100ms";
            this.button100ms.Size = new System.Drawing.Size(75, 23);
            this.button100ms.TabIndex = 3;
            this.button100ms.Text = "100ms";
            this.button100ms.UseVisualStyleBackColor = true;
            this.button100ms.Click += new System.EventHandler(this.button100ms_Click);
            // 
            // button250ms
            // 
            this.button250ms.Location = new System.Drawing.Point(354, 228);
            this.button250ms.Name = "button250ms";
            this.button250ms.Size = new System.Drawing.Size(75, 23);
            this.button250ms.TabIndex = 3;
            this.button250ms.Text = "250ms";
            this.button250ms.UseVisualStyleBackColor = true;
            this.button250ms.Click += new System.EventHandler(this.button250ms_Click);
            // 
            // button500ms
            // 
            this.button500ms.Location = new System.Drawing.Point(435, 228);
            this.button500ms.Name = "button500ms";
            this.button500ms.Size = new System.Drawing.Size(75, 23);
            this.button500ms.TabIndex = 3;
            this.button500ms.Text = "500ms";
            this.button500ms.UseVisualStyleBackColor = true;
            this.button500ms.Click += new System.EventHandler(this.button500ms_Click);
            // 
            // button50ms
            // 
            this.button50ms.Location = new System.Drawing.Point(192, 228);
            this.button50ms.Name = "button50ms";
            this.button50ms.Size = new System.Drawing.Size(75, 23);
            this.button50ms.TabIndex = 3;
            this.button50ms.Text = "50ms";
            this.button50ms.UseVisualStyleBackColor = true;
            this.button50ms.Click += new System.EventHandler(this.button50ms_Click);
            // 
            // button1s
            // 
            this.button1s.Location = new System.Drawing.Point(516, 228);
            this.button1s.Name = "button1s";
            this.button1s.Size = new System.Drawing.Size(75, 23);
            this.button1s.TabIndex = 3;
            this.button1s.Text = "1s";
            this.button1s.UseVisualStyleBackColor = true;
            this.button1s.Click += new System.EventHandler(this.button1s_Click);
            // 
            // buttonFSDJump
            // 
            this.buttonFSDJump.Location = new System.Drawing.Point(597, 228);
            this.buttonFSDJump.Name = "buttonFSDJump";
            this.buttonFSDJump.Size = new System.Drawing.Size(90, 23);
            this.buttonFSDJump.TabIndex = 3;
            this.buttonFSDJump.Text = ">>FSDJump";
            this.buttonFSDJump.UseVisualStyleBackColor = true;
            this.buttonFSDJump.Click += new System.EventHandler(this.buttonFSDJump_Click);
            // 
            // buttonStartJump
            // 
            this.buttonStartJump.Location = new System.Drawing.Point(695, 228);
            this.buttonStartJump.Name = "buttonStartJump";
            this.buttonStartJump.Size = new System.Drawing.Size(90, 23);
            this.buttonStartJump.TabIndex = 3;
            this.buttonStartJump.Text = ">>StartJump";
            this.buttonStartJump.UseVisualStyleBackColor = true;
            this.buttonStartJump.Click += new System.EventHandler(this.buttonStartJump_Click);
            // 
            // textBoxOutputFile
            // 
            this.textBoxOutputFile.Location = new System.Drawing.Point(111, 298);
            this.textBoxOutputFile.Name = "textBoxOutputFile";
            this.textBoxOutputFile.Size = new System.Drawing.Size(625, 20);
            this.textBoxOutputFile.TabIndex = 4;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(4, 301);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(58, 13);
            this.label7.TabIndex = 0;
            this.label7.Text = "Output File";
            // 
            // buttonSelectSourceFolder
            // 
            this.buttonSelectSourceFolder.Location = new System.Drawing.Point(742, 28);
            this.buttonSelectSourceFolder.Name = "buttonSelectSourceFolder";
            this.buttonSelectSourceFolder.Size = new System.Drawing.Size(75, 23);
            this.buttonSelectSourceFolder.TabIndex = 7;
            this.buttonSelectSourceFolder.Text = "Browse";
            this.buttonSelectSourceFolder.UseVisualStyleBackColor = true;
            this.buttonSelectSourceFolder.Click += new System.EventHandler(this.buttonSelectSourceFolder_Click);
            // 
            // buttonSelectDestFolder
            // 
            this.buttonSelectDestFolder.Location = new System.Drawing.Point(742, 141);
            this.buttonSelectDestFolder.Name = "buttonSelectDestFolder";
            this.buttonSelectDestFolder.Size = new System.Drawing.Size(75, 23);
            this.buttonSelectDestFolder.TabIndex = 7;
            this.buttonSelectDestFolder.Text = "Browse";
            this.buttonSelectDestFolder.UseVisualStyleBackColor = true;
            this.buttonSelectDestFolder.Click += new System.EventHandler(this.buttonSelectDestFolder_Click);
            // 
            // checkBoxUseCurrentTime
            // 
            this.checkBoxUseCurrentTime.AutoSize = true;
            this.checkBoxUseCurrentTime.Location = new System.Drawing.Point(755, 300);
            this.checkBoxUseCurrentTime.Name = "checkBoxUseCurrentTime";
            this.checkBoxUseCurrentTime.Size = new System.Drawing.Size(104, 17);
            this.checkBoxUseCurrentTime.TabIndex = 8;
            this.checkBoxUseCurrentTime.Text = "Use Current time";
            this.checkBoxUseCurrentTime.UseVisualStyleBackColor = true;
            // 
            // textBoxPattern
            // 
            this.textBoxPattern.Location = new System.Drawing.Point(111, 105);
            this.textBoxPattern.Name = "textBoxPattern";
            this.textBoxPattern.Size = new System.Drawing.Size(181, 20);
            this.textBoxPattern.TabIndex = 0;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(351, 72);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(20, 13);
            this.label8.TabIndex = 0;
            this.label8.Text = "To";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(4, 108);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(77, 13);
            this.label9.TabIndex = 0;
            this.label9.Text = "Log File Match";
            // 
            // dateTimePickerEndDate
            // 
            this.dateTimePickerEndDate.CustomFormat = "dd/MM/yyyy  hh:mm:ss";
            this.dateTimePickerEndDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerEndDate.Location = new System.Drawing.Point(389, 69);
            this.dateTimePickerEndDate.Name = "dateTimePickerEndDate";
            this.dateTimePickerEndDate.Size = new System.Drawing.Size(202, 20);
            this.dateTimePickerEndDate.TabIndex = 1;
            // 
            // textBoxGotoLineNo
            // 
            this.textBoxGotoLineNo.Location = new System.Drawing.Point(111, 257);
            this.textBoxGotoLineNo.Name = "textBoxGotoLineNo";
            this.textBoxGotoLineNo.Size = new System.Drawing.Size(103, 20);
            this.textBoxGotoLineNo.TabIndex = 0;
            // 
            // JournalPlayerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(918, 800);
            this.Controls.Add(this.checkBoxUseCurrentTime);
            this.Controls.Add(this.buttonSelectDestFolder);
            this.Controls.Add(this.buttonSelectSourceFolder);
            this.Controls.Add(this.buttonClearDestFolder);
            this.Controls.Add(this.buttonStartJump);
            this.Controls.Add(this.buttonFSDJump);
            this.Controls.Add(this.button1s);
            this.Controls.Add(this.button500ms);
            this.Controls.Add(this.button250ms);
            this.Controls.Add(this.button50ms);
            this.Controls.Add(this.button100ms);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonStep);
            this.Controls.Add(this.dateTimePickerEndDate);
            this.Controls.Add(this.dateTimePickerStartDate);
            this.Controls.Add(this.richTextBoxNextEntry);
            this.Controls.Add(this.richTextBoxCurrentEntry);
            this.Controls.Add(this.textBoxOutputFile);
            this.Controls.Add(this.textBoxJournalFile);
            this.Controls.Add(this.textBoxDestFolder);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxGotoLineNo);
            this.Controls.Add(this.textBoxPattern);
            this.Controls.Add(this.textBoxSourceFolder);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label5);
            this.Name = "JournalPlayerForm";
            this.Text = "Journal Player";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxSourceFolder;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxDestFolder;
        private System.Windows.Forms.RichTextBox richTextBoxCurrentEntry;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.DateTimePicker dateTimePickerStartDate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox richTextBoxNextEntry;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxJournalFile;
        private System.Windows.Forms.Button buttonStep;
        private System.Windows.Forms.Button buttonClearDestFolder;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button button100ms;
        private System.Windows.Forms.Button button250ms;
        private System.Windows.Forms.Button button500ms;
        private System.Windows.Forms.Button button50ms;
        private System.Windows.Forms.Button button1s;
        private System.Windows.Forms.Button buttonFSDJump;
        private System.Windows.Forms.Button buttonStartJump;
        private System.Windows.Forms.TextBox textBoxOutputFile;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button buttonSelectSourceFolder;
        private System.Windows.Forms.Button buttonSelectDestFolder;
        private System.Windows.Forms.CheckBox checkBoxUseCurrentTime;
        private System.Windows.Forms.TextBox textBoxPattern;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.DateTimePicker dateTimePickerEndDate;
        private System.Windows.Forms.TextBox textBoxGotoLineNo;
    }
}