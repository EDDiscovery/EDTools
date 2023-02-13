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
using QuickJSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

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
                                  "Phoneme <filename> <fileout> for EDDI phoneme tx\n" +
                                  "Voicerecon <filename> -  Consol read a elite bindings file and output action script lines\n" +
                                  "DeviceMappings <filename> - read elite device pid/vid file for usb info\n" +
                                  "StatusMove lat long latstep longstep heading headstep steptime\n" +
                                  "Status - run for help\n" +
                                  "StatusRead\n" +
                                  "CoriolisModules rootfolder - process coriolis-data\\modules\\<folder>\n" +
                                  "CoriolisModule name - process coriolis-data\\modules\\<folder>\n" +
                                  "CoriolisShips rootfolder - process coriolis-data\\ships\n" +
                                  "CoriolisShip name - process coriolis-data\\ships\n" +
                                  "CoriolisEng rootfolder - process coriolis-data\\modifications\n" +
                                  "FrontierData rootfolder - process cvs file exports of frontier data\n" +
                                  "EDDIData - check vs EDDI data\n" +
                                  "scantranslate - process source files and look for .Tx definitions, run to see options\n" +
                                  "normalisetranslate- process language files and normalise, run to see options\n" +
                                  "scanforenums enumssemicolonlist path wildcard\n" +
                                  "journalindented file - read lines from file in journal format and output indented\n" +
                                  "jsonlines/jsonlinescompressed file - read a json on a single line from the file and output\n" +
                                  "json - read a json from file and output indented\n" +
                                  "jsontofluent file - read a json from file and output fluent code\n" +
                                  "journaltofluent file - read a journal file and output fluent code\n" +
                                  "escapestring - read a json from file and output text with quotes escaped for c# source files\n" +
                                  "@string - read a json from file and output text for @ strings\n" +
                                  "cutdownfile file lines -reduce a file size down to this number of lines\n" +
                                  "xmldump file - decode xml and output attributes/elements showing structure\n" +
                                  "dwwp file - for processing captured html on expeditions and outputing json of stars\n" +
                                  "svg file - read svg file of Elite regions and output EDSM JSON galmap file\n" +
                                  "readlog file - read a continuous log or journal file out to stdout\n" +
                                  "githubrelease - read the releases list and stat it\n" +
                                  "logs wildcard - read files for json lines and process\n" +
                                  "readjournals path - read all .log journal files and check - need code changes\n" +
                                  "csvtocs path - read csv and turn into a cs class\n"+
                                  "comments path\n" +
                                  "mddoc path wildcard [REMOVE]\n" +
                                  "finddoclinks path wildcard [REMOVE]\n" +
                                  "finddoclinks path wildcard typename existingdocexternfile searchstr\n" +
                                  "insertext path wildcard find insert\n" 

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
            else if (arg1.Equals("finddoclinks"))
            {
                // used to search the md files from default documentation for links to external objects
                // paras: . *.md OpenTK.Graphics.OpenGL c:\code\ofc\ofc\docexternlinks.txt opengl

                string path = args.Next();
                string wildcard = args.Next();
                string typename = args.Next();
                string existingfile = args.Next();
                string repname = args.Next();

                if (path != null && wildcard != null && typename != null)
                {
                    FileInfo[] allFiles = Directory.EnumerateFiles(path, wildcard, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                    MDDoc.FindDocLinks(allFiles, typename, existingfile, repname);
                }
                else
                {
                    Console.WriteLine("Usage:\n" + "path wildcard typename defaultdoclinkfile replacementname\n" + "Find all doc links with typename, and add to defaultdoclink, with replacementname\n"
                             );
                }

                return;
            }
            else if (arg1.Equals("mddoc"))      // processes MD DOC for wiki and makes it better
            {
                string path = args.Next();
                string wildcard = args.Next();
                bool removerootnamespace = args.NextEmpty().Contains("REMOVE",StringComparison.InvariantCultureIgnoreCase);

                if (path != null && wildcard != null )
                {
                    FileInfo[] allFiles = Directory.EnumerateFiles(path, wildcard, SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                    MDDoc.PostProcessMD(allFiles, removerootnamespace);
                }
                else
                {
                    Console.WriteLine("Usage:\n" + "Path wildcard [REMOVE]\n" + "Processes MDDocs from default documentation and makes formatting better\n"
                             );
                }

                return;
            }

            else if (arg1.Equals("inserttext"))
            {
                string path = args.Next();
                string wildcard = args.Next();
                string find = args.Next();
                string insert = args.Next();

                if (path != null && wildcard != null && find != null && insert != null )
                {
                    FileInfo[] allFiles = Directory.EnumerateFiles(path, wildcard, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                    InsertText.ProcessInsert(allFiles, find, insert);
                }
                else
                {
                    Console.WriteLine("Usage:\n" + "path wildcard findstring insertbeforestring"
                             );
                }

                return;
            }

            else if (arg1.Equals("normalisetranslate"))
            {
                string primarypath = args.Next();
                int primarysearchdepth = args.Int();
                string primarylanguage = args.Next();
                string language2 = args.Next();
                string options = args.Next();
                string renamefile = args.Next();
                string enums = args.Next();

                if (primarypath == null || primarylanguage == null)
                {
                    Console.WriteLine("Usage:\n" +
                                        "normalisetranslate path-language-files searchdepth language-to-use [secondary-language-to-compare or - for none] [options] [renamefile] [semicolon-list-of-enum-files]\n" +
                                        "Read the language-to-use and write out it into the same files cleanly\n" +
                                        "If secondary is present, read it, and use its definitions instead of the language-to-use. Use its filename for output files\n" +
                                        "Options: single string: Use NS for don't report secondary, NoOutput for no files written (excepting report.lst)\n" +
                                        "Rename file: List of lines with orgID | RenamedID to note renaming of existing IDs (if does not exist won't stop)\n" +
                                        "Unless NoOutput, Write back out the tlf and tlp files to the current directory, and \n" +
                                        "write out copy.bat instructions to move those files back to their correct places\n" +
                                        "semicolon-list-of-enums-files give list of enumerations to cross check against. Use stdenums for built in EDD list\n" +
                                        "Always write report.txt\n" +
                                        "Example:\n" +
                                        "eddtest normalisetranslate c:\\code\\eddiscovery\\EDDiscovery\\Translations 2 example-ex deutsch-de \n"
                                        );
                }
                else
                {
                    string ret = NormaliseTranslationFiles.Process(primarylanguage, primarypath, primarysearchdepth, language2, options, renamefile, enums);
                    Console.WriteLine(ret);
                    System.Diagnostics.Debug.WriteLine(ret);
                }

                return;
            }
            else if (arg1.Equals("translationrepeats"))
            {
                string primarypath = args.Next();
                int primarysearchdepth = args.Int();

                if (primarypath == null )
                {
                    Console.WriteLine("Usage:\n" +
                                        "translaterepeats ..\n"
                                        );
                }
                else
                {
                    string ret = TranslationFileRepeats.Process(primarypath, primarysearchdepth);
                    Console.WriteLine(ret);
                    System.Diagnostics.Debug.WriteLine(ret);
                }

                return;
            }
            else if (arg1.Equals("scanforenums"))
            {
                string enums = args.Next();
                string primarypath = args.Next();
                string primarysearch = args.Next();

                if (enums == null || primarypath == null || primarysearch == null)
                {
                    Console.WriteLine("Usage:\n" +
                                    "scanforenums enumfilelist path searchdepth \n" +
                                    "semicolon-list-of-enums-files give list of enumerations to cross check against. Use stdenums for built in EDD list\n" +
                                    "Write report.txt with results\n"
                                    );
                }
                else
                {
                    FileInfo[] allFiles = Directory.EnumerateFiles(primarypath, primarysearch, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                    Enums.ScanForEnums(enums, allFiles);
                    return;
                }
            }
            else if (arg1.Equals("journal"))
            {
                Journal.JournalEntry(args);
                return;
            }
            else if (arg1.Equals("statusread"))
            {
                string file = "status.json";
                if (args.Left >= 1)
                    file = args.Next();
                Status.StatusRead(file);
                return;
            }
            else if (arg1.Equals("statusmove"))
            {
                Status.StatusMove(args);
                return;
            }
            else if (arg1.Equals("status"))
            {
                Status.StatusSet(args);
                return;
            }
            else if (arg1.Equals("eddidata"))
            {
                EliteDangerousCore.MaterialCommodityMicroResourceType.FillTable();      // for the future
                EDDIData.Process();
            }

            //*************************************************************************************************************
            // these require 1 arg min
            //*************************************************************************************************************

            if (args.Left < 1)
            {
                Console.WriteLine("Not enough arguments, please run without options for help");
            }
            else if (arg1.Equals("githubreleases"))
            {
                string file = args.Next();
                GitHub.Stats(file);
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

                            JSONFormatter json = new JSONFormatter();
                            json.Object().V("Name", name).V("Notes", "DW3305->WPX " + notes).Close();

                            Console.WriteLine(json.Get() + ",");
                        }
                    }
                }
                return;
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
            else if (arg1.Equals("wikiconvert"))
            {
                WikiConvert.Convert(args.Next(), args.Next());
            }
            else if (arg1.Equals("coriolisships"))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(args.Next(), "*.json", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                string ret = CoriolisShips.ProcessShips(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("coriolisship"))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(".", args.Next(), SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                string ret = CoriolisShips.ProcessShips(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("coriolismodules"))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(args.Next(), "*.json", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                string ret = CoriolisModules.ProcessModules(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("coriolismodule"))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(".", args.Next(), SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                string ret = CoriolisModules.ProcessModules(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("corioliseng"))
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(args.Next(), "*.json", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                string ret = CoriolisEng.ProcessEng(allFiles);
                Console.WriteLine(ret);
            }
            else if (arg1.Equals("frontierdata"))
            {
                EliteDangerousCore.MaterialCommodityMicroResourceType.FillTable();
                FrontierData.Process(args.Next());
            }
            else if (arg1.Equals("readjournals"))
            {
                JournalReader.ReadJournals(args.Next());
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

                                Console.WriteLine(jo.ToString(true));
                            }
                            catch
                            {
                                Console.WriteLine("Unable to parse " + s);
                            }
                        }
                    }
                }

            }
            else if (arg1.Equals("jsonlines") || arg1.Equals("jsonlinescompressed"))
            {
                bool indent = arg1.Equals("jsonindented");

                string path = args.Next();
                try
                {
                    string text = File.ReadAllText(path);

                    using (StringReader sr = new StringReader(text))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null && (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape))
                        {
                            JToken tk = JToken.Parse(line, out string err, JToken.ParseOptions.CheckEOL);
                            if (tk != null)
                                Console.WriteLine(tk.ToString(indent));
                            else
                                Console.WriteLine($"Error in JSON {err}");
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed " + ex.Message);
                }
            }
            else if (arg1.Equals("json"))
            {
                string path = args.Next();
                try
                {
                    string text = File.ReadAllText(path);

                    JToken tk = JToken.Parse(text, out string err, JToken.ParseOptions.CheckEOL);
                    if (tk != null)
                        Console.WriteLine(tk.ToString(true));
                    else
                        Console.WriteLine($"{err}\r\nERROR in JSON");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed " + ex.Message);
                }
            }
            else if (arg1.Equals("jsontofluent"))
            {
                string path = args.Next();
                try
                {

                    string text = File.ReadAllText(path);

                    JToken tk = JToken.Parse(text, out string err, JToken.ParseOptions.CheckEOL);
                    if (tk != null)
                    {
                        Console.WriteLine(tk.ToString(true));
                        string code = "";
                        JSONToFluent(tk, ref code, true, true);
                        System.Diagnostics.Debug.WriteLine(code);
                        Console.WriteLine("qj" + code);
                    }
                    else
                        Console.WriteLine($"{err}\r\nERROR in JSON");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed " + ex.Message);
                }
            }
            else if (arg1.Equals("journaltofluent"))
            {
                string path = args.Next();
                try
                {
                    string[] text = File.ReadAllLines(path);

                    foreach (var l in text)
                    {
                        JToken tk = JToken.Parse(l, out string err, JToken.ParseOptions.CheckEOL);
                        if (tk != null)
                        {
                            Console.WriteLine(tk.ToString(true));
                            string code = "";
                            JSONToFluent(tk, ref code, true, true);
                            System.Diagnostics.Debug.WriteLine(code);
                            Console.WriteLine("qj" + code);
                        }
                        else
                            Console.WriteLine($"{err}\r\nERROR in JSON");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed " + ex.Message);
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
            else if (arg1.Equals("readlog"))
            {
                string filename = args.Next();
                long pos = 0;
                long lineno = 0;


                if (Path.GetFileName(filename).Contains("*"))
                {
                    string dir = Path.GetDirectoryName(filename);
                    if (dir == "")
                        dir = ".";
                    FileInfo[] allFiles = Directory.EnumerateFiles(dir, Path.GetFileName(filename), SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderByDescending(p => p.LastWriteTime).ToArray();
                    if (allFiles.Length > 1)
                        filename = allFiles[0].FullName;
                }

                Console.WriteLine("Reading " + filename);

                while (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape)
                {
                    try
                    {
                        if (new FileInfo(filename).Length < pos)
                        {
                            Console.WriteLine("");
                            Console.WriteLine("########################################################################################");
                            Console.WriteLine("");
                            pos = 0;
                            lineno = 0;
                        }

                        var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                        stream.Seek(pos, SeekOrigin.Begin);

                        using (var sr = new StreamReader(stream))
                        {
                            string line = null;
                            while ((line = sr.ReadLine()) != null)
                            {
                                Console.Write(string.Format("{0}:", ++lineno));
                                Console.WriteLine(line);
                            }

                            pos = stream.Position;
                        }
                    }
                    catch
                    {

                    }

                    System.Threading.Thread.Sleep(50);
                }
            }
            else if (arg1.Equals("xmldump"))
            {
                string filename = args.Next();

                XElement bindings = XElement.Load(filename);
                Dump(bindings, 0);
            }
            else if (arg1.Equals("escapestring"))
            {
                string filename = args.Next();

                string text = File.ReadAllText(filename);
                text = text.QuoteString();
                Console.WriteLine(text);
            }
            else if (arg1.Equals("@string"))
            {
                string filename = args.Next();

                string text = File.ReadAllText(filename);
                text = text.Replace("\"", "\"\"");
                Console.WriteLine(text);
            }
            else if (arg1.Equals("svg"))
            {
                string filename = args.Next();
                int id = 0;

                string[] names = new string[] {
                "Galactic Centre",
                "Empyrean Straits",
                "Ryker's Hope",
                "Odin's Hold",
                "Norma Arm",
                "Arcadian Stream",
                "Izanami",
                "Inner Orion-Perseus Conflux",
                "Inner Scutum-Centaurus Arm",
                "Norma Expanse",
                "Trojan Belt",
                "The Veils",
                "Newton's Vault",
                "The Conduit",
                "Outer Orion-Perseus Conflux",
                "Orion-Cygnus Arm",
                "Temple",
                "Inner Orion Spur",
                "Hawking's Gap",
                "Dryman's Point",
                "Sagittarius-Carina Arm",
                "Mare Somnia",
                "Acheron",
                "Formorian Frontier",
                "Hieronymus Delta",
                "Outer Scutum-Centaurus Arm",
                "Outer Arm",
                "Aquila's Halo",
                "Errant Marches",
                "Perseus Arm",
                "Formidine Rift",
                "Vulcan Gate",
                "Elysian Shore",
                "Sanguineous Rim",
                "Outer Orion Spur",
                "Achilles's Altar",
                "Xibalba",
                "Lyra's Song",
                "Tenebrae",
                "The Abyss",
                "Kepler's Crest",
                "The Void", };


                JSONFormatter qjs = new JSONFormatter();
                qjs.Array(null).LF();

                XElement bindings = XElement.Load(filename);
                foreach (XElement x in bindings.Descendants())
                {
                    if (x.HasAttributes)
                    {
                        foreach (XAttribute y in x.Attributes())
                        {
                            if (x.Name.LocalName == "path" && y.Name.LocalName == "d")
                            {
                                //Console.WriteLine(x.Name.LocalName + " attr " + y.Name + " = " + y.Value);
                                var points = BaseUtils.SVG.ReadSVGPath(y.Value);

                                qjs.Object().V("id", id).V("type", "region").V("name", names[id]);
                                qjs.Array("coordinates");
                                foreach (var p in points)
                                {
                                    qjs.Array(null).V(null, p.X * 100000.0 / 2048.0 - 49985).V(null, 0).V(null, p.Y * 100000.0 / 2048.0 - 24105).Close();
                                }
                                qjs.Close().Close().LF();
                                id++;
                            }
                        }
                    }
                }

                qjs.Close();

                Console.WriteLine(qjs.ToString());
            }
            else if (arg1.Equals("logs"))
            {
                string filename = args.Next();

                FileInfo[] allFiles = Directory.EnumerateFiles(".", filename, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                Dictionary<string, string> signals = new Dictionary<string, string>();

                foreach (var f in allFiles)
                {
                    string text = FileHelpers.TryReadAllTextFromFile(f.FullName);
                    if (text != null)
                    {
                        StringReader sr = new StringReader(text);
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            JToken tk = JToken.Parse(line);
                            if (tk != null)
                            {
                                var sn = tk["SignalName"];
                                //  Console.WriteLine("Read " + tk.ToString());
                                if (sn != null)
                                {
                                    string str = tk["SignalName"].Str();
                                    string strl = tk["SignalName_Localised"].Str();
                                    if (tk["IsStation"].Bool(false) == true)
                                    {
                                        if (strl.HasChars())
                                            Console.WriteLine("***** Station has localisation");

                                        strl = "STATION";
                                    }
                                    if (signals.ContainsKey(str))
                                    {
                                        if (signals[str] != strl)
                                            Console.WriteLine("***** Clash, {0} {1} vs {2}", str, strl, signals[str]);
                                    }
                                    else
                                    {
                                        signals[str] = strl;
                                    }
                                }
                            }
                        }
                    }

                }

                Console.WriteLine("***************************** ID list");
                foreach (string v in signals.Keys)
                {
                    if (signals[v] != "STATION" && signals[v].HasChars())
                        Console.WriteLine("{0} {1}", v, signals[v]);
                }
            }
            else if (arg1.Equals("csvtocs"))
            {
                string filename = args.Next();
                string precode = args.Next();
                CSVFile reader = new CSVFile();

                if (reader.Read(filename))
                {
                    foreach (var row in reader.Rows)
                    {
                        string line = "";
                        foreach (var cell in row.Cells)
                        {
                            if (cell.InvariantParseDoubleNull() != null || cell.InvariantParseLongNull() != null)
                            {
                                line = line.AppendPrePad(cell, ",");
                            }
                            else
                            {
                                string celln = cell.Replace("_Name;", "");

                                line = line.AppendPrePad(celln.AlwaysQuoteString(), ",");
                            }
                        }

                        Console.WriteLine($"    {precode}({line}),");
                    }
                }

            }
            else if (arg1.Equals("mergecsharp"))
            {
                MergeCSharp.Merge(args);
            }
            else
            {
                Console.WriteLine("Unknown command, run with empty line for help");
            }
        }


        static void Dump( XElement x, int level)
        {
            string pretext = "                                       ".Substring(0, level * 3);
            Console.WriteLine(level + pretext + x.NodeType + " " + x.Name.LocalName + (x.Value.HasChars() ? (" : " + x.Value) : ""));

            if (x.HasAttributes)
            {
                foreach (XAttribute y in x.Attributes())
                {
                    Console.WriteLine(level + pretext + "  attr " + y.Name + " = " + y.Value);
                }
            }

            if (x.HasElements)
            {
                foreach (XElement y in x.Elements())
                {
                    //Console.WriteLine(level + pretext + x.Name.LocalName + " desc " + y.Name.LocalName);
                    Dump(y, level + 1);
                    //Console.WriteLine(level + pretext + x.Name.LocalName + " Out desc " + y.Name.LocalName);
                }
            }

        }

        public static void JSONToFluent(JToken tk, ref string code, bool indent, bool converttimestamp)
        {
            if (tk.IsObject)
            {
                if (indent)
                    code = code.NewLine();

                if (tk.IsProperty)
                    code += ".Object(" + tk.Name.AlwaysQuoteString() + ")";
                else
                    code += ".Object()";

                foreach (var kvp in tk.Object())
                {
                    JSONToFluent(kvp.Value, ref code, indent,converttimestamp);
                }

                code += ".Close()";
                if (indent)
                    code = code.NewLine();
            }
            else if (tk.IsArray)
            {
                if (indent)
                    code = code.NewLine();

                if (tk.IsProperty)
                    code += ".Array(" + tk.Name.AlwaysQuoteString() + ")";
                else
                    code += ".Array()";

                foreach (var v in tk.Array())
                {
                    JSONToFluent(v, ref code, indent,converttimestamp);
                }

                code += ".Close()";

                if (indent)
                    code = code.NewLine();
            }
            else
            {
                string vstring = tk.ToString();

                if (tk.IsProperty)
                {
                    if ( converttimestamp && tk.Name.Equals("timestamp"))
                    {
                        code += ".UTC(" + tk.Name.AlwaysQuoteString() + ")";
                    }
                    else
                        code += ".V(" + tk.Name.AlwaysQuoteString() + "," + vstring + ")";
                }
                else
                    code += ".V(" + vstring + ")";
            }
        }


    }
}