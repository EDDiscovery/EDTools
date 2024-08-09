using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
                if (t.IsArray && t[0].Object().Contains("header") && t[0].Object().Contains("data"))
                {
                    s = t[0]["data"].ToString(false);
                }
                else

                    s = t.ToString(false);
            }

            richTextBox1.Text = s;
            richTextBox2.Text = "@\"" + s.Replace("\"","\"\"") + "\";";
            if (richTextBox2.Text.HasChars())
                Clipboard.SetText(richTextBox2.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(richTextBox2.Text);
        }

        private void buttonShipModule_Click(object sender, EventArgs e)
        {
            string s = Clipboard.GetText();
            string text = "";

            using (StringReader sr = new StringReader(s))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("Unknown Module"))
                    {
                        int index = line.IndexOf("{");

                        if ( index>0)
                        {
                            string res = line.Substring(index);
                            index = res.IndexOf("},");
                            if (index >= 0)
                                res = res.Substring(0, index + 2);
                            text += res + Environment.NewLine;
                        }

                    }
                }
            }

            richTextBox1.Text = s;
            richTextBox2.Text = text;
            if (richTextBox2.Text.HasChars())
                Clipboard.SetText(richTextBox2.Text);

        }
    }
}
