﻿using BaseUtils;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace EDDTest
{
    class Program
    {
        static void Main(string[] stringargs)
        {
            CommandArgs args = new CommandArgs(stringargs);

            string arg1 = args.Next();
            if (arg1 != null)
                arg1 = arg1.ToLower();

            if (arg1 == null )
            {
                Console.WriteLine("Journal - write journal, run for help\n"+
                                  "EDDBSTARS <filename> or EDDBPLANETS or EDDBSTARNAMES for the eddb dump\n" +
                                  "EDSMSTARS <filename> read the main dump and analyse\n" +
                                  "Phoneme <filename> <fileout> for EDDI phoneme tx\n" +
                                  "Voicerecon <filename>\n" +
                                  "DeviceMappings <filename>\n" +
                                  "StatusMove lat long latstep longstep heading headstep steptime\n" +
                                  "Status <Status flags>... UI <Flags> C:cargo F:fuel FG:Firegroup G:Gui L:Legalstate\n" +
                                  "                   superflags: normal,supercruise, landed, SRV, fight, station\n" +
                                  "StatusRead\n" +
                                  "CorolisModules rootfolder - process corolis-data\\modules\\<folder>\n" +
                                  "CorolisModule name - process corolis-data\\modules\\<folder>\n" +
                                  "CorolisShips rootfolder - process corolis-data\\ships\n" +
                                  "CorolisShip name - process corolis-data\\ships\n" +
                                  "Coroliseng rootfolder - process corolis-data\\modifications\n" +
                                  "FrontierData rootfolder - process cvs file exports of frontier data\n" +
                                  "scantranslate - process source files and look for .Tx definitions, run to see options\n" +
                                  "translatereader - process language files and normalise, run to see options\n" +
                                  "journalindented file - read lines from file in journal format and output indented\n" +
                                  "jsonindented file - read a json in file and indent\n" +
                                  "jsoncompressed file - read a json in file and compress\n" +
                                  "cutdownfile file lines\n" +
                                  "dwwp file\n"
                                  );

                return;
            }

            //*************************************************************************************************************
            // these provide their own help or do not require any more args
            //*************************************************************************************************************

            if (arg1.Equals("scantranslate"))
            {
                // sample scantranslate c:\code\eddiscovery\elitedangerous\journalevents *.cs c:\code\eddiscovery\eddiscovery\translations\ 2 italiano-it combine > c:\code\output.txt

                string path = args.Next();
                string wildcard = args.Next();
                string txpath = args.Next();
                int txsearchdepth = args.Int();
                string lang = args.Next();

                if (path != null && wildcard != null)
                {
                    FileInfo[] allFiles = Directory.EnumerateFiles(path, wildcard, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                    bool showrepeat = false;
                    bool showerrorsonly = false;

                    while (args.More)
                    {
                        string a = args.Next().ToLowerInvariant();
                        if (a == "showrepeats")
                            showrepeat = true;
                        if (a == "showerrorsonly")
                            showerrorsonly = true;
                    }

                    string ret = ScanTranslate.Process(allFiles, lang, txpath, txsearchdepth, showrepeat, showerrorsonly);
                    Console.WriteLine(ret);
                }
                else
                {
                    Console.WriteLine("Usage:\n" +
                             "scantranslate path filewildcard languagefilepath searchdepth language[opt]..- \n\n" +
                             "path filewildcard is where the source files to search for .Tx is in \n" +
                             "languagefilepath is where the .tlf files are located\n" +
                             "searchupdepth is the depth of search upwards (to root) to look thru folders for include files - 2 is normal\n" +
                             "language is the language to compare against - example-ex\n" +
                             "Opt: ShowRepeats means show repeated entries in output\n" +
                             "Opt: ShowErrorsOnly means show only errors\n"+
                             "\n"+
                             "Example:n" +
                             "eddtest scantranslate . *.cs  c:\\code\\eddiscovery\\EDDiscovery\\Translations 2 example-ex showerrorsonly\n" +
                             "\nJudgement is still required to see if a found definition has to be in the example-ex file.  Its not perfect\n" +
                             "\nYou first run this with example-ex and fix up example-ex until the translator log (in appdata) shows no errors\n"+
                             "Then you use translatereader to normalise the example-ex and all the other files\n"
                             );
                }

                return;
            }
            else if (arg1.Equals("translatereader"))
            {
                string primarypath = args.Next();
                int primarysearchdepth = args.Int();
                string primarylanguage = args.Next();
                string language2 = args.Next();
                string options = args.Next();

                if (primarypath == null || primarylanguage == null)
                {
                    Console.WriteLine("Usage:\n" +
                                        "translatereader path-language-files searchdepth language-to-use [secondary-language-to-compare] \n" +
                                        "Read the language-to-use and write out it into the same files cleanly\n" +
                                        "If secondary is present, read it, and use its definitions instead of the language-to-use\n" +
                                        "Write back out the tlf and tlp files to the current directory\n" +
                                        "Write out copy instructions to move those files back to their correct places\n" +
                                        "Example:n" +
                                        "eddtest translatereader c:\\code\\eddiscovery\\EDDiscovery\\Translations 2 example-ex deutsch-de \n" 
                                        );
                }
                else
                {
                    string ret = TranslateReader.Process(primarylanguage, primarypath, primarysearchdepth, language2, options);
                    Console.WriteLine(ret);
                }

                return;
            }
            else if (arg1.Equals("journal"))
            {
                Journal.JournalEntry(args);
                return;
            }
            else if (arg1.Equals("statusread"))
            {
                Status.StatusRead();
                return;
            }
            else if (arg1.Equals("statusmove"))
            {
                Status.StatusMove(args);
                return;
            }

            //*************************************************************************************************************
            // these require 1 arg min
            //*************************************************************************************************************

            if (args.Left < 1)
            {
                Console.WriteLine("Not enough arguments, please run without options for help");
            }
            else if (arg1.Equals("status"))
            {
                Status.StatusSet(args);
            }
            else if (arg1.Equals("dwwp"))
            {
                string text = File.ReadAllText(args.Next());
                StringParser sp = new StringParser(text);
                while (true)
                {
                    string notes = sp.NextWord("-");
                    if (notes == null)
                        break;

                    notes = notes.Trim();

                    if (sp.IsCharMoveOn('-'))
                    {
                        string reftext = sp.NextWord(":").Trim();

                        if (sp.IsCharMoveOn(':'))
                        {
                            string name = sp.NextWord("\r").Trim();

                            //                            Console.WriteLine("N: '" + name + "' Ref '" + reftext + "' loc '" + loc + "'");

                            QuickJSONFormatter json = new QuickJSONFormatter();
                            json.Object().V("Name", name).V("Notes", "DW3305->WPX " + notes).Close();

                            Console.WriteLine(json.Get() + ",");
                        }
                    }
                }
                return;
            }
            else if (arg1.Equals("eddbstars"))
            {
                EDDB.EDDBLog(args.Next(), "\"Star\"", "\"spectral_class\"", "Star class ");
            }
            else if (arg1.Equals("eddbplanets"))
            {
                EDDB.EDDBLog(args.Next(), "\"Planet\"", "\"type_name\"", "Planet class");
            }
            else if (arg1.Equals("eddbstarnames"))
            {
                EDDB.EDDBLog(args.Next(), "\"Star\"", "\"name\"", "Star Name");
            }
            else if (arg1.Equals("edsmstars"))
            {
                EDSMStars.Process(args);
            }
            else if (arg1.Equals("voicerecon"))
            {
                BindingsFile.Bindings(args.Next());
            }
            else if (arg1.Equals("devicemappings"))
            {
                BindingsFile.DeviceMappings(args.Next());
            }
            else if (arg1.Equals("phoneme"))
            {
                Speech.Phoneme(args.Next(), args.Next());
            }
            else if (arg1.Equals("corolisships"))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(args.Next(), "*.json", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                string ret = CorolisShips.ProcessShips(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("corolisship"))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(".", args.Next(), SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                string ret = CorolisShips.ProcessShips(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("corolismodules"))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(args.Next(), "*.json", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                string ret = CorolisModules.ProcessModules(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("corolismodule"))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(".", args.Next(), SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                string ret = CorolisModules.ProcessModules(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("frontierdata"))
            {
                FrontierData.Process(args.Next());
            }
            else if (arg1.Equals("journalindented"))
            {
                string path = args.Next();

                using (Stream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null)
                        {
                            JObject jo = new JObject();
                            try
                            {
                                jo = JObject.Parse(s);

                                Console.WriteLine(jo.ToString(Newtonsoft.Json.Formatting.Indented));
                            }
                            catch
                            {
                                Console.WriteLine("Unable to parse " + s);
                            }
                        }
                    }
                }

            }
            else if (arg1.Equals("jsonindented") || arg1.Equals("jsoncompressed"))
            {
                bool indent = arg1.Equals("jsonindented");

                string path = args.Next();
                try
                {
                    string text = File.ReadAllText(path);

                    using (StringReader sr = new StringReader(text))
                    {
                        string line;
                        while( (line= sr.ReadLine())!=null && (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape))
                        {
                            JToken tk = JToken.Parse(line);
                            Console.WriteLine(tk.ToString(indent ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None));
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed " + ex.Message);
                }
            }
            else if (arg1.Equals("specialreadjournals"))       // special one for coding only purposes - need to change code
            {
                string path = args.Next();
                string search = args.Next();

                FileInfo[] allFiles = Directory.EnumerateFiles(path, search, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                foreach (FileInfo f in allFiles)
                {
                    bool pname = false;

                    using (Stream fs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            string s;
                            while ((s = sr.ReadLine()) != null)
                            {
                                JObject jo = new JObject();
                                try
                                {
                                    jo = JObject.Parse(s);

                                    string ename = jo["event"].Str();

                                    if (ename == "Scan")
                                    {
                                        string bname = jo["BodyName"].Str();

                                        if (bname.Contains("Ring"))
                                        {
                                            if (pname == false)
                                            {
                                                pname = true;
                                                Console.WriteLine("--------------- FILE " + f.FullName);
                                            }
                                            Console.WriteLine(jo.ToString(Newtonsoft.Json.Formatting.Indented));
                                        }
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("Unable to parse " + s);
                                }
                            }
                        }
                    }
                }
            }
            else if (arg1.Equals("cutdownfile"))
            {
                string filename = args.Next();
                int numberlines = args.Int();

                using (StreamReader sr = new StreamReader(filename))         // read directly from file..
                {
                    using (StreamWriter wr = new StreamWriter(filename + ".out"))         // read directly from file..
                    {
                        for (int i = 0; i < numberlines; i++)
                        {
                            string line = sr.ReadLine();
                            if (line != null)
                                wr.WriteLine(line);
                            else
                                break;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Unknown command, run with empty line for help");
            }
        }

    }
}