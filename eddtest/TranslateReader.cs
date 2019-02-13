using BaseUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//translatereader >c:\code\k c:\code\eddiscovery\eddiscovery\translations\ 2 example-ex c:\code G c:\code\eddiscovery\eddiscovery\translations 2 italiano-it SECONDTX
//batch file to copy the resulting output to the files..
//copy Gexample-ex.tlf \code\eddiscovery\eddiscovery\translations\%1-%2.tlf
//copy Gtranslation-example-uc.tlp \code\eddiscovery\eddiscovery\usercontrols\translation-%1-uc.tlp
//copy Gtranslation-example-ed.tlp \code\eddiscovery\elitedangerous\elitedangerous\translation-%1-ed.tlp
//copy Gtranslation-example-je.tlp \code\eddiscovery\elitedangerous\journalevents\translation-%1-je.tlp



namespace EDDTest
{
    public static class TranslateReader
    {
        static public string Process(string language, string txpath, int searchdepth, 
                                    string language2, 
                                    string options
            )            // overall index of items
        {
            BaseUtils.Translator primary = BaseUtils.Translator.Instance;
            primary.LoadTranslation(language, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, Path.GetTempPath(), loadorgenglish: true, loadfile: true);

            if ( !primary.Translating)
            {
                Console.WriteLine("Primary translation did not load " + language);
                return "";
            }


            BaseUtils.Translator secondary = new BaseUtils.Translator();
            if ( language2 != null )
            {
                secondary.LoadTranslation(language2, System.Globalization.CultureInfo.CurrentCulture, new string[] { txpath }, searchdepth, @"c:\code", loadorgenglish: true, loadfile: true);

                if ( !secondary.Translating )
                {
                    Console.WriteLine("Secondary translation did not load " + language2);
                    return "";
                }
            }

            string totalret = "";

            string section = "";

            string filetowrite = "";

            List<StreamWriter> filelist = new List<StreamWriter>();

            bool hasdotted = false;

            foreach (string id in primary.EnumerateKeys)
            {
                string ret = "";

                string orgfile = primary.GetOriginalFile(id);
                FileInfo fi = new FileInfo(orgfile);
                if (filetowrite == null || !filetowrite.Equals(fi.Name))
                {
                    //ret += Environment.NewLine + "WRITETOFILE " + filetowrite + Environment.NewLine;

                    filetowrite = fi.Name;

                    if (secondary.Translating)
                    {
                        string txname = filetowrite.Replace(language, language2);

                        if ( txname.Equals(filetowrite))
                        {
                            txname = filetowrite.Replace(language.Left(language.IndexOf('-')), language2.Left(language2.IndexOf('-')));
                        }

                        filelist.Add(new StreamWriter(Path.Combine(".", txname), false, Encoding.UTF8));

                        if (filelist.Count > 1)
                            filelist[0].WriteLine(Environment.NewLine + "include " + txname);
                    }
                    else
                    {

                        filelist.Add(new StreamWriter(Path.Combine(".", filetowrite), false, Encoding.UTF8));

                        if (filelist.Count > 1)
                            filelist[0].WriteLine(Environment.NewLine + "include " + filetowrite);
                    }
                }

                string idtouse = id;

                StringParser sp = new StringParser(id);
                string front = sp.NextWord(".:");
                if (front != section)
                {
                    if ( hasdotted || sp.IsChar('.') ) 
                        ret += Environment.NewLine + "SECTION " + front + Environment.NewLine + Environment.NewLine;
                    if (sp.IsChar('.'))
                        hasdotted = true;
                    section = front;
                }

                if ( hasdotted && sp.IsChar('.') )
                    idtouse = sp.LineLeft;;

                string orgeng = primary.GetOriginalEnglish(id);
                string txprimary = primary.GetTranslation(id);

                if (secondary.Translating )
                {
                    if (secondary.IsDefined(id))
                    {
                        txprimary = secondary.GetTranslation(id);
                        secondary.UnDefine(id);
                    }
                    else
                    {
                        txprimary = null;       // meaning not present
                    }
                }

                ret += idtouse + ": " + orgeng.AlwaysQuoteString().EscapeControlChars();

                if (txprimary==null || txprimary.Equals(orgeng) || ( txprimary[0] == '<' && txprimary[txprimary.Length-1] == '>'))
                    ret += " @";
                else
                    ret += " => " + txprimary.AlwaysQuoteString().EscapeControlChars();

                ret += Environment.NewLine;


                totalret += ret;
                filelist.Last().Write(ret);
            }

            if (secondary.Translating)
            {
                foreach (string id in secondary.EnumerateKeys)
                {
                    totalret += "Secondary defines " + id + " but primary does not" + Environment.NewLine;
                }
            }

            foreach (var f in filelist)
                f.Close();


            return totalret;
        }
    }
}

