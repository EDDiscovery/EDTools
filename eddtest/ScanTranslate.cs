/*
 * Copyright © 2015 - 2021 robbyxp @ github.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using BaseUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

        static public Tuple<string, string> ProcessLine(string combinedline, string curline, int txpos, int parapos , bool warnoldidchange)
                                    
        {
            bool ok = false;
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

                        if (nextword.StartsWith("EDTx."))
                        {
                            nextword = nextword.Substring(5).Replace("_",".");

                            if (s1.IsCharMoveOn(')'))
                            {
                                keyword = nextword;
                                ok = true;
                            }
                        }
                    }
                }
            }

            return (ok) ? new Tuple<string, string>(keyword, engphrase) : null;
        }

        class DefInfo
        {
            public string newtype;
            public string parent;
        }

        static public string Process(FileInfo[] files, string language, string txpath, int searchdepth,  bool showrepeats, bool showerrorsonly)            // overall index of items
        {
            string locals = "";
            List<Definition> idsdone = new List<Definition>();

            BaseUtils.Translator trans = BaseUtils.Translator.Instance;

            if (language.HasChars() && txpath.HasChars())
            {
                trans.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, Path.GetTempPath(), loadorgenglish: true);

                if (trans.Translating)
                {
                    Console.WriteLine("Loaded translation " + language + " to compare against");
                }
                else
                {
                    Console.WriteLine("Translation for " + language + " Not found");
                    return "";
                }
            }
            else
                Console.WriteLine("No translation comparision given");

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
                            int txpos = combined.IndexOf(".T(", startpos);
                            int parapos = txpos + 3;

                            string localtext = "";

                            if (txpos != -1)
                            {
                                Tuple<string, string> ret = ProcessLine(combined, line, txpos, parapos, false);

                                if (ret == null)
                                    localtext = fi.FullName + ":" + lineno + ":Miss formed line around " + combined.Mid(txpos, 30) + Environment.NewLine;
                                else
                                {
                                    string id = ret.Item1;
                                    string engquoted = ret.Item2.AlwaysQuoteString();

                                    Definition def = idsdone.Find(x => x.token.Equals(ret.Item1, StringComparison.InvariantCultureIgnoreCase));

                                    string res = null;

                                    if (def != null)        // if previously did the def..
                                    {
                                        if (def.text != ret.Item2)
                                            res = "Different Text: " + fi.FullName + ":" + lineno + " ID has different text " + id + " " + Environment.NewLine + "   >> " + ret.Item2.AlwaysQuoteString() + " orginal " + def.text.AlwaysQuoteString() + " at " + def.firstdeflocation;
                                        else if (showrepeats)       // if showrepeats is off, then no output, since we already done it
                                            res = "Repeat: " + id + " " + engquoted;
                                    }
                                    else
                                    {
                                        idsdone.Add(new Definition(id, ret.Item2, fi.FullName + ":" + lineno));

                                        if (trans.IsDefined(id))
                                        {
                                            if (!showerrorsonly)
                                                res = id + ": " + engquoted + " @";     // list it

                                            string foreign = trans.GetTranslation(id);
                                            string english = trans.GetOriginalEnglish(id);
                                            english = english.EscapeControlChars();

                                            if (english != ret.Item2)
                                            {
                                                res = "Translator Difference: " + id + " // English differs from \"" + ret.Item2 + "\" vs \"" + english + "\"";
                                            }
                                        }
                                        else if (trans.Translating)       // if we are checking translation, do it..
                                        {
                                            res = ".T( Missing: " + id + ": " + engquoted + " @";
                                        }
                                    }

                                    if (res != null)
                                    {
                                        res += Environment.NewLine;
                                        localtext = res;
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
                            else if (propname == "HeaderText" && sp.IsCharMoveOn('='))
                            {
                                value = sp.NextQuotedWord();
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

                                if (!ok )
                                    value = null;
                            }

                            if ( value != null && value != "<code>" && value != "")
                            {
                                string classname = (classes.Count > 0) ? classes.Last() : "ERROR NO CLASS!";
                                string id = classname + (controlname.HasChars() ? "." + controlname : "");
                                string res = id + ": " + value.AlwaysQuoteString() + " @";

                                if (trans.IsDefined(id))
                                {
                                    string foreign = trans.GetTranslation(id);
                                    string english = trans.GetOriginalEnglish(id);
                                    english = english.EscapeControlChars();

                                    if (english != value)
                                    {
                                        res = "Translator difference: " + id + " // English differs from \"" + value + "\" vs \"" + english + "\"";
                                    }
                                    else if (showerrorsonly)
                                        res = null;
                                }
                                else if (trans.Translating)       // if we are checking translation, do it..
                                {
                                    res = "Designer Missing: " + res;
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

            return locals;
        }
    }
}

