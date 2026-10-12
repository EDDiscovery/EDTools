using BaseUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Translations
{
    public partial class EditForm : Form
    {
        // richtext boxes have \n as the endline, but we are always using \r\n so make sure when we compute we take into account
        public string EditText { get { return singleline ? textBox.Text : richTextBox.Text.Replace("\n","\r\n"); } set { textBox.Text=  richTextBox.Text = value;  } }

        public void SingleLineEdit() { textBox.Visible = true; richTextBox.Visible = false; singleline = true; }
        private bool singleline = false;
        public EditForm()
        {
            InitializeComponent();
            textBox.Visible = false;
        }

        private void richTextBox_TextChanged(object sender, EventArgs e)
        {
            this.Text = "ID:" + richTextBox.Text.Replace("\n", "\r\n").CalcSha8();

        }
    }
}
