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

            if (arg1.Equals("Status", StringComparison.InvariantCultureIgnoreCase))
            {
                Status.StatusSet(args);
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
            else if (arg1.Equals("jsonindented", StringComparison.InvariantCultureIgnoreCase))
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
            else if (arg1.Equals("readjournals", StringComparison.InvariantCultureIgnoreCase))
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
                              "Phoneme <filename> <fileout> for EDDI phoneme tx\n" +
                              "Voicerecon <filename>\n" +
                              "DeviceMappings <filename>\n" +
                              "StatusMove <various paras see entry>\n" +
                              "Status <Status flags>...  multiple ones are: supercruise, landed, fight (see code)\n" +
                              "CorolisModules rootfolder - process corolis-data\\modules\\<folder>\n" +
                              "CorolisModule name - process corolis-data\\modules\\<folder>\n" +
                              "CorolisShips rootfolder - process corolis-data\\ships\n" +
                              "CorolisShip name - process corolis-data\\ships\n" +
                              "Coroliseng rootfolder - process corolis-data\\modifications\n" +
                              "FrontierData rootfolder - process cvs file exports of frontier data\n" +
                              "scantranslate path filewildcard languagefilepath searchdepth language [opt]..- process source files and look for .Tx definitions\n" +
                              "                 path filewildcard is where the source files to search for .Tx is in \n" +
                              "                 languagefilepath is where the .tlf files are located - must include trailing \\ \n" +
                              "                 searchupdepth is the depth of search upwards (to root) to look thru folders for include files - 2 is normal\n" +
                              "                 language is the language to choose - in ISO format, such as fr\n" +
                              "                 Opt: Combine means don't repeat IDs if found in previous files\n" +
                              "                 Opt: ShowRepeats means show repeated entries in output\n" +
                              "                 Opt: ShowErrorsOnly means show only errors\n" +
                              "jsonindented file\n"
                              );

        }


    }
}