using BaseUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translations
{
    internal class ScanMkIIFiles
    {
        // look thru designer.cs and other cs files for translation strings
        static public List<string> ScanAnalyse(string file)
        {
            var utc8nobom = new UTF8Encoding(false);        // give it the default UTF8 no BOM encoding, it will detect BOM or UCS-2 automatically
            List<string> ids = new List<string>();

            using (StreamReader sr = new StreamReader(file, utc8nobom))         // read directly from file.. presume UTF8 no bom
            {
                bool updatefile = false;
                string line;
                string classname = null;

                while ((line = sr.ReadLine()) != null)
                {
                    StringParser sp = new StringParser(line);

                    if (file.Contains(".Designer.cs", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (line.Contains("this.") && line.Contains(".Text = "))
                        {
                            if (sp.IsStringMoveOn("this."))
                            {
                                string control = sp.NextWord(".");
                                if (sp.IsStringMoveOn(".Text") && sp.IsCharMoveOn('='))
                                {
                                    int tpos = sp.Position;
                                    string text = sp.NextQuotedWord();

                                    if (text.Length >= 2 && text.HasLetterChars() && !text.StartsWith("<code"))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Translate {text}");
                                        ids.Add(text);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
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
                                    ids.Add(v);

                            }
                        }

                        if (sp.IsStringMoveOn("public enum") && line.Contains(".Tx()"))
                        {
                            classname = sp.NextWord();
                            System.Diagnostics.Debug.WriteLine($"Enum {classname}");
                        }
                        else if (classname != null )
                        {
                            if (sp.IsString("}"))
                            {
                                System.Diagnostics.Debug.WriteLine($"Enum END {classname}");
                                classname = null;
                            }
                            else
                            {
                                string text = sp.NextWord(", ").SplitCapsWordFull();
                                ids.Add(text);
                            }
                        }

                        int pos = 0;
                        while ((pos = line.IndexOf(new string[] { ".Tx()", ".PTx()" },out int itemno,  startindex: pos)) != -1)
                        {
                            StringParser spquoteback = new StringParser(line, pos);
                            if (spquoteback.ReverseBack(true))
                            {
                                int quotestart = spquoteback.Position;

                                string text = spquoteback.NextQuotedWord();
                                if (text == null)
                                {
                                    System.Diagnostics.Debug.WriteLine($".TX() : Programatic cannot extract");
                                }
                                else if ( itemno == 1)
                                {
                                    System.Diagnostics.Debug.WriteLine($".TX() : `{text}`");
                                    ids.Add(text.Trim());

                                }
                                else if (text.Length >= 2 && text.HasLetterChars() && !text.StartsWith("<code") && !(text.StartsWith("(") && text.EndsWith(".Tx();")))
                                {
                                    System.Diagnostics.Debug.WriteLine($".TX() : `{text}`");
                                    ids.Add(text);
                                }
                            }

                            pos += 4;
                        }

                    }
                }
            }

            return ids;
        }

        // Scan for TX strings, load translator if required and see if english text is present.

        static public List<string> ScanFiles(string path, string wildcard)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(path, wildcard, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
            List<string> list = new List<string>();

            foreach (var f in allFiles)
            {
                System.Diagnostics.Debug.WriteLine($"Process {f.FullName}");
                list.AddRange(ScanAnalyse(f.FullName));
            }

            return list;
        }
    }
}
