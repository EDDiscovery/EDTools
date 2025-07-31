using BaseUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace EDDTest.Translations
{
    // replacetxid c:\code\eddiscovery\eddiscovery  *.cs c:\code\eddiscovery\eddiscovery\translations 2 example-ex
    // replacetxid c:\code\eddiscovery\elitedangerouscore *.cs c:\code\eddiscovery\eddiscovery\translations 2 example-ex
    // replacetxid c:\code\eddiscovery\eddiscovery\usercontrols\history  *.cs c:\code\eddiscovery\eddiscovery\translations 2 example-ex

    // ??                 ExtendedControls.MessageBoxTheme.Show(FindForm(), "System could not be found - has not been synched or EDSM is unavailable".T(EDTx.UserControlTravelGrid_NotSynced));


    internal class ScanForTX
    {
        // look thru designer.cs and other cs files for translation strings
        static public void ScanAnalyse(string file, Dictionary<string,string> idandtext, bool replacetxid)
        {
            var utc8nobom = new UTF8Encoding(false);        // give it the default UTF8 no BOM encoding, it will detect BOM or UCS-2 automatically

            using (StreamReader sr = new StreamReader(file, utc8nobom))         // read directly from file.. presume UTF8 no bom
            {
                bool updatefile = false;
                List<string> lines = new List<string>();
                string line;
                string classname = "?";
                while ((line = sr.ReadLine()) != null)
                {
                    if (file.Contains(".Designer.cs") )
                    {
                        StringParser sp = new StringParser(line);
                        if ( sp.IsStringMoveOn("partial class"))
                        {
                            classname = sp.NextWord();
                        }
                        else if (line.Contains("this.") && line.Contains(".Text = "))
                        {
                            if (sp.IsStringMoveOn("this."))
                            {
                                string control = sp.NextWord(".");
                                if (sp.IsStringMoveOn(".Text") && sp.IsCharMoveOn('='))
                                {
                                    string text = sp.NextQuotedWord();
                                    if (text.Length > 2 && text.Any(x=>char.IsLetter(x)))
                                        idandtext[classname+"."+control] = text.ReplaceEscapeControlCharsFull();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (line.Contains("Translator.Instance.TranslateToolstrip"))
                        {
                            //             BaseUtils.Translator.Instance.TranslateToolstrip(historyContextMenu, enumlistcms, this);
                            StringParser sp = new StringParser(line);
                            int textstartindex = sp.Position;
                            if (sp.SkipTo(".TranslateToolstrip") && sp.IsCharMoveOn('('))
                            {
                                updatefile = true;
                                string ttname = sp.NextWord(", ");
                                line = new string(' ', textstartindex) + $"BaseUtils.TranslatorMkII.Instance.TranslateToolstrip({ttname});";
                            }
                            else
                                System.Diagnostics.Debug.WriteLine("BAD toolttrip line");
                        }
                        else if (line.Contains("Translator.Instance.TranslateTooltip"))
                        {
                            //             BaseUtils.Translator.Instance.TranslateTooltip(toolTip, enumlisttt, this);
                            StringParser sp = new StringParser(line);
                            int textstartindex = sp.Position;
                            if (sp.SkipTo(".TranslateTooltip") && sp.IsCharMoveOn('('))
                            {
                                updatefile = true;
                                string ttname = sp.NextWord(", ");
                                line = new string(' ', textstartindex) + $"BaseUtils.TranslatorMkII.Instance.TranslateTooltip({ttname},this);";
                            }
                            else
                                System.Diagnostics.Debug.WriteLine("BAD tooltip line");
                        }
                        else if (line.Contains("Translator.Instance.TranslateControls"))
                        {
                            StringParser sp = new StringParser(line);
                            int textstartindex = sp.Position;
                            if (sp.SkipTo(".TranslateControls") && sp.IsCharMoveOn('('))
                            {
                                updatefile = true;
                                string ttname = sp.NextWord(",) ");
                                line = new string(' ', textstartindex) + $"BaseUtils.TranslatorMkII.Instance.TranslateControls({ttname});";
                            }
                            else
                                System.Diagnostics.Debug.WriteLine("BAD controls line");
                        }
                        else
                        {
                            {
                                int pos = 0;
                                while ((pos = line.IndexOf(".TxID", pos)) != -1)
                                {
                                    StringParser sp = new StringParser(line, pos + 5);
                                    if (sp.IsCharMoveOn('('))
                                    {
                                        string term = sp.NextWord(")");

                                        if (sp.IsCharMoveOn(')'))
                                        {
                                            StringParser spquoteback = new StringParser(line, pos);
                                            if (spquoteback.ReverseBack(true))
                                            {
                                                string text = spquoteback.NextQuotedWord();
                                                idandtext[term] = text.ReplaceEscapeControlCharsFull();
                                            }
                                            else
                                                idandtext[term] = "<code>";

                                            updatefile = true;
                                            string frontpart = line.Substring(0, pos);
                                            line = frontpart + ".Tx()" + sp.LineLeft;
                                        }
                                    }
                                    pos = pos + 6;
                                }
                            }

                            {
                                int pos = 0;
                                while ((pos = line.IndexOf(".T(", pos)) != -1)
                                {

                                    StringParser sp = new StringParser(line, pos + 5);
                                    string term = sp.NextWord(")");

                                    if (sp.IsCharMoveOn(')'))
                                    {
                                        StringParser spquoteback = new StringParser(line, pos);
                                        if (spquoteback.ReverseBack(true))
                                        {
                                            string text = spquoteback.NextQuotedWord();
                                            idandtext[term] = text.ReplaceEscapeControlCharsFull();
                                        }
                                        else
                                            idandtext[term] = "<code>";

                                        updatefile = true;
                                        string frontpart = line.Substring(0, pos);
                                        line = frontpart + ".Tx()" + sp.LineLeft;
                                    }
                                    pos = pos + 6;
                                }
                            }
                        }
                    }

                    lines.Add(line);
                }

                if (replacetxid)
                {
                    if (updatefile)
                    {
                        var inencoding = sr.CurrentEncoding;
                        sr.Close();

                        System.Diagnostics.Debug.WriteLine($" -- WRITE BACK {file}");

                        using (StreamWriter wr = new StreamWriter(file, false, inencoding))
                        {
                            foreach (var outline in lines)
                            {
                                wr.WriteLine(outline);
                            }
                        }
                    }
                    else
                        System.Diagnostics.Debug.WriteLine($" **** WANTED TO WRITE BACK BUT COULD NOT {file}");

                }

            }
        }

        // Scan for TX strings, load translator if required and see if english text is present.

        static public void ScanForTXFiles(string path, string wildcard, string txpath, int searchdepth, string language)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(path, wildcard, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            bool replace = false;

            Dictionary<string, string> ids = new Dictionary<string, string>();
            foreach (var f in allFiles)
            {
                System.Diagnostics.Debug.WriteLine($"Process {f.FullName}");
                ScanAnalyse(f.FullName, ids,replace);
            }

            if (language != null)
            {
                BaseUtils.TranslatorMkII primary = new TranslatorMkII();
                primary.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, Path.GetTempPath(), null, true);

                if (primary.Translating)
                {
                    HashSet<string> english = primary.EnumerateEnglish.ToHashSet();

                    foreach (var x in ids)
                    {
                        if (x.Value.StartsWith("<code"))
                        {
                            //System.Diagnostics.Debug.WriteLine($"{x.Key} Code");
                        }
                        else if (english.Contains(x.Value))
                        { 
                            //System.Diagnostics.Debug.WriteLine($"{x.Key} Present `{x.Value}`");
                        }
                        else
                        {
                            if ( primary.IsDefined(x.Key))
                            {
                                System.Diagnostics.Debug.WriteLine($"**** {x.Key} Is Present But english text not Present `{x.Value}`");
                            }
                            else
                                System.Diagnostics.Debug.WriteLine($"**** {x.Key} Not Present `{x.Value}`");

                        }
                    }

                }
                else
                {
                    Console.WriteLine("Primary translation did not load " + language);
                }
            }
            else
            {
                foreach (var x in ids)
                {
                    System.Diagnostics.Debug.WriteLine($"{x.Key} = `{x.Value}`");
                }
            }
           }
    }
}
