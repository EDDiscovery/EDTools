
namespace ConvertToAtString
{
    partial class Form1
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
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.richTextBox2 = new System.Windows.Forms.RichTextBox();
            this.buttonPastConvertCopy = new System.Windows.Forms.Button();
            this.buttonCopy = new System.Windows.Forms.Button();
            this.buttonShipModule = new System.Windows.Forms.Button();
            this.checkBoxVerbose = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(12, 61);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(1100, 250);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            // 
            // richTextBox2
            // 
            this.richTextBox2.Location = new System.Drawing.Point(12, 327);
            this.richTextBox2.Name = "richTextBox2";
            this.richTextBox2.Size = new System.Drawing.Size(1100, 250);
            this.richTextBox2.TabIndex = 0;
            this.richTextBox2.Text = "";
            // 
            // buttonPastConvertCopy
            // 
            this.buttonPastConvertCopy.Location = new System.Drawing.Point(13, 13);
            this.buttonPastConvertCopy.Name = "buttonPastConvertCopy";
            this.buttonPastConvertCopy.Size = new System.Drawing.Size(211, 23);
            this.buttonPastConvertCopy.TabIndex = 1;
            this.buttonPastConvertCopy.Text = "Paste, Convert @, Copy";
            this.buttonPastConvertCopy.UseVisualStyleBackColor = true;
            this.buttonPastConvertCopy.Click += new System.EventHandler(this.buttonPasteConvertCopy_Click);
            // 
            // buttonCopy
            // 
            this.buttonCopy.Location = new System.Drawing.Point(1032, 13);
            this.buttonCopy.Name = "buttonCopy";
            this.buttonCopy.Size = new System.Drawing.Size(80, 23);
            this.buttonCopy.TabIndex = 1;
            this.buttonCopy.Text = "Copy";
            this.buttonCopy.UseVisualStyleBackColor = true;
            this.buttonCopy.Click += new System.EventHandler(this.buttonCopy_Click);
            // 
            // buttonShipModule
            // 
            this.buttonShipModule.Location = new System.Drawing.Point(622, 13);
            this.buttonShipModule.Name = "buttonShipModule";
            this.buttonShipModule.Size = new System.Drawing.Size(93, 23);
            this.buttonShipModule.TabIndex = 2;
            this.buttonShipModule.Text = "Ship Module";
            this.buttonShipModule.UseVisualStyleBackColor = true;
            this.buttonShipModule.Click += new System.EventHandler(this.buttonShipModule_Click);
            // 
            // checkBoxVerbose
            // 
            this.checkBoxVerbose.AutoSize = true;
            this.checkBoxVerbose.Location = new System.Drawing.Point(239, 17);
            this.checkBoxVerbose.Name = "checkBoxVerbose";
            this.checkBoxVerbose.Size = new System.Drawing.Size(65, 17);
            this.checkBoxVerbose.TabIndex = 3;
            this.checkBoxVerbose.Text = "Verbose";
            this.checkBoxVerbose.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1134, 617);
            this.Controls.Add(this.checkBoxVerbose);
            this.Controls.Add(this.buttonShipModule);
            this.Controls.Add(this.buttonCopy);
            this.Controls.Add(this.buttonPastConvertCopy);
            this.Controls.Add(this.richTextBox2);
            this.Controls.Add(this.richTextBox1);
            this.Name = "Form1";
            this.Text = "Convert string";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.RichTextBox richTextBox2;
        private System.Windows.Forms.Button buttonPastConvertCopy;
        private System.Windows.Forms.Button buttonCopy;
        private System.Windows.Forms.Button buttonShipModule;
        private System.Windows.Forms.CheckBox checkBoxVerbose;
    }
}

