/*
 * Copyright © 2015 - 2024 robbyxp @ github.com
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
 */

using BaseUtils;
using QuickJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EDDTest
{
    // adjust to your preference
    enum ProcessResult { Nothing, Found, StopProcessing }

    interface JournalAnalyse
    {
        ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname);
        string Report();

        string OutputName { get; }
    }

    class CommonAnalyse : JournalAnalyse
    {
        public string OutputName { get; set; }
        Dictionary<string, int> rep1 = new Dictionary<string, int>();
        Dictionary<string, int> rep2 = new Dictionary<string, int>();
        Dictionary<string, int> rep3 = new Dictionary<string, int>();
        Dictionary<string, int> rep4 = new Dictionary<string, int>();

        public void Incr(string value)
        {
            if (rep1.ContainsKey(value))
                rep1[value]++;
            else
                rep1[value] = 1;
        }
        public void Incr2(string value)
        {
            if (rep2.ContainsKey(value))
                rep2[value]++;
            else
                rep2[value] = 1;
        }
        public void Incr3(string value)
        {
            if (rep3.ContainsKey(value))
                rep3[value]++;
            else
                rep3[value] = 1;
        }
        public void Incr4(string value)
        {
            if (rep3.ContainsKey(value))
                rep3[value]++;
            else
                rep3[value] = 1;
        }

        public virtual ProcessResult Process(string filename, int lineno, string cmdrname, JObject jr, string eventname)
        {
            return ProcessResult.Nothing;
        }

        public string Report()
        {
            var keys = rep1.Keys.ToList();
            keys.Sort();
            string str = "";
            foreach (var key in keys)
            {
                str += $"{key} : {rep1[key]}" + Environment.NewLine;
            }

            if (rep2.Count > 0)
            {
                str += "------------" + Environment.NewLine;

                keys = rep2.Keys.ToList();
                keys.Sort();
                foreach (var key in keys)
                {
                    str += $"{key} : {rep2[key]}" + Environment.NewLine;
                }
            }

            if (rep3.Count > 0)
            {
                str += "------------" + Environment.NewLine;

                keys = rep3.Keys.ToList();
                keys.Sort();
                foreach (var key in keys)
                {
                    str += $"{key} : {rep3[key]}" + Environment.NewLine;
                }
            }

            if (rep4.Count > 0)
            {
                str += "------------" + Environment.NewLine;

                keys = rep4.Keys.ToList();
                keys.Sort();
                foreach (var key in keys)
                {
                    str += $"{key} : {rep4[key]}" + Environment.NewLine;
                }
            }

            return str;
        }
    }

    //  journalanalyse "c:\users\rk\saved games\frontier developments\elite dangerous" *.log loadout
    //  journalanalyse loadout "c:\code\logs" *.log 

    public static class JournalAnalysis
    {
        public static void Analyse(CommandArgs args)
        {
            string type = args.Next();
            string path = args.Next();
            string filename = args.Next();
            DateTime starttime = args.Next().ParseDateTime(DateTime.MinValue, System.Globalization.CultureInfo.CurrentCulture);

            if (path == "J")
            {
                path = @"c:\users\rk\saved games\frontier developments\elite dangerous";
            }
            else if (path == "L")
            {
                path = @"c:\code\logs";
            }

            FileInfo[] allFiles = Directory.EnumerateFiles(path, filename, SearchOption.AllDirectories).Select(f => new FileInfo(f)).
                                Where(g=>g.LastWriteTimeUtc>=starttime).OrderBy(p => p.FullName).ToArray();

            JournalAnalyse ja = null;
            type = type.ToLowerInvariant();
            if (type == "slot")
                ja = new SlotAnalyse();
            else if (type == "scan")
                ja = new ScanAnalyse();
            else if (type == "fsdLoc")
                ja = new SlotAnalyse();
            else if (type == "shiptype")
                ja = new ShipTypeAnalyse();
            else if (type == "loadout")
                ja = new LoadoutAnalyse();
            else if (type == "services")
                ja = new ServicesAnalyse();
            else if (type == "fsdloc")
                ja = new FSDLocAnalyse();
            else if (type == "booktaxi")
                ja = new BookTaxiAnalyse();
            else if (type == "marketid")
                ja = new MarketIDAnalyse();
            else if (type == "bodytype")
                ja = new BodyTypeAnalyse();
            else if (type == "passengers")
                ja = new PassengerAnalyse();
            else if (type == "loadgame")
                ja = new LoadGameAnalyse();
            else if (type == "missioncomplete")
                ja = new MissionCompleteAnalyse();
            else if (type == "missionaccepted")
                ja = new MissionAcceptedAnalyse();
            else if (type == "typeanalyse")
                ja = new TypeAnalysis();
            else if (type == "crewroleanalyse")
                ja = new CrewRoleAnalysis();
            else if (type == "scanfind")
            {
                var sf = new ScanFind();
                sf.BodyName = args.Next();
                sf.Modern = args.Bool();
                System.Diagnostics.Debug.Assert(sf.BodyName != null);
                ja = sf;
            }
            else if (type == "docked")      // journalanalyse docked J *.log X Coriolis 1 2 3
            {
                var sf = new DockedFind();
                sf.StationType = args.Next();
                sf.X = args.Double();
                sf.Y = args.Double();
                sf.Z = args.Double();
                ja = sf;
            }
            else if (type == "stationdocked")
            {
                var sf = new ScanDockingBodies();
                ja = sf;
            }
            else
            {
                Console.Error.WriteLine("Not recognised analysis type");
                return;
            }

            int filecount = 0;

            foreach (var fi in allFiles)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        break;
                }

                if (filecount++ % 50 == 0)
                {
                    Console.WriteLine($"Processing {fi.FullName} count {filecount}");
                    System.Diagnostics.Debug.WriteLine($"Processing {fi.FullName} count {filecount}");
                }

                int found = 0;
                string cmdrname = "?";

                using (StreamReader sr = new StreamReader(fi.FullName))         // read directly from file.. presume UTF8 no bom
                {
                    int lineno = 1;
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line != "")
                        {
                            JObject jr = JObject.Parse(line, out string error, JToken.ParseOptions.CheckEOL);

                            if (jr != null)
                            {
                                string eventname = jr["event"].Str();

                                if (eventname == "Commander")
                                    cmdrname = jr["Name"].Str();

                                ProcessResult pr = ja.Process(fi.FullName, lineno, cmdrname, jr, eventname);
                                if (pr == ProcessResult.Found)
                                    found++;
                                else if (pr == ProcessResult.StopProcessing)
                                    break;
                            }
                        }

                        lineno++;
                    }
                }

                if (found > 0)
                {
                //    Console.Error.WriteLine($"Found {found} : {fi.FullName}");
                  //  Console.WriteLine($"Found {found} : {fi.FullName}");
                }
                else
                {
                    // Console.WriteLine($"Nothing in : {fi.FullName}");
                }


            }

            string rep = ja.Report();
            Console.WriteLine(rep);
            File.WriteAllText(ja.OutputName ?? "report.txt", rep);
        }
    }
}


        

