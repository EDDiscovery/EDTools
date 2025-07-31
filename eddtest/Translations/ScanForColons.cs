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
    // scancolonstx c:\code\eddiscovery *.cs c:\code\eddiscovery\eddiscovery\translations 2 example-ex

    internal class ScanForColons
    {
        // look thru designer.cs and other cs files for translation strings
        static public void ScanAnalyse(string file, bool replacetxid)
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
                    if (file.Contains(".Designer.cs", StringComparison.InvariantCultureIgnoreCase) )
                    {
                        if ( line.Contains("this.toolStripLabelSystem."))
                        {

                        }
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
                                    int tpos = sp.Position;
                                    string text = sp.NextQuotedWord();

                                    if (text.EndsWith(": ") || text.EndsWith(":"))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"We should replace {line}");
                                        string newline = line.Substring(0, tpos) + text.ReplaceIfEndsWith(": ","").ReplaceIfEndsWith(":","").AlwaysQuoteString() + ";";
                                        updatefile = true;
                                        line = newline;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        {
                            int pos = 0;
                            while ((pos = line.IndexOf(".Tx(", pos)) != -1)
                            {
                                StringParser sp = new StringParser(line, pos + 4);
                                string term = sp.NextWord(")");

                                if (sp.IsCharMoveOn(')'))
                                {
                                    StringParser spquoteback = new StringParser(line, pos);
                                    if (spquoteback.ReverseBack(true))
                                    {
                                        int quotestart = spquoteback.Position;

                                        string text = spquoteback.NextQuotedWord();
                                        //System.Diagnostics.Debug.WriteLine($".TX() : `{text}`");

                                        if (text.EndsWith(": ") || text.EndsWith(":"))
                                        {
                                            System.Diagnostics.Debug.WriteLine($"We should replace {line}");
                                            updatefile = true;
                                            string frontpart = line.Substring(0, quotestart);
                                            string insertpart = ".Tx(" + term + ")+\": \"";
                                            string newline = frontpart + text.ReplaceIfEndsWith(": ", "").ReplaceIfEndsWith(":", "").AlwaysQuoteString() + insertpart + sp.LineLeft;
                                            line = newline;
                                            pos += insertpart.Length-1;
                                        }
                                        else
                                            pos = pos + 4;
                                    }
                                    else
                                        pos = pos + 4;
                                }
                                else
                                    System.Diagnostics.Debug.Assert(false);
                            }
                        }
                    }

                    lines.Add(line);
                }

                if (updatefile)
                {
                    if (replacetxid)
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

        static public void ScanForColonsFiles(string path, string wildcard, string txpath, int searchdepth, string language)
        {
            bool replace = true;

            BaseUtils.TranslatorMkII primary = new TranslatorMkII();
            primary.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, Path.GetTempPath(), null, true, true);

            if (primary.Translating)
            {

                FileInfo[] allFiles = Directory.EnumerateFiles(path, wildcard, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                foreach (var f in allFiles)
                {
                    System.Diagnostics.Debug.WriteLine($"Process {f.FullName}");
                    ScanAnalyse(f.FullName, replace);
                }

            }
        }
    }
}
