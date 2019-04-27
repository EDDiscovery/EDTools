using BaseUtils;
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

            int repeatdelay = 0;

            while (true) // read optional args
            {
                string opt = (args.Left > 0) ? args[0] : null;

                if (opt != null)
                {
                    if (opt.Equals("-keyrepeat", StringComparison.InvariantCultureIgnoreCase))
                    {
                        repeatdelay = -1;
                        args.Remove();
                    }
                    else if (opt.Equals("-repeat", StringComparison.InvariantCultureIgnoreCase) && args.Left >= 1)
                    {
                        args.Remove();
                        if (!int.TryParse(args.Next(), out repeatdelay))
                        {
                            Console.WriteLine("Bad repeat delay\n");
                            return;
                        }
                    }
                    else
                        break;
                }
                else
                    break;
            }

            string arg1 = args.Next();

            if (arg1 == null)
            {
                Help();
                return;
            }

            if (arg1.Equals("StatusMove", StringComparison.InvariantCultureIgnoreCase))
            {
                Status.StatusMove(args);
                return;
            }

            if (arg1.Equals("StatusRead", StringComparison.InvariantCultureIgnoreCase))
            {
                Status.StatusRead(args);
                return;
            }

            if (arg1.Equals("Status", StringComparison.InvariantCultureIgnoreCase))
            {
                Status.StatusSet(args);
                return;
            }


            if (arg1.Equals("DWWP", StringComparison.InvariantCultureIgnoreCase))
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

                            Console.WriteLine(json.Get()+ "," );
                        }
                    }
                }
                return;
            }

            if (args.Left < 1)
            {
                Help();
                return;
            }

            if (arg1.Equals("EDDBSTARS", StringComparison.InvariantCultureIgnoreCase))
            {
                EDDB.EDDBLog(args.Next(), "\"Star\"", "\"spectral_class\"", "Star class ");
            }
            else if (arg1.Equals("EDDBPLANETS", StringComparison.InvariantCultureIgnoreCase))
            {
                EDDB.EDDBLog(args.Next(), "\"Planet\"", "\"type_name\"", "Planet class");
            }
            else if (arg1.Equals("EDDBSTARNAMES", StringComparison.InvariantCultureIgnoreCase))
            {
                EDDB.EDDBLog(args.Next(), "\"Star\"", "\"name\"", "Star Name");
            }
            else if (arg1.Equals("EDSMStars", StringComparison.InvariantCultureIgnoreCase))
            {
                EDSMStars.Process(args);
            }
            else if (arg1.Equals("voicerecon", StringComparison.InvariantCultureIgnoreCase))
            {
                BindingsFile.Bindings(args.Next());
            }
            else if (arg1.Equals("devicemappings", StringComparison.InvariantCultureIgnoreCase))
            {
                BindingsFile.DeviceMappings(args.Next());
            }
            else if (arg1.Equals("Phoneme", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Left >= 1)
                    Speech.Phoneme(args.Next(), args.Next());
            }
            else if (arg1.Equals("Corolisships", StringComparison.InvariantCultureIgnoreCase))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(args.Next(), "*.json", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();


                string ret = CorolisShips.ProcessShips(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("Corolisship", StringComparison.InvariantCultureIgnoreCase))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(".", args.Next(), SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();


                string ret = CorolisShips.ProcessShips(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("Corolismodules", StringComparison.InvariantCultureIgnoreCase))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(args.Next(), "*.json", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                string ret = CorolisModules.ProcessModules(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("Corolismodule", StringComparison.InvariantCultureIgnoreCase))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(".", args.Next(), SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                string ret = CorolisModules.ProcessModules(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("FrontierData", StringComparison.InvariantCultureIgnoreCase))
            {
                FrontierData.Process(args.Next());
            }
            else if (arg1.Equals("scantranslate", StringComparison.InvariantCultureIgnoreCase))
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
                    bool combine = false;
                    bool showrepeat = false;
                    bool showerrorsonly = false;

                    while (args.More)
                    {
                        string a = args.Next().ToLowerInvariant();
                        if (a == "combine")
                            combine = true;
                        if (a == "showrepeats")
                            showrepeat = true;
                        if (a == "showerrorsonly")
                            showerrorsonly = true;
                    }

                    string ret = ScanTranslate.Process(allFiles, lang, txpath, txsearchdepth, combine, showrepeat , showerrorsonly);
                    Console.WriteLine(ret);
                }
            }
            else if (arg1.Equals("translatereader", StringComparison.InvariantCultureIgnoreCase))
            {
                string primarypath = args.Next();
                int primarysearchdepth = args.Int();
                string primarylanguage = args.Next();
                string language2 = args.Next();
                string options = args.Next();

                string ret = TranslateReader.Process(primarylanguage, primarypath, primarysearchdepth , language2, options);
                Console.WriteLine(ret);
            }

            else if (arg1.Equals("journalindented", StringComparison.InvariantCultureIgnoreCase))
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
            else if (arg1.Equals("jsonindented", StringComparison.InvariantCultureIgnoreCase) || arg1.Equals("jsoncompressed", StringComparison.InvariantCultureIgnoreCase))
            {
                bool indent = arg1.Equals("jsonindented", StringComparison.InvariantCultureIgnoreCase);

                string path = args.Next();
                try
                {
                    string text = File.ReadAllText(path);
                    JToken tk = JToken.Parse(text);
                    Console.WriteLine("Output:" + Environment.NewLine + tk.ToString(indent ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None));
                }
                catch( Exception ex )
                {
                    Console.WriteLine("Failed " + ex.Message);
                }
            }
            else if (arg1.Equals("specialreadjournals", StringComparison.InvariantCultureIgnoreCase))       // special one for coding only purposes - need to change code
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

                                    if ( ename == "Scan")
                                    {
                                        string bname = jo["BodyName"].Str();

                                        if ( bname.Contains("Ring"))
                                        {
                                            if ( pname == false)
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
            else if(arg1.Equals("cutdownfile", StringComparison.InvariantCultureIgnoreCase))
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
                Journal.JournalEntry(arg1, args.Next(), args, repeatdelay);
            }
        }

        static void Help()
        {
            Console.WriteLine("[-keyrepeat]|[-repeat ms]\n" +
                             Journal.Help() +
                              "EDDBSTARS <filename> or EDDBPLANETS or EDDBSTARNAMES for the eddb dump\n" +
                              "EDSMSTARS <filename> read the main dump and analyse\n" +
                              "Phoneme <filename> <fileout> for EDDI phoneme tx\n" +
                              "Voicerecon <filename>\n" +
                              "DeviceMappings <filename>\n" +
                              "StatusMove lat long latstep longstep heading headstep steptime\n" +
                              "Status <Status flags>... UI <Flags>,normal,supercruise, landed, SRV, fight C:cargo F:fuel FG:Firegroup G:Gui L:Legalstate\n" +
                              "StatusRead user\n"+
                              "CorolisModules rootfolder - process corolis-data\\modules\\<folder>\n" +
                              "CorolisModule name - process corolis-data\\modules\\<folder>\n" +
                              "CorolisShips rootfolder - process corolis-data\\ships\n" +
                              "CorolisShip name - process corolis-data\\ships\n" +
                              "Coroliseng rootfolder - process corolis-data\\modifications\n" +
                              "FrontierData rootfolder - process cvs file exports of frontier data\n" +
                              "scantranslate path filewildcard languagefilepath searchdepth language [opt]..- process source files and look for .Tx definitions\n" +
                              "                 path filewildcard is where the source files to search for .Tx is in \n" +
                              "                 languagefilepath is where the .tlf files are located\n" +
                              "                 searchupdepth is the depth of search upwards (to root) to look thru folders for include files - 2 is normal\n" +
                              "                 language is the language to compare against - example-ex etc\n" +
                              "                 Opt: Combine means don't repeat IDs if found in previous files\n" +
                              "                 Opt: ShowRepeats means show repeated entries in output\n" +
                              "                 Opt: ShowErrorsOnly means show only errors\n" +
                              "translatereader path-language-files searchdepth language-to-use [secondary-language-to-compare] \n"+
                              "                Read the language-to-use and write out it into the same files cleanly\n" +
                              "                if secondary is present, read it, and use its definitions instead of the language-to-use\n" +
                              "                write back out the tlf and tlp files to the current directory\n" +
                              "                write out copy instructions to move those files back to their correct places\n" +
                              "journalindented file - read lines from file in journal format and output indented\n" +
                              "jsonindented file - read a json in file and indent\n" +
                              "jsoncompressed file - read a json in file and compress\n" +
                              "cutdownfile file lines\n" +
                              "dwwp file\n"
                              );

        }


    }
}