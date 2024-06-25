using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BaseUtils;
using QuickJSON;

namespace ConvertToAtString
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string s= Clipboard.GetText();
            JToken t = JToken.Parse(s, JToken.ParseOptions.CheckEOL);
            if ( t != null)
            {
                if ( t.IsArray && t[0].Object().Contains("header") && t[0].Object().Contains("data"))
                {
                    s = t[0]["data"].ToString(false);
                }
            }

            richTextBox1.Text = s;
            richTextBox2.Text = "@\"" + s.Replace("\"","\"\"") + "\";";
            Clipboard.SetText(richTextBox2.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(richTextBox2.Text);
        }
    }
}
