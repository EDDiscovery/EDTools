using BaseUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDDTest
{
    public static class ScanTranslate
    {
        public class Definition
        {
            public Definition(string t, string x, string l) { token = t;text = x; firstdeflocation = l; }
            public string token;
            public string text;
            public string firstdeflocation;
        };

        static public Tuple<string, string, bool> ProcessLine(string combinedline, string curline, int txpos, int parapos , bool warnoldidchange)
                                    
        {
            bool ok = false;
            bool local = true;
            string engphrase = "";
            string keyword = "";

            StringParser s0 = new StringParser(combinedline, txpos);

            if (s0.ReverseBack())
            {
                var res = s0.NextOptionallyBracketedQuotedWords();

                if (res != null)
                {
                    foreach (var t in res)
                    {
                        string ns = t.Item1.Replace(" ", "");

                        if (t.Item2)
                            engphrase += t.Item1;
                        else if (ns == "+Environment.NewLine+Environment.NewLine+")
                            engphrase += "\\r\\n\\r\\n";
                        else if (ns == "+Environment.NewLine+Environment.NewLine")
                            engphrase += "\\r\\n\\r\\n";
                        else if (ns == "+Environment.NewLine+")
                            engphrase += "\\r\\n";
                        else if (ns == "Environment.NewLine+")
                            engphrase += "\\r\\n";
                        else if (ns == "+Environment.NewLine")
                            engphrase += "\\r\\n";
                        else if (ns == "+")
                            engphrase += "";
                        else
                        {
                            engphrase = null;
                            break;
                        }
                    }

                    if (engphrase != null)
                    {
                        StringParser s1 = new StringParser(combinedline, parapos);

                        string nextword = s1.NextWord(",)");

                        if (nextword != null && nextword.StartsWith("typeof("))
                        {
                            nextword = nextword.Substring(7);

                            if ( s1.IsCharMoveOn(')'))
                            {
                                if (s1.IsCharMoveOn(','))
                                {
                                    keyword = s1.NextQuotedWord(")");

                                    if (keyword != null)
                                    {
                                        keyword = nextword + "." + keyword;
                                        ok = true;
                                    }
                                }
                                else if (s1.IsCharMoveOn(')'))
                                {
                                    if (warnoldidchange && engphrase.FirstAlphaNumericText() != engphrase.ReplaceNonAlphaNumeric())
                                        Console.WriteLine("Warning : Changed ID " + engphrase + "  " + engphrase.FirstAlphaNumericText() + " old " + engphrase.ReplaceNonAlphaNumeric());
                                    keyword = nextword + "."+ engphrase.FirstAlphaNumericText();
                                    ok = true;
                                }
                            }
                        }
                        else if (nextword != null && (nextword == "this" || nextword == "t"))
                        {
                            if (s1.IsCharMoveOn(','))
                            {
                                keyword = s1.NextQuotedWord(")");

                                if (keyword != null)
                                {
                                    ok = true;
                                }
                            }
                            else if (s1.IsCharMoveOn(')'))
                            {
                                keyword = engphrase.FirstAlphaNumericText();

                                if (warnoldidchange && keyword != engphrase.ReplaceNonAlphaNumeric())
                                    Console.WriteLine("Warning : Changed ID " + engphrase + "  " + engphrase.FirstAlphaNumericText() + " old " + engphrase.ReplaceNonAlphaNumeric());

                                ok = true;
                            }
                        }
                        else if (s1.IsCharMoveOn(')'))
                        {
                            keyword = engphrase.FirstAlphaNumericText();
                            if (warnoldidchange && keyword != engphrase.ReplaceNonAlphaNumeric())
                                Console.WriteLine("Warning : Changed ID " + engphrase + "  " + engphrase.FirstAlphaNumericText() + " old " + engphrase.ReplaceNonAlphaNumeric());
                            local = false;
                            ok = true;
                        }
                    }
                }
            }

            return (ok) ? new Tuple<string, string, bool>(keyword, engphrase, local) : null;
        }

        class DefInfo
        {
            public string newtype;
            public string parent;
        }

        static public string Process(FileInfo[] files, string language, string txpath, int searchdepth, bool combinedone, bool showrepeats, bool showerrorsonly)            // overall index of items
        {
            string locals = "";
            string globals = "";
            bool doneglobalstitle = false;
            List<Definition> globalsdone = new List<Definition>();
            List<Definition> localsdone = new List<Definition>();

            BaseUtils.Translator trans = BaseUtils.Translator.Instance;

            if (language.HasChars() && txpath.HasChars())
            {
                trans.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, Path.GetTempPath(), loadorgenglish:true);
            }

            if (!trans.Translating)
            {
                locals += "********************* TRANSLATION NOT LOADED" + Environment.NewLine;

            }

            foreach (var fi in files)
            {
                var utc8nobom = new UTF8Encoding(false);        // give it the default UTF8 no BOM encoding, it will detect BOM or UCS-2 automatically

                using (StreamReader sr = new StreamReader(fi.FullName, utc8nobom))         // read directly from file.. presume UTF8 no bom
                {
                    List<string> classes = new List<string>();
                    List<string> baseclasses = new List<string>();
                    List<int> classeslevel = new List<int>();
                    Dictionary<string, DefInfo> winformdefs = new Dictionary<string, DefInfo>();

                    int bracketlevel = 0;

                    if ( !combinedone )
                        localsdone = new List<Definition>();

                    bool donelocaltitle = false;

                    string line,previoustext="";
                    int lineno = 0;

                    string dropdown = null;

                    while ((line = sr.ReadLine()) != null)
                    {
                        lineno++;

                        int startpos = previoustext.Length;

                        string combined = previoustext + line;

                        while (true)
                        {
                            bool usebasename = false;

                            int txpos = combined.IndexOf(".Tx(", startpos);
                            int txbpos = combined.IndexOf(".Txb(", startpos);
                            int parapos = txpos + 4;

                            if ( txbpos >= 0 && (txpos ==-1 || txpos > txbpos ))
                            {
                                txpos = txbpos;
                                parapos = txpos + 5;
                                usebasename = true;
                            }

                            string localtext = "";
                            string globaltext = "";

                            if (txpos != -1)
                            {
                                Tuple<string, string, bool> ret = ProcessLine(combined, line, txpos, parapos, false);

                                if (ret == null)
                                    localtext = fi.FullName + ":" + lineno + ":Miss formed line around " + combined.Mid(txpos, 30) + Environment.NewLine;
                                else
                                {
                                    bool local = ret.Item3;

                                    string classprefix = "";

                                    if ( !ret.Item1.Contains("."))     // typeof is already has class name sksksk.skksks 
                                        classprefix = local ? (usebasename ? (baseclasses.Count > 0 ? (baseclasses.Last()+".") : null) : (classes.Count > 0 ? (classes.Last()+".") : null)) : "";

                                    if (classprefix == null)
                                    {
                                        localtext = fi.FullName + ":" + lineno + ":ERROR: No class to assign name to - probably not reading {} properly " + ret.Item1 + Environment.NewLine;
                                    }
                                    else
                                    {
                                        string id = classprefix + ret.Item1;

                                        Definition def = (local ? localsdone : globalsdone).Find(x => x.token.Equals(id, StringComparison.InvariantCultureIgnoreCase));

                                        string engquoted = ret.Item2.AlwaysQuoteString();

                                        string res = null;
                                        if (def != null)
                                        {
                                            if (def.text != ret.Item2)
                                                res = fi.FullName + ":" + lineno + ":ERROR: ID has different text " + id + " " + Environment.NewLine + "   >> " + ret.Item2.AlwaysQuoteString() + " orginal " + def.text.AlwaysQuoteString() + " at " + def.firstdeflocation;
                                            else if (showrepeats)       // if showrepeats is off, then no output, since we already done it
                                                res = "//Repeat " + id + " " + engquoted;
                                        }
                                        else
                                        {
                                            (local ? localsdone : globalsdone).Add(new Definition(id, ret.Item2, fi.FullName + ":" + lineno));

                                            res = id + ": " + engquoted + " @";     // list it

                                            if (trans.IsDefined(id))
                                            {
                                                string foreign = trans.GetTranslation(id);
                                                string english = trans.GetOriginalEnglish(id);
                                                english = english.EscapeControlChars();

                                                if ( foreign != null )
                                                {
                                                    if (english != ret.Item2)
                                                    {
                                                        res += " // Translation present, english differs from " + ret.Item2 + " vs " + english;
                                                    }
                                                    else
                                                    {   
                                                        if (!showerrorsonly)            // everything is okay - do we list it..
                                                            res += " // OK";
                                                        else
                                                            res = null;
                                                    }
                                                }
                                                else
                                                {
                                                    if (!showerrorsonly)                // if we are showing all, we say there is no translation
                                                        res += " // Translation present, no definition present";
                                                    else
                                                        res = null;
                                                }
                                            }       
                                            else if ( trans.Translating )       // if we are checking translation, do it..
                                            {
                                                res += " // NOT DEFINED";
                                            }
                                        }

                                        if (res != null)
                                        {
                                            res += Environment.NewLine;

                                            if (local)
                                                localtext = res;
                                            else
                                                globaltext = res;
                                        }
                                    }
                                }

                                if (localtext.HasChars())
                                {
                                    if (!donelocaltitle)
                                    {
                                        string text = "///////////////////////////////////////////////////// " + (classes.Count > 0 ? classes[0] : "?") + " in " + fi.Name + Environment.NewLine;
                                        locals += text;
                                        donelocaltitle = true;
                                    }
                                    locals += localtext;
                                }

                                if ( globaltext.HasChars())
                                { 
                                    if (!doneglobalstitle)
                                    {
                                        globals += "///////////////////////////////////////////////////// Globals" + Environment.NewLine;
                                        doneglobalstitle = true;
                                    }
                                    globals += globaltext;
                                }

                                startpos = parapos;
                            }
                            else
                                break;
                        }

                        previoustext += line;
                        if (previoustext.Length > 20000)
                            previoustext = previoustext.Substring(10000);


                        line = line.Trim();

                        int clspos = line.IndexOf("partial class ");
                        if (clspos == -1)
                            clspos = line.IndexOf("public class ");
                        if (clspos == -1)
                            clspos = line.IndexOf("abstract class ");
                        if (clspos == -1)
                            clspos = line.IndexOf("static class ");

                        if (clspos >= 0)
                        {
                            StringParser sp = new StringParser(line, clspos);
                            sp.NextWord(" ");
                            sp.NextWord(" ");
                            classes.Add(sp.NextWord(":").Trim());
                            baseclasses.Add(sp.IsCharMoveOn(':') ? sp.NextWord(",") : null);
                            classeslevel.Add(bracketlevel);
                            System.Diagnostics.Debug.WriteLine(lineno + " {" + bracketlevel * 4 + " Push " + classes.Last() + " " + baseclasses.Last());
                        }

                        if (line.StartsWith("{"))
                        {
                            if (line.Length == 1 || !line.Substring(1).Trim().StartsWith("}"))
                            {
                                System.Diagnostics.Debug.WriteLine(lineno + " {" + bracketlevel * 4);
                                bracketlevel++;
                            }
                            else
                                System.Diagnostics.Debug.WriteLine(lineno + " Rejected {" + bracketlevel * 4);
                        }

                        if (line.StartsWith("}"))
                        {
                            if (line.Length == 1 || line.Substring(1).Trim()[0] == '/' || line.Substring(1).Trim()[0] == ';')
                            {
                                if (classeslevel.Count > 0 && classeslevel.Last() == bracketlevel - 1)
                                {
                                    System.Diagnostics.Debug.WriteLine(lineno + " Pop {" + (bracketlevel-1) * 4 + " " + classes.Last());
                                    classes.RemoveAt(classes.Count - 1);
                                    classeslevel.RemoveAt(classeslevel.Count - 1);
                                    baseclasses.RemoveAt(baseclasses.Count - 1);
                                }
                                bracketlevel--;
                                System.Diagnostics.Debug.WriteLine(lineno + " }" + bracketlevel * 4);
                            }
                            else
                                System.Diagnostics.Debug.WriteLine(lineno + " Rejected }" + bracketlevel * 4);

                        }

                        if ( line.StartsWith("this.") && fi.Name.Contains("Designer",StringComparison.InvariantCultureIgnoreCase) )
                        {
                            StringParser sp = new StringParser(line, 5);
                            string controlname = sp.NextWord(".=,()} ");
                            string propname = sp.IsCharMoveOn('.') ? sp.NextWord(".=,()} ") : null;
                            string value = null;        // value used to indicate we have a id

                            System.Diagnostics.Debug.WriteLine(lineno + " This. " + controlname + " . " + propname);

                            if (dropdown != null)
                            {
                                System.Diagnostics.Debug.WriteLine(">> Drop down " + dropdown + " " + sp.LineLeft);

                                DefInfo di = winformdefs.ContainsKey(controlname) ? winformdefs[controlname] : null;

                                if (di != null)
                                    di.parent = dropdown;

                                if (sp.Find("});"))
                                    dropdown = null;
                            }
                            else if (propname == "DropDownItems")
                            {
                                System.Diagnostics.Debug.WriteLine("Found drop down " + sp.LineLeft);
                                dropdown = controlname;
                            }
                            else if (propname == "SetToolTip" && sp.IsCharMoveOn('('))
                            {
                                if (sp.NextWord(".") == "this" && sp.IsCharMoveOn('.'))
                                {
                                    controlname = sp.NextWord(".=,()} ") + ".ToolTip";
                                    if (sp.IsCharMoveOn(','))
                                    {
                                        value = sp.NextQuotedWord();
                                    }
                                }
                            }
                            else if (controlname != "Text" && propname == null && sp.IsCharMoveOn('='))      // this.control = 
                            {
                                if (sp.NextWord() == "new")     // this.name = new..
                                {
                                    winformdefs[controlname] = new DefInfo() { newtype = sp.NextWord(";") };
                                    System.Diagnostics.Debug.WriteLine("Def " + controlname + " " + winformdefs[controlname]);
                                }
                            }
                            else if ((controlname == "Text" && propname == null && sp.IsCharMoveOn('=')) || (propname != null && propname == "Text" && sp.IsCharMoveOn('=')))
                            {
                                value = sp.NextQuotedWord();

                                bool ok = true;

                                if (controlname != "Text")
                                {
                                    DefInfo di = winformdefs.ContainsKey(controlname) ? winformdefs[controlname] : null;

                                    if (di != null)
                                    {
                                        string[] excluded = new string[]
                                        {
                                                "ComboBoxCustom", "NumberBoxDouble", "NumberBoxLong", "VScrollBarCustom",     // Controls not for translation..
                                                "StatusStripCustom" , "RichTextBoxScroll","TextBoxBorder", "AutoCompleteTextBox", "DateTimePicker" , "NumericUpDownCustom",
                                                "Panel", "DataGridView", "GroupBox", "SplitContainer", "LinkLabel"
                                        };

                                        ok = Array.FindIndex(excluded, (xx) => di.newtype.Contains(xx)) == -1;

                                        if (di.parent != null)
                                        {
                                            DefInfo dip = winformdefs.ContainsKey(di.parent) ? winformdefs[di.parent] : null;       // double deep ..
                                            controlname = (dip != null && dip.parent != null ? (dip.parent + ".") : "") + di.parent + "." + controlname;
                                        }
                                    }
                                }
                                else
                                    controlname = "";

                                if (!ok || value == "<code>")
                                    value = null;
                            }

                            if ( value != null )
                            {
                                string classname = (classes.Count > 0) ? classes.Last() : "ERROR NO CLASS!";
                                string id = classname + (controlname.HasChars() ? "." + controlname : "");
                                string res = id + ": " + value.AlwaysQuoteString() + " @";

                                if (trans.IsDefined(id))
                                {
                                    string foreign = trans.GetTranslation(id);
                                    string english = trans.GetOriginalEnglish(id);
                                    english = english.EscapeControlChars();

                                    if (foreign != null)
                                    {
                                        if (english != value )
                                        {
                                            res += " // Translation present, english differs from " + value + " vs " + english;
                                        }
                                        else
                                        {
                                            if (!showerrorsonly)            // everything is okay - do we list it..
                                                res += " // OK";
                                            else
                                                res = null;
                                        }
                                    }
                                    else
                                    {
                                        if (!showerrorsonly)                // if we are showing all, we say there is no translation
                                            res += " // Translation present, no definition present";
                                        else
                                            res = null;
                                    }
                                }
                                else if (trans.Translating)       // if we are checking translation, do it..
                                {
                                    res += " // NOT DEFINED";
                                }

                                if (res != null)
                                {
                                    if (!donelocaltitle)
                                    {
                                        string text = "///////////////////////////////////////////////////// " + (classes.Count > 0 ? classes[0] : "?") + " in " + fi.Name + Environment.NewLine;
                                        locals += text;
                                        donelocaltitle = true;
                                    }

                                    locals += res + Environment.NewLine;
                                }
                            }
                        }

                    }
                }
            }

            return locals + globals;
        }
    }
}

