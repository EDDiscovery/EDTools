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

namespace JournalPlayer
{
    public partial class JournalPlayerForm : Form
    {
        string datafile = "playersettings.json";
        JObject settings;
        string DestFolder { get { return settings["Dest"].Str(); } set { settings["Dest"] = value; } }
        string SourceFolder { get { return settings["Source"].Str(); } set { settings["Source"] = value; } }
        DateTime Starttime { get { return settings["Starttime"].DateTimeUTC(); } set { settings["Starttime"] = value.ToStringZulu(); } }

        FileInfo[] files;
        int fileentry;
        int lineno;
        Stream fs;
        StreamReader sr;
        string outfilename;
        Timer tme = new Timer();
        string stopon;

        public JournalPlayerForm()
        {
            InitializeComponent();

            settings = new JObject
            {
                ["Source"] = @"C:\Users\RK\Saved Games\Frontier Developments\Elite Dangerous",
                ["Dest"] = @"c:\code\logs\test",
                ["Starttime"] = "2024-01-22T14:42:01Z"
            };

            if ( File.Exists(datafile))
            {
                string text = File.ReadAllText(datafile);
                if (text != null)
                    settings = JObject.Parse(text);
            }

            textBoxDestFolder.Text = DestFolder;
            textBoxSourceFolder.Text = SourceFolder;
            dateTimePickerStartDate.Value = Starttime;

            this.textBoxSourceFolder.TextChanged += new System.EventHandler(this.textBoxSourceFolder_TextChanged);
            this.textBoxDestFolder.TextChanged += new System.EventHandler(this.textBoxDestFolder_TextChanged);
            this.dateTimePickerStartDate.ValueChanged += new System.EventHandler(this.dateTimePickerStartDate_ValueChanged);

            Clear();

            tme.Tick += Tme_Tick;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            File.WriteAllText(datafile,settings.ToString(true));
            base.OnClosed(e);
        }
        private void Tme_Tick(object sender, EventArgs e)
        {
            buttonStep_Click(null, null);
        }


        private void textBoxSourceFolder_TextChanged(object sender, EventArgs e)
        {
            Clear();
            SourceFolder = textBoxSourceFolder.Text;

        }
        private void textBoxDestFolder_TextChanged(object sender, EventArgs e)
        {
            Clear();
            DestFolder = textBoxDestFolder.Text;
        }
        private void buttonClearDestFolder_Click(object sender, EventArgs e)
        {
            BaseUtils.FileHelpers.DeleteFiles(DestFolder, "*.log", new TimeSpan(0), 0);
        }

        private void dateTimePickerStartDate_ValueChanged(object sender, EventArgs e)
        {
            Clear();
            Starttime = dateTimePickerStartDate.Value;
        }


        private void buttonStop_Click(object sender, EventArgs e)
        {
            tme.Stop();
        }

        private void button50ms_Click(object sender, EventArgs e)
        {
            tme.Stop();
            tme.Interval = 50;
            tme.Start();
        }

        private void button100ms_Click(object sender, EventArgs e)
        {
            tme.Stop();
            tme.Interval = 100;
            tme.Start();
        }

        private void button250ms_Click(object sender, EventArgs e)
        {
            tme.Stop();
            tme.Interval = 250;
            tme.Start();
        }

        private void button500ms_Click(object sender, EventArgs e)
        {
            tme.Stop();
            tme.Interval = 500;
            tme.Start();
        }
        private void button1s_Click(object sender, EventArgs e)
        {
            tme.Stop();
            tme.Interval = 1000;
            tme.Start();
        }
        private void buttonFSDJump_Click(object sender, EventArgs e)
        {
            tme.Stop();
            tme.Interval = 100;
            stopon = "FSDJump";
            tme.Start();
        }

        private void buttonStartJump_Click(object sender, EventArgs e)
        {
            tme.Stop();
            tme.Interval = 100;
            stopon = "StartJump";
            tme.Start();

        }

        private void Clear()
        {
            fileentry = -1;
            stopon = null;
            richTextBoxCurrentEntry.Clear();
            richTextBoxNextEntry.Clear();
            textBoxJournalFile.Text = "None";
            richTextBoxCurrentEntry.Text = richTextBoxNextEntry.Text = "";
            tme.Stop();
        }

        private void WriteLast()
        {
            if (richTextBoxNextEntry.Text.Length > 0)        // if we have a previous next entry
            {
                string outline = richTextBoxNextEntry.Text.Substring(richTextBoxNextEntry.Text.IndexOf(": ") + 1) + Environment.NewLine;
                BaseUtils.FileHelpers.TryAppendToFile(outfilename, outline, true);
                richTextBoxCurrentEntry.Text = richTextBoxNextEntry.Text;
            }
        }

        private void buttonStep_Click(object sender, EventArgs e)
        {
            if (fileentry == -1)
            {
                if (Directory.Exists(DestFolder))
                {
                    if (Directory.Exists(SourceFolder))
                    {
                        files = Directory.EnumerateFiles(SourceFolder, "journal*.log", SearchOption.TopDirectoryOnly)
                            .Select(f => new FileInfo(f)).Where(t => t.LastWriteTime >= Starttime).OrderBy(p => p.LastWriteTime).ToArray();

                        if (files.Length > 0)
                        {
                            fileentry = 0;
                        }
                        else
                            MessageBox.Show($"No log files found in {SourceFolder}");
                    }
                    else
                        MessageBox.Show("Source folder does not exist");
                }
                else
                    MessageBox.Show("Dest folder does not exist");
            }

            while (true)
            {
                if (fileentry >= files.Length)
                {
                    WriteLast();
                    Clear();
                    MessageBox.Show("No more log files");
                    return;
                }

                if (fs == null)
                {
                    fs = new FileStream(files[fileentry].FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    sr = new StreamReader(fs);
                    lineno = 0;
                    textBoxJournalFile.Text = files[fileentry].FullName;
                }

                string line = sr.ReadLine();
                lineno++;

                if (line == null)
                {
                    sr.Close();
                    sr = null;
                    fs.Close();
                    fs = null;
                    fileentry++;
                }
                else
                {
                    WriteLast();

                    richTextBoxNextEntry.Text = lineno.ToStringInvariant() + ": " + line;
                    outfilename = Path.Combine(DestFolder, Path.GetFileName(files[fileentry].FullName));        // set here as when we change we need to write to the last file

                    if (stopon != null)
                    {
                        JObject jo = JObject.Parse(line);
                        string eventname = jo["event"].Str();

                        if ( eventname == stopon )
                        {
                            tme.Stop();
                            stopon = null;
                        }
                    }
                    break;
                }
            }
        }

    }
}
