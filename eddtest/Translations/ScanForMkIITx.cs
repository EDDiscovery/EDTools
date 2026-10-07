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

    internal class ScanForMKIITx
    {
        // look thru designer.cs and other cs files for translation strings
        static public void ScanAnalyse(string file)
        {
            var utc8nobom = new UTF8Encoding(false);        // give it the default UTF8 no BOM encoding, it will detect BOM or UCS-2 automatically

            using (StreamReader sr = new StreamReader(file, utc8nobom))         // read directly from file.. presume UTF8 no bom
            {
                bool updatefile = false;
                string line;
                string classname = "?";
                while ((line = sr.ReadLine()) != null)
                {
                    if (file.Contains(".Designer.cs", StringComparison.InvariantCultureIgnoreCase) )
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
                                    int tpos = sp.Position;
                                    string text = sp.NextQuotedWord();

                                    if (text.Length >= 2 && text.HasLetterChars() && !text.StartsWith("<code"))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Translate {text}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        int pos = 0;
                        while ((pos = line.IndexOf(".Tx()", pos)) != -1)
                        {
                            StringParser spquoteback = new StringParser(line, pos);
                            if (spquoteback.ReverseBack(true))
                            {
                                int quotestart = spquoteback.Position;

                                string text = spquoteback.NextQuotedWord();
                                if ( text == null )
                                {
                                    System.Diagnostics.Debug.WriteLine($".TX() : Programatic cannot extract");
                                }
                                else if (text.Length >= 2 && text.HasLetterChars() && !text.StartsWith("<code"))
                                {
                                    System.Diagnostics.Debug.WriteLine($".TX() : `{text}`");
                                }
                            }

                            pos += 4;
                        }
                    }
                }
            }
        }

        // Scan for TX strings, load translator if required and see if english text is present.

        static public void ScanFiles(string path, string wildcard)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(path, wildcard, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            foreach (var f in allFiles)
            {
                System.Diagnostics.Debug.WriteLine($"Process {f.FullName}");
                ScanAnalyse(f.FullName);
            }
        }
    }
}
