/*
 * Copyright © 2015 - 2025 robbyxp @ github.com
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
 */

using BaseUtils;
using QuickJSON;
using System;
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

            if ( args.Left == 0)
            {
                Console.WriteLine("Journal: Journal - write journal with an event, run for help\n"+
                                  "         journalindented file - read lines from file in journal format and output indented\n" +
                                  "         journalplay file file - read journal file and play into another file line by line, altering timestamp\n" +
                                  "         journalanalyse type path filenamewildcard - read all .log journal files and check - see code for type\n" +
                                  "         journaltofluent file - read a journal file and output fluent code\n" +
                                  "         readjournallogs file - read a continuous log or journal file out to stdout\n" +

                                  "JSON:    jsonlines/jsonlinescompressed file - read a json on a single line from the file and output\n" +
                                  "         json - read a json from file and output indented\n" +
                                  "         @json <verbose1/0> <@string1/0> -read json lines from file and output text in verbose or not. Optionally @ the strings. Non verbose has 132 line length limit\n" +
                                  "         jsontofluent file - read a json from file and output fluent code\n" +

                                  "MDDocs:  mddoc path wildcard [REMOVE] - process MDDOCS from output of doc tool and clean up\n" +
                                  "         finddoclinks path wildcard [REMOVE]\n" +
                                  "         finddoclinks path wildcard typename existingdocexternfile searchstr\n" +
                                  "Translx: normalisetranslate- process language files and normalise, run to see options\n" +
                                  "         scanforenums enumssemicolonlist path wildcard\n" +

                                  "Coriolis:CoriolisModules rootfolder - process coriolis-data\\modules\\<folder>\n" +
                                  "         CoriolisModule name - process coriolis-data\\modules\\<folder>\n" +
                                  "         CoriolisShips rootfolder - process coriolis-data\\ships\n" +
                                  "         CoriolisShip name - process coriolis-data\\ships\n" +
                                  "         CoriolisEng rootfolder - process coriolis-data\\modifications\n" +

                                  "Frontier:FrontierData rootfolder - process cvs file exports of frontier data\n" +
                                  "         FDEVIDs rootfolder - process cvs file exports of EDCD FDEV IDs\n" +
                                  "         Voicerecon <filename> -  Console read a elite bindings file and output action script lines\n" +
                                  "         outfitting file - Read outfitting.csv and compare vs item data\n" +
                                  "         DeviceMappings <filename> - read elite device pid/vid file for usb info\n" +

                                  "Websites:EDDIData - check vs EDDI data\n" +
                                  "         ArtieData - check vs Artie data\n" +
                                  "         edsy filename itemsmodulefilepathtosyncto - read a dump of EDSY EDDB file and process into ItemsModules.cs\n"+
                                  "         Phoneme <filename> <fileout> for EDDI phoneme tx\n" +
                                  "         githubrelease - read the releases list and stat it\n" +

                                  "Status:  Status - run for help\n" +
                                  "         StatusRead\n" +
                                  "         StatusMove lat long latstep longstep heading headstep steptime\n" +

                                  "Files:   escapestring - read file and output text with quotes escaped for c# source files\n" +
                                  "         @string - read a file and output text for @ strings\n" +
                                  "         xmldump file - decode xml and output attributes/elements showing structure\n" +
                                  "         cutdownfile file lines -reduce a file size down to this number of lines\n" +
                                  "         mergecsharp files... - merge CS files\n" +
                                  "         inserttext path wildcard find insert : If insert = \"\" then lines are removed\n",
                                  "         wikiconvert path filespecwildcard\n",
                                  "         corrupt path\n",
                                  "         svg file - read svg file of Elite regions and output EDSM JSON galmap file\n" +
                                  ""

                                  );

                return;
            }

            while (true)
            {
                string cmd = args.NextLI();

                if (cmd == null)
                    break;

                #region DOCS

                else if (cmd.Equals("finddoclinks"))
                {
                    if (args.Left >= 5)
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
                    }
                    else
                    { 
                        Console.WriteLine($"Too few args for {cmd}"); break; 
                    }
                }
                else if (cmd.Equals("mddoc"))      // processes MD DOC for wiki and makes it better
                {
                    if (args.Left >= 2)
                    {
                        string path = args.Next();
                        string wildcard = args.Next();
                        bool removerootnamespace = args.NextEmpty().Contains("REMOVE", StringComparison.InvariantCultureIgnoreCase);

                        if (path != null && wildcard != null)
                        {
                            FileInfo[] allFiles = Directory.EnumerateFiles(path, wildcard, SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                            MDDoc.PostProcessMD(allFiles, removerootnamespace);
                        }
                        else
                        {
                            Console.WriteLine("Usage:\n" + "Path wildcard [REMOVE]\n" + "Processes MDDocs from default documentation and makes formatting better\n"
                                     );
                        }
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }

                #endregion

                #region Translate

                else if (cmd.Equals("normalisetranslate"))
                {
                    if (args.Left >= 3)
                    {
                        string primarypath = args.Next();       // mandatory
                        int primarysearchdepth = args.Int();    // mandatory
                        string primarylanguage = args.Next();   // mandatory

                        string language2 = args.Next();         // optional..
                        string renamefile = args.Next();
                        string enums = args.Next();

                        if (primarypath == null || primarylanguage == null)
                        {
                            Console.WriteLine("Usage:\n" +
                                                "normalisetranslate path-language-files searchdepth language-to-use [secondary-language-to-compare or - for none] [renamefile or -] [semicolon-list-of-enum-files]\n" +
                                                "Read the language-to-use and check it\n" +
                                                "secondary-language: Read this language and overwrite the secondary files with normalised versions against the first, but carrying over the translations\n" +
                                                "Rename file: List of lines with orgID | RenamedID to note renaming of existing IDs\n" +
                                                "semicolon-list-of-enums-files: give list of enumerations to cross check against. Use c:\\code\\eddiscovery for built in EDD list of translator enums\n" +
                                                "Always write report.txt\n" +
                                                "Example:\n" +
                                                "eddtest normalisetranslate c:\\code\\eddiscovery\\EDDiscovery\\Translations 2 example-ex deutsch-de \n"
                                                );
                        }
                        else
                        {
                            string ret = NormaliseTranslationFiles.ProcessNew(primarylanguage, primarypath, primarysearchdepth, language2, renamefile, enums);
                            Console.WriteLine(ret);
                            System.Diagnostics.Debug.WriteLine(ret);
                        }
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }
                else if (cmd.Equals("scanforenums"))
                {
                    if (args.Left >= 3)
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
                    else
                    { 
                        Console.WriteLine($"Too few args for {cmd}"); break; 
                    }
                }

                #endregion

                #region Coriolis
                else if (cmd.Equals("coriolisships"))
                {
                    if (args.Left >= 1)
                    {
                        FileInfo[] allFiles = Directory.EnumerateFiles(args.Next(), "*.json", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                        string ret = CoriolisShips.ProcessShips(allFiles);
                        Console.WriteLine(ret);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }
                else if (cmd.Equals("coriolisship"))
                {
                    if (args.Left >= 1)
                    {
                        FileInfo[] allFiles = Directory.EnumerateFiles(".", args.Next(), SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                        string ret = CoriolisShips.ProcessShips(allFiles);
                        Console.WriteLine(ret);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }
                else if (cmd.Equals("coriolismodules"))
                {
                    if (args.Left >= 1)
                    {
                        FileInfo[] allFiles = Directory.EnumerateFiles(args.Next(), "*.json", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                        string ret = CoriolisModules.ProcessModules(allFiles);
                        Console.WriteLine(ret);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }
                else if (cmd.Equals("coriolismodule"))
                {
                    if (args.Left >= 1)
                    {
                        FileInfo[] allFiles = Directory.EnumerateFiles(".", args.Next(), SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                        string ret = CoriolisModules.ProcessModules(allFiles);
                        Console.WriteLine(ret);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }
                else if (cmd.Equals("corioliseng"))
                {
                    if (args.Left >= 1)
                    {
                        FileInfo[] allFiles = Directory.EnumerateFiles(args.Next(), "*.json", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();
                        string ret = CoriolisEng.ProcessEng(allFiles);
                        Console.WriteLine(ret);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }

                #endregion

                #region Frontier
                else if (cmd.Equals("frontierdata"))
                {
                    if (args.Left >= 1)
                    {
                        EliteDangerousCore.MaterialCommodityMicroResourceType.FillTable();
                        FrontierData.Process(args.Next());
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }
                else if (cmd.Equals("fdevids"))
                {
                    if (args.Left >= 1)
                    {
                        EliteDangerousCore.MaterialCommodityMicroResourceType.FillTable();
                        FDevIDs.Process(args.Next());
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }

                else if (cmd.Equals("voicerecon"))
                {
                    if (args.Left >= 1)
                    {
                        BindingsFile.Bindings(args.Next());
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }
                else if (cmd.Equals("outfitting"))
                {
                    if (args.Left >= 1)
                    {
                        string filename = args.Next();
                        EDCDOutfitting.Process(filename);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }
                else if (cmd.Equals("devicemappings"))
                {
                    if (args.Left >= 1)
                    {
                        BindingsFile.DeviceMappings(args.Next());
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }

                #endregion

                #region WebSiteData

                else if (cmd.Equals("eddidata"))
                {
                    EliteDangerousCore.MaterialCommodityMicroResourceType.FillTable();      // for the future
                    EDDIData.CheckModulesvsEDDI();
                    EDDIData.CheckSignalsvsEDDI();
                }
                else if (cmd.Equals("artiedata"))
                {
                    if (args.Left >= 1)
                    {
                        EliteDangerousCore.MaterialCommodityMicroResourceType.FillTable();
                        ArtieData.Process(args.Next());
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }
                else if (cmd.Equals("phoneme"))
                {
                    if (args.Left >= 2)
                    {
                        Speech.Phoneme(args.Next(), args.Next());
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }
                else if (cmd.Equals("edsy"))
                {
                    if (args.Left >= 2)
                    {
                        string infilename = args.Next();
                        string itemsmod = args.Next();
                        var edsy = new ItemModulesEDSY();
                        edsy.ReadEDSY(infilename, itemsmod);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }


                else if (cmd.Equals("githubreleases"))
                {
                    if (args.Left >= 1)
                    {
                        string file = args.Next();
                        GitHub.Stats(file);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }

                #endregion

                #region Status

                else if (cmd.Equals("status"))
                {
                    Status.StatusSet(args);
                    break;
                }
                else if (cmd.Equals("statusread"))
                {
                    string file = "status.json";
                    if (args.Left >= 1)
                        file = args.Next();
                    Status.StatusRead(file);
                }
                else if (cmd.Equals("statusmove"))
                {
                    Status.StatusMove(args);
                    break;
                }

                #endregion

                #region Journal

                else if (cmd.Equals("journal"))
                {
                    Journal.JournalEntry(args);
                    break;
                }
                else if (cmd.Equals("journalindented"))
                {
                    if (args.Left >= 1)
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
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }
                else if (cmd.Equals("journalplay"))
                {
                    // example: journalplay  c:\code\Journal.2024-01-19T150652.01.log journalplay1.log journalplay c:\code\Journal.2024-01-19T213117.01.log journalplay2.log

                    if (args.Left >= 2)
                    {
                        string inpath = args.Next();
                        string outpath = args.Next();
                        int timerms = 0;
                        string rununtil = null;

                        using (Stream fs = new FileStream(inpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (StreamReader sr = new StreamReader(fs))
                            {
                                Console.WriteLine($"Reading file {inpath} playing to {outpath}");
                                int lineno = 1;
                                string s;
                                while ((s = sr.ReadLine()) != null)
                                {
                                    JObject jo = JObject.Parse(s);
                                    if (jo != null)
                                    {
                                        string eventname = jo["event"].Str();
                                        jo["timestamp"] = "";

                                        Console.WriteLine(lineno++ + ": " + eventname + ": " + jo.ToString(false));

                                        if (rununtil == eventname)
                                        {
                                            timerms = 0;
                                            rununtil = null;
                                        }

                                        if (timerms == 0 || Console.KeyAvailable)
                                        {
                                            var ck = Console.ReadKey();

                                            timerms = 0;
                                            rununtil = null;

                                            if (ck.Key == ConsoleKey.Escape)
                                                break;
                                            else if (ck.Key >= ConsoleKey.D1 && ck.Key <= ConsoleKey.D9)
                                                timerms = 100 * (ck.Key - ConsoleKey.D1) + 100;
                                            else if (ck.Key == ConsoleKey.F1)
                                            {
                                                timerms = 100;
                                                rununtil = "FSDJump";
                                            }
                                            else if (ck.Key == ConsoleKey.F2)
                                            {
                                                timerms = 100;
                                                rununtil = "StartJump";
                                            }
                                        }

                                        jo["timestamp"] = DateTime.UtcNow.StartOfSecond().ToStringZuluInvariant();  // change time to right now

                                        BaseUtils.FileHelpers.TryAppendToFile(outpath, jo.ToString() + Environment.NewLine, true);

                                        if (timerms > 0)
                                            System.Threading.Thread.Sleep(timerms);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Too few args for {cmd} filenamein filenameout");
                        break;
                    }

                }
                else if (cmd.Equals("journalanalyse"))
                {
                    if (args.Left >= 4)
                    {
                        string type = args.Next();
                        string path = args.Next();
                        string filename = args.Next();
                        string datetime = args.Next();
                        JournalAnalysis.Analyse(path, filename, datetime.ParseDateTime(DateTime.MinValue, CultureInfo.CurrentCulture), type);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }

                else if (cmd.Equals("journaltofluent"))
                {
                    if (args.Left >= 1)
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
                                    string code = QuickJSON.JSONFormatter.ToFluent(tk, true);
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
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }

                else if (cmd.Equals("readjournallogs"))
                {
                    if (args.Left >= 1)
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
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }

                #endregion

                #region JSON

                else if (cmd.Equals("jsonlines") || cmd.Equals("jsonlinescompressed"))
                {
                    if (args.Left >= 1)
                    {
                        bool indent = cmd.Equals("jsonindented");

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
                    else
                    {
                        Console.WriteLine("Too few args for jsonlines* filename");
                        break;
                    }

                }
                else if (cmd.Equals("json"))
                {
                    if (args.Left >= 1)
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
                    else
                    {
                        Console.WriteLine("Too few args for json filename");
                        break;
                    }

                }
                else if (cmd.Equals("@json"))
                {
                    if (args.Left >= 3)
                    {
                        string filename = args.Next();
                        bool verbose = args.Int() != 0;
                        bool atit = args.Int() != 0;

                        string[] text = File.ReadAllLines(filename);
                        string res = "";
                        foreach (var t in text)
                        {
                            var jtoken = JToken.Parse(t);
                            if (jtoken != null)
                            {
                                if (verbose)
                                {
                                    res += "----------------------" + Environment.NewLine + jtoken.ToString(true) + Environment.NewLine;
                                }
                                else
                                {
                                    var sb = jtoken.ToString("", "", "", false, 132);
                                    if (!sb.EndsWith(Environment.NewLine))
                                        sb += Environment.NewLine;
                                    res += sb;
                                    if (JToken.Parse(sb) == null)
                                    {
                                        res += "ERROR!!";
                                    }
                                }
                            }
                            else
                                res += "BAD JSON: " + t + Environment.NewLine;
                        }

                        if (atit)
                            res = res.Replace("\"", "\"\"");

                        Console.WriteLine(res);
                    }
                    else
                    {
                        Console.WriteLine("Too few args for @json filename verbose1/0 atstrings0/1");
                        break;
                    }
                }
                else if (cmd.Equals("jsontofluent"))
                {
                    if (args.Left >= 1)
                    {
                        string path = args.Next();
                        try
                        {
                            string text = File.ReadAllText(path);

                            JToken tk = JToken.Parse(text, out string err, JToken.ParseOptions.CheckEOL);
                            if (tk != null)
                            {
                                Console.WriteLine(tk.ToString(true));
                                string code = QuickJSON.JSONFormatter.ToFluent(tk, true);
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
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }


                #endregion

                #region Files

                else if (cmd.Equals("escapestring"))
                {
                    if (args.Left >= 1)
                    {
                        string filename = args.Next();
                        string text = File.ReadAllText(filename);
                        text = text.QuoteString();
                        Console.WriteLine(text);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }
                else if (cmd.Equals("@string"))
                {
                    if (args.Left >= 1)
                    {
                        string filename = args.Next();
                        string text = File.ReadAllText(filename);
                        text = text.Replace("\"", "\"\"");
                        Console.WriteLine(text);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }
                else if (cmd.Equals("xmldump"))
                {
                    if (args.Left >= 2)
                    {
                        string filename = args.Next();
                        int format = args.Int();

                        XElement bindings = XElement.Load(filename);
                        XMLHelpers.Dump(bindings, 0, format);
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }
                else if (cmd.Equals("cutdownfile"))
                {
                    if (args.Left >= 2)
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
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }
                else if (cmd.Equals("mergecsharp"))
                {
                    MergeCSharp.Merge(args);
                }
                else if (cmd.Equals("inserttext"))
                {
                    if (args.Left >= 4)
                    {
                        string path = args.Next();
                        string wildcard = args.Next();
                        string find = args.Next();
                        string insert = args.Next();

                        if (path != null && wildcard != null && find != null && insert != null)
                        {
                            FileInfo[] allFiles = Directory.EnumerateFiles(path, wildcard, SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                            string[] findstrings = null;
                            if (find.StartsWith("@"))
                            {
                                findstrings = File.ReadAllLines(find.Substring(1));
                            }
                            else
                                findstrings = new string[] { find };

                            InsertText.ProcessInsert(allFiles, findstrings, insert);
                        }
                        else
                        {
                            Console.WriteLine("Usage:\n" + "path wildcard findstring insertbeforestring"
                                     );
                        }
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }

                else if (cmd.Equals("wikiconvert"))
                {
                    if (args.Left >= 2)
                    {
                        WikiConvert.Convert(args.Next(), args.Next());
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }
                }
                else if (cmd.Equals("svg"))
                {
                    if (args.Left >= 1)
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
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }
                else if (cmd.Equals("corrupt"))
                {
                    if (args.Left >= 1)
                    {
                        string path = args.Next();
                        FileInfo fi = new FileInfo(path);

                        using (Stream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            Random rnd = new Random();
                            for(int i = 0; i < 100; i++)
                            {
                                long pos = rnd.Next() + fi.Length/2;
                                if ( pos < fi.Length)
                                {
                                    Console.WriteLine($"Corrupt {pos}");
                                    fs.Seek(pos, SeekOrigin.Begin);
                                    fs.WriteByte((Byte)i);
                                }
                            }

                            fs.Close();
                        }
                    }
                    else
                    { Console.WriteLine($"Too few args for {cmd}"); break; }

                }

                #endregion

                else
                {
                    Console.WriteLine("Unknown command, run with empty line for help");
                    break;
                }
            }
        }



    }
}