using QuickJSON;
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

namespace EDDStatusDisplay
{
    public partial class StatusForm : Form
    {

        public bool IsClosed = false;
        Timer tm = new Timer();
        public StatusForm()
        {
            InitializeComponent();
            tm.Interval = 200;
            tm.Tick += Tm_Tick;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            tm.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            IsClosed = true;
        }

        private void Tm_Tick(object sender, EventArgs e)
        {
            string user = Environment.GetEnvironmentVariable("USERNAME");

            string path = @"c:\users\" + user + @"\saved games\frontier developments\elite dangerous\";
            string watchfile = Path.Combine(path, "status.json");

            string laststatus = "";

            string nextstatus = null;

            Stream stream = null;
            try
            {
                stream = File.Open(watchfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                StreamReader reader = new StreamReader(stream);

                nextstatus = reader.ReadToEnd();

                stream.Close();
            }
            catch
            { }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }

            if (nextstatus != null && nextstatus != laststatus)
            {
                JToken j = JToken.Parse(nextstatus);

                if (j != null)
                {
                    SetStatus(j);
                }
            }
        }

        void SetStatus(QuickJSON.JToken json)
        {
            ulong flags = json["Flags"].ULong();

            List<Control> ctrllist = new List<Control>();
            foreach (Control c in Controls)
                ctrllist.Add(c);

            foreach (RadioButton rb in ctrllist.Where(x => x is RadioButton && x.Tag != null && ((string)x.Tag).StartsWith("F1")))
            {
                int bit = ((string)rb.Tag).Substring(3).InvariantParseInt(0);
                rb.Checked = (flags & (1UL << bit)) != 0;
            }
        }
    }

}
