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
            splitContainer.Panel2Collapsed = true;
            ColFound.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView.AllowRowHeaderVisibleSelection = true;
            buttonNextMissing.Enabled = false;
        }

        List<BaseUtils.TranslatorMkII> translators;
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadFiles();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        private void LoadFiles()
        {
            Cursor = Cursors.WaitCursor;
            SetDirty(false);
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

                if (args.Peek.EqualsIIC("None"))
                    translations = new List<string>();
                else
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
                primary.TryGetValue("336EE34F690338", out string text);

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

                SetDirty(false);

                // all entries must be present in all translators and not be in the wrong section for it to be clean
                foreach (string id in translators[0].EnumerateIDs)
                {
                    translators[0].TryGetEntry(id, out var primaryentry);

                    for (int tx = 1; tx < translations.Count; tx++)
                    {
                        if (translators[tx].TryGetEntry(id,out var entry))
                        {
                            if ( entry.Section != primaryentry.Section)
                            {
                                SetDirty();
                                break;
                            }
                        }
                        else
                        {
                            SetDirty();
                            break;
                        }
                    }
                }
                        

                comboBoxSection.Items.Add("All");
                var files = translators[0].FileNames.ToArray();
                comboBoxSection.Items.AddRange(files);
                var sections = translators[0].Sections.ToArray();
                Array.Sort(sections);
                comboBoxSection.Items.AddRange(sections);
                comboBoxSection.SelectedItem = "All";
                //comboBoxSection.SelectedItem = "KeyForm";
                comboBoxSection.SelectedIndexChanged += ComboBoxSection_SelectedIndexChanged;

            }

            Display();

            Cursor = Cursors.Default;
        }

        private void Display()
        {
            Cursor = Cursors.WaitCursor;
            dataGridView.Rows.Clear();

            dataGridView.SuspendLayout();
            if (translators.Count > 0)
            {
                foreach (var id in translators[0].EnumerateIDs)
                {
                    if (!id.StartsWith("SOURCE:"))
                    {
                        translators[0].TryGetEntry(id, out TranslatorMkII.Entry entry);
                        var data = MakeCells(id);
                        int rowno = dataGridView.Rows.Add(data.ToArray());
                        dataGridView.Rows[rowno].Tag = id;
                        dataGridView.Rows[rowno].HeaderCell.Value = (rowno+1).ToStringInvariant();
                        HighlightPunctuation(rowno);
                    }
                }

                Filter();
            }

            dataGridView.ResumeLayout();
            Cursor = Cursors.Default;
        }
        private List<object> MakeCells(string id)
        {
            translators[0].TryGetEntry(id, out BaseUtils.TranslatorMkII.Entry entry);
            System.Diagnostics.Debug.Assert(entry != null && entry.File != null && entry.Section != null && entry.English != null);
            List<Object> data = new List<object> { Path.GetFileName(entry.File) + ":" + entry.Line.ToStringInvariant(), 
                                                entry.Section,
                                                "",
                                                id, entry.English.EscapeControlChars() };       // display escaped
            for (int i = 1; i < translators.Count; i++)
            {
                string v = null;
                translators[i].TryGetValue(id, out v);
                data.Add((v ?? "").EscapeControlChars());   // display escaped
            }
            return data;
        }

        #region ToolBar Main

        private void ComboBoxSection_SelectedIndexChanged(object sender, EventArgs e)
        {
            Filter();
        }

        private void textBoxFilter_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                Filter();
        }

        private void textBoxFind_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FindNext(textBoxFind.Text);
            }
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

                foreach (string id in primary.EnumerateIDs)
                {
                    primary.TryGetEntry(id, out BaseUtils.TranslatorMkII.Entry entry);

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

                        string orgenglish = row.Cells[ColEnglish.Index].Value.ToString().ReplaceEscapeControlChars();
                        string shatouse = orgenglish.CalcSha8();
                        System.Diagnostics.Debug.Assert(row.Cells[ColID.Index].Value.ToString().Equals(shatouse));

                        fileoutputs[outputfileindex].Append(shatouse);     // output id, colon, primary english text
                        fileoutputs[outputfileindex].Append(": ");
                        fileoutputs[outputfileindex].Append(orgenglish.EscapeControlChars().AlwaysQuoteString());

                        string txstring = row.Cells[txn + ColEnglish.Index].Value.ToString().ReplaceEscapeControlChars();

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

            SetDirty(false);
        }

        private void buttonReload_Click(object sender, EventArgs e)
        {
            if (dirty == false || MessageBox.Show("Abandon Changes", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                var rowno = dataGridView.CurrentCell?.RowIndex;

                LoadFiles();

                if (rowno!=null)
                {
                    dataGridView.DisplayRow(rowno.Value, true);
                }
            }
        }

        private void buttonNextPunct_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentCell != null)
                NextPunc(dataGridView.CurrentCell.RowIndex + 1);
        }

        private void buttonNextMissing_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentCell != null)
                NextMissing(dataGridView.CurrentCell.RowIndex + 1);
        }
        #endregion

        #region Right Clicks

        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            int[] rowset = dataGridView.SelectedRows(true, true);
            bool vs = dataGridView.ClickedRightRow != null;
            bool vmult = rowset.Length > 0;
            editEntryToolStripMenuItem.Enabled = vs && dataGridView.HitColumn >= ColEnglish.Index;
            deleteEntryToolStripMenuItem.Enabled = vs;
            trimTextForSpacesToolStripMenuItem.Enabled = removeAllFieldBuilderControlsToolStripMenuItem.Enabled = vmult;
            removeAllFieldBuilderControlsToolStripMenuItem.Enabled = vmult;
            removeBracketsAndSpacesToolStripMenuItem.Enabled = vmult;
            insertEntryAboveToolstripMenuItem.Enabled = insertEntryBelowToolStripMenuItem.Enabled = vs;
            changeSectionNameToolStripMenuItem.Enabled = vs;
            findNextMissingTranslationFromScanToolStripMenuItem.Enabled = findNextPunctationMarkToolStripMenuItem.Enabled = vs;
        }

        private void editEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditInBox(dataGridView.RightClickRow, dataGridView.HitColumn);
        }

        private void deleteEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var row = dataGridView.ClickedRightRow;
            if (row != null)
            {
                string shatoremove = row.Cells[ColID.Index].Value.ToString();

                foreach (var tx in translators)
                {
                    tx.Delete(shatoremove);
                }

                dataGridView.Rows.RemoveAt(row.Index);

                WriteHeaders();
                SetDirty();
            }
        }

        private void trimTextForSpacesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Trim((s) => s.Trim().ReplaceIfStartsWith("- ", "").ReplaceIfStartsWith(",", "").ReplaceIfStartsWith(";", "").Replace("  "," ")
                            .ReplaceIfEndsWith(",", "").ReplaceIfEndsWith(".", "").ReplaceIfEndsWith(":","").Trim());
        }
        private void removeBracketsAndSpacesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Trim((s) => s.Trim().ReplaceIfStartsWith("(", "").ReplaceIfEndsWith(")", "").ReplaceIfStartsWith("<", "").ReplaceIfEndsWith(">", ""));
        }

        private void removeAllFieldBuilderControlsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Trim((s) =>
            {
                s = s.Trim().ReplaceIfStartsWith("<", "").ReplaceIfStartsWith(";", "");

                s = s.Trim();
                int si = s.LastIndexOf("{");
                if (si > 0)
                    s = s.Substring(0, si).Trim();


                si = s.LastIndexOf(";");
                if (si > 0)
                    s = s.Substring(0, si).Trim();

                s = s.ReplaceIfEndsWith(":", "").Trim();
                s = s.ReplaceIfEndsWith(",", "").Trim();
                return s;
            });

        }
        private void Trim(Func<string,string> actionon)
        {
            int[] rowset = dataGridView.SelectedRows(true, true);
            bool changedsomething = false;

            foreach ( int rowno in rowset)
            {
                DataGridViewRow row = dataGridView.Rows[rowno];
                string originalkey = row.Tag.ToString();
                string originalenglish = row.Cells[ColEnglish.Index].Value.ToString().ReplaceEscapeControlChars().ReplaceEscapeControlChars();      // go back to normal
                string newenglish = actionon(originalenglish);
                string newsha = newenglish.CalcSha8();


                // change the foreign bits
                for (int i = ColEnglish.Index+1; i < dataGridView.ColumnCount; i++)
                {
                    string nt = actionon(row.Cells[i].Value.ToString().ReplaceEscapeControlChars()).EscapeControlChars();      // display escaped
                    if (nt != row.Cells[i].Value.ToString())
                    {
                        row.Cells[i].Value = nt;
                        changedsomething = true;
                    }
                }

                if (originalenglish == newenglish)
                {
                    if ( !changedsomething)
                        MessageBox.Show($"{rowno + 1} {originalkey} No change in english text or any other text\r\n{newenglish}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (translators[0].IsDefined(newsha))
                {
                    MessageBox.Show($"{rowno + 1} {originalkey} Duplicate definition of {newenglish}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    changedsomething = true;
                    row.Cells[ColEnglish.Index].Value = newenglish;
                    row.Cells[ColFound.Index].Value = "0";          // cancel the find, rerun it !

                    string sha = translators[0].ChangeEnglish(originalkey, newenglish.ReplaceEscapeControlChars());

                    row.Cells[ColID.Index].Value = sha;
                    row.Tag = sha;
                }

                if ( changedsomething)
                    HighlightPunctuation(row.Index);
            }

            if (changedsomething)
            {
                SetDirty();
            }
        }

        private void insertEntryAboveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertNewEntry(false);
        }
        private void insertEntryBelowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertNewEntry(true);
        }

        private void InsertNewEntry(bool after)
        {
            var row = dataGridView.ClickedRightRow;
            if (row != null)
            {
                string shatoinsertat = row.Cells[ColID.Index].Value.ToString();
                string english = "Edit text!";
                while (translators[0].IsDefined(english.CalcSha8()))
                    english += "!";

                string sha = english.CalcSha8();

                foreach (var tx in translators)
                {
                    tx.Insert(shatoinsertat, after, english, tx == translators[0] ? english : null);
                }

                var data = MakeCells(sha);
                int rowi = row.Index + (after ? 1 : 0);
                dataGridView.Rows.Insert(rowi, data.ToArray());
                dataGridView.Rows[rowi].Tag = sha;
                dataGridView.Rows[rowi].Cells[ColFound.Index].Value = "0";      // mark as needs rescan

                WriteHeaders();

                SetDirty();
            }
        }

        private void changeSectionNameStripMenuItem_Click(object sender, EventArgs e)
        {
            var row = dataGridView.ClickedRightRow;
            if (row != null)
            {
                EditForm ef = new EditForm();
                ef.EditText = "Name..";
                ef.SingleLineEdit();
                if (ef.ShowDialog(this) == DialogResult.OK)
                {
                    string cursectionname = row.Cells[ColSection.Index].Value.ToString();
                    string shatoinsertat = row.Cells[ColID.Index].Value.ToString();

                    string newsectionname = ef.EditText;

                    foreach (var tx in translators)
                    {
                        tx.ChangeSectionName(shatoinsertat, newsectionname);
                    }

                    for (int r = row.Index; r < dataGridView.RowCount; r++)
                    {
                        var id = dataGridView.Rows[r].Tag.ToString();
                        translators[0].TryGetEntry(id, out TranslatorMkII.Entry entry);
                        System.Diagnostics.Debug.Assert(entry.Section != null);
                        if (entry.Section == cursectionname)
                        {
                            entry.Section = newsectionname;
                            dataGridView.Rows[r].Cells[ColSection.Index].Value = newsectionname;
                        }
                        else
                            break;
                    }

                    SetDirty();
                }
            }
        }
        private void findNextMissingTranslationFromScanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var row = dataGridView.RightClickRow;
            if (row >= 0)
                NextMissing(row);
        }
        private void findNextPunctationMarkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var row = dataGridView.RightClickRow;
            if (row >= 0)
                NextPunc(row);
        }


        #endregion


        #region Cell Clicks

        private void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            Edited(e.RowIndex,e.ColumnIndex);
        }

        private void dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if ( e.RowIndex == -1 && e.ColumnIndex == -1)
            {
                dataGridView.Sort(new RowComparer());
                dataGridView.ClearSelection();
            }
        }

        private void dataGridView_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView.EndEdit();
            EditInBox(e.RowIndex, e.ColumnIndex);
        }

        #endregion



        #region Helpers
        private void EditInBox(int rowindex, int colindex)
        {
            if (rowindex >= 0 && colindex >= 0 && dataGridView[colindex, rowindex].ReadOnly == false)
            {
                string text = dataGridView[colindex, rowindex].Value.ToString().ReplaceEscapeControlChars();

                EditForm ef = new EditForm();
                ef.EditText = text;
                if (ef.ShowDialog(this) == DialogResult.OK)
                {
                    dataGridView[colindex, rowindex].Value = ef.EditText.EscapeControlChars();
                    Edited(rowindex, colindex);
                }
            }
        }

        private void Edited(int rowindex, int colindex)
        {
            if (colindex == ColEnglish.Index)
            {
                var row = dataGridView.Rows[rowindex];
                string orginalkey = row.Tag.ToString();
                string newenglish = row.Cells[ColEnglish.Index].Value.ToString().ReplaceEscapeControlChars();       // move back to normal, not display escaped

                if (translators[0].TryGetEntry(orginalkey, out TranslatorMkII.Entry orgentry) && orgentry.English != newenglish)
                {
                    string newsha = newenglish.CalcSha8();

                    if (translators[0].IsDefined(newsha))
                    {
                        MessageBox.Show($"Duplicate definition of {newenglish}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        row.Cells[ColEnglish.Index].Value = orgentry.English;
                    }
                    else
                    {
                        row.Cells[ColID.Index].Value = newsha;

                        foreach (var tx in translators)
                        {
                            tx.ChangeEnglish(orginalkey, newenglish);
                        }

                        row.Tag = newsha;

                        row.Cells[ColFound.Index].Value = "0";      // mark as needs rescan

                        HighlightPunctuation(row.Index);
                        SetDirty();
                    }
                }
            }
            else
            {
                HighlightPunctuation(rowindex);
                SetDirty();
            }
        }

        Color FieldBuilderPunctuationColour = Color.LightYellow;
        Color FormatPunctuationColour = Color.LightCyan;
        Color SpacesPunctuationColour = Color.Lavender;
        private void HighlightPunctuation(int rowno)
        {
            for (int i = ColEnglish.Index; i < dataGridView.ColumnCount; i++)
            {
                string value = dataGridView.Rows[rowno].Cells[i].Value.ToString();

                bool badspaced = value.StartsWith(" ") || value.Trim().StartsWith("-") || value.Trim().StartsWith(",") || value.EndsWith(" ") || 
                    value.Trim().EndsWith(",") || value.Trim().EndsWith(".");
                bool badfield = value.Trim().StartsWith("<") || value.Contains(";N0") || value.Contains(";N1") || value.Contains(";0.") || value.Contains(";;");
                bool badformat = value.Contains("{") && value.Contains("}");

                if ( value.Contains("Tab setup failure"))
                {

                }
                dataGridView.Rows[rowno].Cells[i].Style.BackColor = badfield ? FieldBuilderPunctuationColour : badformat ? FormatPunctuationColour : badspaced ? SpacesPunctuationColour : Color.Empty;
                dataGridView.Rows[rowno].Cells[i].ToolTipText = (badfield ? "Bad Field Builder " : "") + (badformat ? "Bad Format " : "") + (badspaced ? "Spaces commas fullstops" : "");
            }
        }


        private void Filter()
        {
            Cursor = Cursors.WaitCursor;
            var curcell = dataGridView.CurrentCell;

            string section = comboBoxSection.SelectedItem.ToString();
            string textfind = textBoxFilter.Text;
            dataGridView.FilterGridView((row) =>
            {
                if (textfind.HasChars())
                {
                    bool found = false;
                    for (int i = 0; i < dataGridView.ColumnCount; i++)
                    {
                        var cc = row.Cells[i].Value.ToString();
                        if (cc.StartsWithOrContains(textfind))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        return false;
                }

                if (section.Contains("."))
                {
                    return row.Cells[ColSource.Index].Value.ToString().Contains(section);
                }
                else
                {
                    return section == "All" || row.Cells[ColSection.Index].Value.ToString() == section;
                }
            });

            if (curcell != null)
            {
                dataGridView.SetCurrentSelOnRow(curcell.RowIndex, curcell.ColumnIndex);
                dataGridView.DisplayRow(curcell.RowIndex, true);
            }

            Cursor = Cursors.Default;
        }

        private void WriteHeaders()
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
                row.HeaderCell.Value = (row.Index + 1).ToStringInvariant();
        }


        private void NextMissing(int row)
        {
            while (row < dataGridView.RowCount)
            {
                if (dataGridView[ColFound.Index, row].Value.ToString() == "0" && dataGridView.Rows[row].Visible)
                {
                    dataGridView.DisplayRow(row, true);
                    dataGridView.CurrentCell = dataGridView[ColEnglish.Index, row];
                    break;
                }
                row++;
            }
        }
        private void NextPunc(int row)
        {
            while (row < dataGridView.RowCount)
            {
                for (int i = ColEnglish.Index; i < dataGridView.ColumnCount; i++)
                {
                    if (dataGridView[i, row].Style.BackColor != Color.Empty && dataGridView.Rows[row].Visible)
                    {
                        dataGridView.DisplayRow(row, true);
                        dataGridView.CurrentCell = dataGridView[i, row];
                        return;
                    }
                }
                row++;
            }
        }

        private void FindNext(string text)
        {
            if (dataGridView.DisplayedRowCount(true) > 0)
            {
                int startrow = dataGridView.CurrentCell != null ? dataGridView.CurrentCell.RowIndex : 0;
                for (int row = startrow + 1; row != startrow; row = (row + 1) % dataGridView.RowCount)
                {
                    for (int i = ColEnglish.Index; i < dataGridView.ColumnCount; i++)
                    {
                        if (dataGridView[i, row].Value.ToString().StartsWithOrContains(text))
                        {
                            dataGridView.DisplayRow(row, true);
                            dataGridView.CurrentCell = dataGridView[i, row];
                            return;
                        }
                        if (checkBoxEnglishOnly.Checked)
                            break;
                    }
                }
            }
        }

        bool dirty = false;
        void SetDirty(bool dirty = true)
        {
            this.dirty = dirty;
            buttonSave.BackColor = dirty ? Color.Red : Color.FromArgb(240, 240, 240);
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

        #endregion


        #region Scan Files

        string initdir = @"c:\code\eddiscovery";
        bool selectedonce = false;
        private void buttonScanFiles_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog ofd = new FolderBrowserDialog();
            ofd.SelectedPath = initdir;
            if (selectedonce || Directory.Exists(initdir) || ofd.ShowDialog() == DialogResult.OK)
            {
                selectedonce = true;
                splitContainer.Panel2Collapsed = true;
                richTextBoxErrors.Clear();


                Dictionary<string, ScanMkIIFiles.Item> items = new Dictionary<string, ScanMkIIFiles.Item>();

                ScanMkIIFiles.ScanFiles(items, Path.Combine(ofd.SelectedPath, "EDDiscovery"), "*.cs");

#if true
                ScanMkIIFiles.ScanFiles(items, Path.Combine(ofd.SelectedPath, "ActionLanguage", "ActionLanguage"), "*.cs");
                ScanMkIIFiles.ScanFiles(items, Path.Combine(ofd.SelectedPath, "EliteDangerousCore"), "*.cs");
                ScanMkIIFiles.ScanFiles(items, Path.Combine(ofd.SelectedPath, "BaseUtilities", "BaseUtilities", "WinForms"), "*.cs");
                ScanMkIIFiles.ScanFiles(items, Path.Combine(ofd.SelectedPath, "ExtendedControls", "ExtendedForms"), "*.cs");
                ScanMkIIFiles.ScanFiles(items, Path.Combine(ofd.SelectedPath, "ExtendedControls", "ExtendedControls"), "*.cs");
                ScanMkIIFiles.ScanFiles(items, Path.Combine(ofd.SelectedPath, "ExtendedControls", "AudioForms"), "*.cs");
#endif
                initdir = ofd.SelectedPath;

                foreach(DataGridViewRow row in dataGridView.Rows)
                {
                    string id = row.Tag.ToString();
                    translators[0].TryGetEntry(id, out BaseUtils.TranslatorMkII.Entry entry);

                    bool found = items.TryGetValue(entry.English, out ScanMkIIFiles.Item im);
                    row.Cells[ColFound.Index].Style.BackColor = found ? Color.LightGreen : Color.Empty;

                    string flist = im != null ? string.Join(Environment.NewLine, 
                        im.FileLines.Keys.Select(x => output(x,im.FileLines[x]))) : "0";

                    row.Cells[ColFound.Index].Value = flist;

                    if (found)
                        items.Remove(entry.English);

                    row.Cells[ColSource.Index].Style.BackColor = flist!=null && entry.File.ContainsIIC(@"EDDiscovery\EDDiscovery") &&
                                        flist.ContainsIIC(@"EDDiscovery\EliteDangerousCore") ? Color.Red : Color.Empty;
                }

                if ( items.Count>0)
                {
                    foreach(KeyValuePair<string, ScanMkIIFiles.Item> kvp in items)
                    {
                        foreach (var item in kvp.Value.FileLines)
                        {
                            richTextBoxErrors.AppendText(kvp.Key.CalcSha8() + " `" + kvp.Key + "` : " + output(item.Key, item.Value));
                            richTextBoxErrors.AppendText("\r\n");

                            //System.Diagnostics.Debug.WriteLine($"Key : `{kvp.Key}` :\r\n  ");
                            //for (int i = 0; i < kvp.Key.Length; i++)
                            //    System.Diagnostics.Debug.Write($"{(int)kvp.Key[i]:x2}");
                            //System.Diagnostics.Debug.WriteLine("\n");
                        }

                    }
                    splitContainer.Panel2Collapsed = false;
                }
                buttonNextMissing.Enabled = true;
            }

        }

        string output(string file, List<int> items)
        {
            return Path.GetFileNameWithoutExtension(file) + ":" + string.Join(",",items.Select(x=>x.ToStringInvariant()));
        }


        // to use this, just load on the command line the language you want to patch. Then point at the patch .tlp/.tlf and it will update
        // items in green were not previously defined, red have been changed

        private void buttonPatch_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "TLFTLP|*.tlf;*.tlp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                TranslatorMkII patch = new TranslatorMkII();
                patch.LoadFile(ofd.FileName,-1);

                if (patch.Translating)
                {
                    foreach (var id in patch.EnumerateIDs)
                    {
                        if (!TranslatorMkII.IsSourceID(id))     // same ID, same english text
                        {
                            patch.TryGetEntry(id, out TranslatorMkII.Entry patchentry);
                            if (translators[1].TryGetEntry(id, out TranslatorMkII.Entry entry))
                            {
                                if (entry.Foreign != patchentry.Foreign)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Difference {id} `{entry.English}`: `{entry.Foreign}` vs `{patchentry.Foreign}`");
                                    entry.Foreign = patchentry.Foreign;
                                    for( int row = 0; row < dataGridView.Rows.Count; row++)
                                    {
                                        if (dataGridView.Rows[row].Tag.ToString() == id)
                                        {
                                            string orgvalue = dataGridView.Rows[row].Cells[ColEnglish.Index + 1].Value.ToString();
                                            dataGridView.Rows[row].Cells[ColEnglish.Index + 1].ToolTipText = orgvalue;
                                            dataGridView.Rows[row].Cells[ColEnglish.Index + 1].Value = patchentry.Foreign;
                                            dataGridView.Rows[row].Cells[ColEnglish.Index + 1].Style.BackColor = orgvalue.HasChars() ? Color.Red : Color.Green;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"Same       {id} `{entry.English}` :  `{entry.Foreign}` vs `{patchentry.Foreign}`");
                                }

                            }
                        }

                    }
                }
            }
        }

        #endregion

        }
}
