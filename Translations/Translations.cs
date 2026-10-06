using BaseUtils;
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

namespace Translations
{
    // c:\code\eddiscovery\eddiscovery\translations 2 francais-fr chinese-zh deutsch-de italiano-it polski-pl portugues-pt-br russian-ru spanish-es
    // deutsch-de


    public partial class Translations : Form
    {
        public Translations()
        {
            InitializeComponent();
        }

        List<BaseUtils.TranslatorMkII> translators;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            LoadFiles();
        }

        private void LoadFiles()
        {
            translators = new List<BaseUtils.TranslatorMkII>();

            CommandArgs args = new CommandArgs(Environment.CommandLine);
            args.Next();
            
            string txpath = null;
            int searchdepth = 2;
            List<string> translations = new List<string>();

            if (args.Left == 0)
            {
                txpath = Directory.GetCurrentDirectory();

                translations = Directory.EnumerateFiles(txpath, "*.tlf", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f))
                        .OrderBy(p => Path.GetFileNameWithoutExtension(p.FullName))
                        .Where(p => !Path.GetFileNameWithoutExtension(p.FullName).EqualsIIC("example-ex"))
                        .Select(y => Path.GetFileNameWithoutExtension(y.FullName)).ToList();

                if ( translations.Count == 0)
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Filter = "TLF Files|*.tlf";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txpath = Path.GetDirectoryName(ofd.FileName);
                        translations = new List<string> { Path.GetFileNameWithoutExtension(ofd.FileName) };
                    }
                    else
                        Close();
                }
            }
            else if (args.Left == 1)
            {
                txpath = Directory.GetCurrentDirectory();

                translations = new List<string> { args.Next() };
            }
            else
            {
                txpath = args.Next();
                searchdepth = args.Int();
                while (args.Left > 0)
                    translations.Add(args.Next());
            }

            BaseUtils.TranslatorMkII primary = new TranslatorMkII();
            primary.LoadTranslation("example-ex", System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, null, null, true, true);

            while ( dataGridView.Columns.Count > ColEnglish.Index + 1)
            {
                dataGridView.Columns.RemoveAt(ColEnglish.Index + 1);
            }

            if (primary.Translating)
            {
                translators.Add(primary);
                foreach( var nl in translations)
                {
                    TranslatorMkII next = new TranslatorMkII();
                    next.LoadTranslation(nl, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, null, null, true, true);
                    if (next.Translating)
                    {
                        translators.Add(next);
                        var col = new DataGridViewTextBoxColumn() { HeaderText = nl };
                        dataGridView.Columns.Add(col);
                    }
                    else
                        break;
                }
            }

            Display();

            buttonSave.Enabled = buttonReload.Enabled = false;
        }

        private void Display()
        {
            dataGridView.Rows.Clear();

            if (translators.Count > 0)
            {
                foreach (var id in translators[0].EnumerateKeys)
                {
                    if (!id.StartsWith("SOURCE:"))
                    {
                        var data = MakeCells(id);
                        int rowno = dataGridView.Rows.Add(data.ToArray());
                        dataGridView.Rows[rowno].Tag = id;
                    }
                }
                WriteHeaders();
                dataGridView.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);
            }
        }

        private void WriteHeaders()
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
                row.HeaderCell.Value = (row.Index + 1).ToStringInvariant();
        }

        private List<object> MakeCells(string id)
        {
            translators[0].TryGetEntry(id, out BaseUtils.TranslatorMkII.TranslationEntry entry);
            List<Object> data = new List<object> { Path.GetFileNameWithoutExtension(entry.File) + ":" + entry.Line.ToStringInvariant(), id, entry.English };
            for (int i = 1; i < translators.Count; i++)
            {
                string v = null;
                translators[i].TryGetValue(id, out v);
                data.Add(v ?? "");
            }
            return data;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            var primary = translators[0];

            for (int txn = 0; txn < translators.Count; txn++)
            {
                string foreignlang = translators[txn].Language;

                int rowno = 0;

                string currentfilename = null;
                List<string> filename = new List<string>();                         // filenames created
                List<StringBuilder> fileoutputs = new List<StringBuilder>();        // with stringbuilder 
                int outputfileindex = 0;

                foreach (string id in primary.EnumerateKeys)
                {
                    primary.TryGetEntry(id, out BaseUtils.TranslatorMkII.TranslationEntry entry);

                    // transmute filename to foreign name
                    if (currentfilename == null || !entry.File.EqualsIIC(currentfilename))
                    {
                        string nerfname = entry.File.Replace(primary.Language, foreignlang);

                        int alreadyexists = filename.FindIndex(x => x.EqualsIIC(nerfname));
                        if (alreadyexists >= 0)
                        {
                            outputfileindex = alreadyexists;
                            System.Diagnostics.Debug.WriteLine($"Continue with previous output file {nerfname} {outputfileindex}");
                        }
                        else
                        {
                            outputfileindex = fileoutputs.Count;
                            fileoutputs.Add(new StringBuilder());
                            nerfname = nerfname.Replace(primary.Language.Substring(0, primary.Language.IndexOf("-")), foreignlang.Substring(0, foreignlang.IndexOf("-")));
                            filename.Add(nerfname);
                            System.Diagnostics.Debug.WriteLine($"Changed to new output file {nerfname} {outputfileindex}");
                        }

                        currentfilename = entry.File;
                    }

                    if (TranslatorMkII.IsSourceID(id))
                    {
                        primary.TryGetValue(id, out string sourcetext);             // this has the translation in it, or for comments the text line. May be null if not defined

                        if (sourcetext.StartsWith("include ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            sourcetext = sourcetext.Replace(primary.Language.Substring(0, primary.Language.IndexOf("-")), foreignlang.Substring(0, foreignlang.IndexOf("-")));
                        }

                        fileoutputs[outputfileindex].Append(sourcetext);
                        fileoutputs[outputfileindex].Append(Environment.NewLine);

                        //System.Diagnostics.Debug.WriteLine($"{primary.GetOriginalFile(id)}:{primary.GetOriginalLine(id)} {txt}");
                    }
                    else
                    {
                        var row = dataGridView.Rows[rowno];
                        System.Diagnostics.Debug.Assert(row.Tag.ToString().Equals(id));

                        string orgenglish = row.Cells[2].Value.ToString();
                        string shatouse = orgenglish.CalcSha8();
                        System.Diagnostics.Debug.Assert(row.Cells[1].Value.ToString().Equals(shatouse));

                        fileoutputs[outputfileindex].Append(shatouse);     // output id, colon, primary english text
                        fileoutputs[outputfileindex].Append(": ");
                        fileoutputs[outputfileindex].Append(orgenglish.EscapeControlChars().AlwaysQuoteString());

                        string txstring = row.Cells[txn + 2].Value.ToString();

                        if (txn == 0 || txstring.IsEmpty())
                        {
                            fileoutputs[outputfileindex].Append(" @");
                        }
                        else
                        {
                            fileoutputs[outputfileindex].Append(" => ");
                            fileoutputs[outputfileindex].Append(txstring.EscapeControlChars().AlwaysQuoteString());
                        }
                        fileoutputs[outputfileindex].Append(Environment.NewLine);

                        rowno++;
                    }
                }

                //now overwrite them all
                for (int i = 0; i < fileoutputs.Count; i++)
                {
                    string contents = fileoutputs[i].ToString();
                    File.WriteAllText(filename[i], contents, Encoding.UTF8);
                }
            }

            LoadFiles();
        }

        private void buttonReload_Click(object sender, EventArgs e)
        {
            if ( MessageBox.Show("Abandon Changes","Warning",MessageBoxButtons.OKCancel,MessageBoxIcon.Warning) == DialogResult.OK )
                LoadFiles();
        }

        private void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == ColEnglish.Index)
            {
                var row = dataGridView.Rows[e.RowIndex];
                string orginalkey = row.Tag.ToString();
                string newenglish = row.Cells[2].Value.ToString();

                if (translators[0].TryGetValue(orginalkey, out string orgenglish) && orgenglish != newenglish)
                {
                    string newsha = newenglish.CalcSha8();
                    row.Cells[1].Value = newsha;

                    foreach (var tx in translators)
                    {
                        tx.ChangeEnglish(orginalkey,newenglish);
                    }

                    row.Tag = newsha;

                    buttonSave.Enabled = buttonReload.Enabled = true;
                }
            }
        }

        private void insertHereToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var row = dataGridView.ClickedRightRow;
            if (row != null)
            {
                string shatoinsertat = row.Cells[1].Value.ToString();
                string english = "Edit text!";
                while (translators[0].IsDefined(english.CalcSha8()))
                    english += "!";

                string sha = english.CalcSha8();

                foreach (var tx in translators)
                {
                    tx.Insert(shatoinsertat, english, tx == translators[0] ? english : null);
                }

                var data = MakeCells(sha);
                int rowi = row.Index;
                dataGridView.Rows.Insert(rowi, data.ToArray());
                dataGridView.Rows[rowi].Tag = sha;

                WriteHeaders();
                buttonSave.Enabled = buttonReload.Enabled = true;
            }

        }

        private void deleteEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var row = dataGridView.ClickedRightRow;
            if (row != null)
            {
                string shatoremove = row.Cells[1].Value.ToString();

                foreach (var tx in translators)
                {
                    tx.Delete(shatoremove);
                }

                dataGridView.Rows.RemoveAt(row.Index);

                WriteHeaders();
                buttonSave.Enabled = buttonReload.Enabled = true;
            }
        }

        private void dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if ( e.RowIndex == -1 && e.ColumnIndex == -1)
            {
                dataGridView.Sort(new RowComparer());
                dataGridView.ClearSelection();
            }
        }

        private class RowComparer : System.Collections.IComparer
        {
            public int Compare(object x, object y)
            {
                DataGridViewRow dataGridViewRow1 = (DataGridViewRow)x;
                DataGridViewRow dataGridViewRow2 = (DataGridViewRow)y;

                return dataGridViewRow1.HeaderCell.Value.ToString().InvariantParseInt(0) - dataGridViewRow2.HeaderCell.Value.ToString().InvariantParseInt(0);
            }
        }
    }
}
