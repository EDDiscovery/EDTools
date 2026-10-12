using BaseUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translations
{
    // special codes in .cs files:
    //      .NotTxFile() : ignore file
    //
    //      public enum name // .Tx()   : translate this
    //                          .NotTx() : stop translation from here on
    //                          .NotTxThis() : disregard this entry
    //
    //  .Tx() .PTx() .TxCond( and .TxIfDefined expects a " " in front of them.  Disregarded if not a quoted string before

    // Special codes in designer.cs files:
    //      // .NotTxFile() Ignore it
    //      // .ExcludedTX() list,list of controls which do not get translated.  use this for the form title exclusion

    internal class ScanMkIIFiles
    {
        public class Item
        {
            public Dictionary<string, List<int>> FileLines { get; set; } = new Dictionary<string, List<int>>();
        }

        static void Add(Dictionary<string, Item> items, string text, string file, int line)
        {
            string rid = text.ReplaceEscapeControlChars();
            if (!items.TryGetValue(rid, out Item value))
            {
               // System.Diagnostics.Debug.WriteLine($"Add text {items.Count} {Path.GetFileName(file)} : `{text}`");
                items[rid] = new Item();
            }
            if (!items[rid].FileLines.TryGetValue(file, out List<int> lines))
                items[rid].FileLines[file] = new List<int>();

            items[rid].FileLines[file].Add(line);
        }

        static void AddCheck(Dictionary<string, Item> items, string text, string file, int line)
        {
            if (text.Length >= 2 && text.HasLetterChars() && !text.StartsWith("<code"))
                Add(items, text, file,line);
        }

        // look thru designer.cs and other cs files for translation strings
        static public void ScanAnalyse(Dictionary<string, Item> ids, string file)
        {
            var utc8nobom = new UTF8Encoding(false);        // give it the default UTF8 no BOM encoding, it will detect BOM or UCS-2 automatically

            int lineno = 0;

            List<string> excludeddesignercontrols = new List<string>();

            using (StreamReader sr = new StreamReader(file, utc8nobom))         // read directly from file.. presume UTF8 no bom
            {
                string line;
                string classname = null;

                while ((line = sr.ReadLine()) != null)
                {
                    lineno++;
                    line = line.Trim();

                    if (line.Contains(".NotTxFile()"))          // disable scan
                        break;

                    if (line.Contains("// .ExcludedTX()"))        // disable controls
                    {
                        string[] a1 = line.Substring(17).Trim().SplitNoEmptyStrings(',');
                        var trimmed = a1.Select(a => a.Trim());
                        excludeddesignercontrols.AddRange(trimmed);
                    }

                    const string enl = "\" + Environment.NewLine +";

                    while (line.EndsWith("\" +") || line.EndsWith(enl))
                    {
                        string nextline = sr.ReadLine().Trim();
                        nextline = nextline.ReplaceIfStartsWith("\"", "", donttrimstart:true);
                        lineno++;
                        line = line.ReplaceIfEndsWith("\" +", "").ReplaceIfEndsWith(enl,@"\r\n") + nextline;
                    }

                //  System.Diagnostics.Debug.WriteLine($"Line {lineno}:{line}");

                    StringParser sp = new StringParser(line);

                    if (file.Contains(".Designer.cs", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (sp.IsStringMoveOn("this."))
                        {
                            if (line.Contains(".Text = "))        // form title or control text
                            {
                                string control = sp.NextWord(". ");

                                if (control == "Text" && sp.IsCharMoveOn('='))        // Form main title
                                {
                                    if (!excludeddesignercontrols.Contains("this"))
                                    {
                                        string text = sp.NextQuotedWord();
                                        AddCheck(ids, text, file, lineno);
                                    }
                                }
                                else if (sp.IsStringMoveOn(".Text") && sp.IsCharMoveOn('='))    // control text
                                {
                                    var ex = excludeddesignercontrols.Where(x => control.WildCardMatch(x)).FirstOrDefault();
                                    if (ex==null)
                                    {
                                        string text = sp.NextQuotedWord();
                                        AddCheck(ids, text, file,lineno);
                                    }
                                }
                            }
                            else if (line.Contains(".HeaderText = "))        // form title or control text
                            {
                                string control = sp.NextWord(". ");

                                if (sp.IsStringMoveOn(".HeaderText") && sp.IsCharMoveOn('='))    // control text
                                {
                                    var ex = excludeddesignercontrols.Where(x => control.WildCardMatch(x)).FirstOrDefault();

                                    if (ex == null)
                                    {
                                        string text = sp.NextQuotedWord();
                                        AddCheck(ids, text, file,lineno);
                                    }
                                }
                            }
                            else if (line.Contains("SetToolTip("))
                            {
                                sp.Find("SetToolTip(", true);

                                string control = sp.NextWord(",");
                                sp.IsCharMoveOn(',');

                                string text = sp.NextQuotedWord();
                                AddCheck(ids, text, file, lineno);
                            }
                        }
                    }
                    else
                    {
                        // Materials Commods
                        if (sp.IsStringMoveOn("Add(CatType."))
                        {
                            while (!sp.IsEOL && !sp.IsChar('"'))
                            {
                                sp.NextWord(", "); sp.IsCharMoveOn(',');
                            }

                            if (sp.IsChar('"'))
                            {
                                string v = sp.NextQuotedWord(") ");
                                if (v != null)
                                    Add(ids, v, file, lineno);

                            }
                        }

                        // Pop out info

                        if (sp.IsStringMoveOn("{ new PanelInfo("))
                        {
                            while (!sp.IsEOL && !sp.IsChar('"'))
                            {
                                sp.NextWord(", "); sp.IsCharMoveOn(',');
                            }

                            if (sp.IsChar('"'))
                            {
                                string windowtitle = sp.NextQuotedWord(","); sp.IsCharMoveOn(',');
                                string refname = sp.NextQuotedWord(","); sp.IsCharMoveOn(',');
                                string description = sp.NextQuotedWord(","); sp.IsCharMoveOn(',');

                                if ( windowtitle!=null && description!=null)
                                {
                                    Add(ids, windowtitle, file, lineno);
                                    Add(ids, description, file, lineno);

                                }
                            }
                        }

                        // Enumerations lists

                        if (sp.IsStringMoveOn("public enum") && line.Contains(".Tx()"))
                        {
                            classname = sp.NextWord();
                            //System.Diagnostics.Debug.WriteLine($"Enum {classname}");
                        }
                        else if (classname != null)
                        {
                            if (sp.IsString("}") || line.Contains(".NotTx()"))      // turn off
                            {
                                //System.Diagnostics.Debug.WriteLine($"Enum END {classname}");
                                classname = null;
                            }
                            else if (!sp.IsString("{") && !line.Contains(".NotTxThis()"))
                            {
                                while (!sp.IsEOL && !sp.IsString("//") && sp.IsLetter())
                                {
                                    string text = sp.NextWord(", ").SplitCapsWordFull();
                                    Add(ids, text, file, lineno);
                                    //System.Diagnostics.Debug.WriteLine($"For {classname} add `{text}`");
                                    sp.SkipUntil(new char[] { ',' });
                                    sp.IsCharMoveOn(',');

                                }
                            }
                        }
                        else
                        {
                            // Marked as translatable

                            string[] terms = new string[] { ".Tx()", ".PTx()", ".TxCond(", ".TxIfDefined(" };
                            int pos = 0;
                            while ((pos = line.IndexOf(terms, out int itemno, startindex: pos)) != -1)
                            {
                                StringParser spquoteback = new StringParser(line, pos);
                                if (line.Contains("mkiiindex + 6"))
                                {

                                }
                                if (spquoteback.ReverseBack(true, false))
                                {
                                    int quotestart = spquoteback.Position;

                                    string text = spquoteback.NextQuotedWord();
                                    if (text == null)
                                    {
                                        System.Diagnostics.Debug.WriteLine($".TX() : Programatic cannot extract {line}");
                                    }
                                    else if (itemno == 1)
                                    {
                                        //System.Diagnostics.Debug.WriteLine($".TX() : `{text}`");
                                        Add(ids, text.Trim(), file, lineno);
                                    }
                                    else// if (text.Length >= 2 && text.HasLetterChars() && !text.StartsWith("<code") && !(text.StartsWith("(") && text.EndsWith(".Tx();")))
                                    {
                                        //System.Diagnostics.Debug.WriteLine($".TX() : `{text}`");
                                        Add(ids, text, file, lineno);
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($".TX() : Programatic cannot extract {line}");
                                }

                                pos += terms[itemno].Length;
                            }
                        }

                    }
                }
            }
        }

        // Scan for TX strings, load translator if required and see if english text is present.

        static public void ScanFiles(Dictionary<string, Item> ids, string path, string wildcard)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(path, wildcard, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            foreach (var f in allFiles)
            {
               // System.Diagnostics.Debug.WriteLine($"Process {f.FullName}");
                ScanAnalyse(ids, f.FullName);
            }

        }
    }
}
