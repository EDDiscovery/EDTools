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
using System.IO;
using System.Linq;

namespace EDDTest
{
    // adjust to your preference

    interface JournalAnalyse
    {
        void Process(int lineno, JObject jr, string eventname);
        void Report();
    }

    class ScanAnalyse : JournalAnalyse
    {
        public void Process(int lineno, JObject jr, string eventname)
        {
            if (eventname == "Scan")
            {
                if ( jr["BodyName"].Str().Contains("Ring",StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine(jr.ToString());
                }
            }
        }

        public void Report()
        {
        }
    }


    public static class JournalReader
    {
        public static void ReadJournals(string path)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(path, "*.log", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            ScanAnalyse a1 = new ScanAnalyse();

            JournalAnalyse ja = a1;

            foreach (var fi in allFiles)
            {
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
                                ja.Process(lineno, jr, eventname);
                            }
                        }

                        lineno++;
                    }
                }
            }

            ja.Report();
        }
    }
}


        



//        string ln = jr["timestamp"].Str() + ": " + lineno + ": " + jr["event"].Str();
//        string text = ln + jr.ToString();



//                                if (eventname.Equals("") )
//                                {

//                                }


//                                // looks at jumps
//                                //if (eventname.Equals("StartJump") && jr["JumpType"].Str().Equals("Hyperspace"))
//                                //{
//                                //    System.Diagnostics.Debug.WriteLine($"\n{text}");
//                                //    action = true;
//                                //    accum = "";     // reset 
//                                //}
//                                //else if (eventname.Equals("FSDJump"))
//                                //{
//                                //    System.Diagnostics.Debug.WriteLine($"{text}");
//                                //    action = false;

//                                //    foreach (var s in accum.Split(','))
//                                //    {
//                                //        if (dict.ContainsKey(s))
//                                //            dict[s] += ".";
//                                //        else
//                                //            dict[s] = "1";
//                                //    }

//                                //    accum = "";
//                                //}
//                                //else if (action)
//                                //{
//                                //    System.Diagnostics.Debug.WriteLine($"{text}");
//                                //    if (eventname.Equals("LoadGame") || eventname.Equals("Location"))
//                                //    {

//                                //        action = false;

//                                //    }
//                                //    else
//                                //    {
//                                //        accum = accum.AppendPrePad(eventname, ",");
//                                //    }
//                            }

//                foreach (var e in exits)
//{
//    int bodyid = e["BodyID"].Int(-1);
//    string starsys = e["StarSystem"].Str("x");
//    bool found = false;

//    Console.WriteLine(e.ToString());

//    foreach (JToken j in cache)
//    {
//        if (!found && j["BodyName"].Str().StartsWith(starsys))
//        {
//            JArray p = j["Parents"].Array();
//            if (p != null)
//            {
//                foreach (JToken o in p)
//                {
//                    JObject oo = o.Object();
//                    int nullid = oo["Null"].Int(-1);
//                    if (nullid == bodyid)
//                    {
//                        Console.WriteLine($">> Found null scan with bodyid {j.ToString()}");
//                        found = true;
//                    }
//                    int starid = oo["Star"].Int(-1);
//                    if (starid == bodyid)
//                    {
//                        Console.WriteLine($">> ERROR! scan with bodyid {j.ToString()}");
//                        found = true;
//                    }
//                    int planetid = oo["Planet"].Int(-1);
//                    if (planetid == bodyid)
//                    {
//                        Console.WriteLine($">> ERROR! scan with bodyid {j.ToString()}");
//                        found = true;
//                    }
//                }
//            }
//        }
//    }

//    if (!found)
//        Console.WriteLine(" No scan");


//    //if (jr.Contains("BodyID"))
//    //{
//    //    if (eventname == "SupercruiseExit")
//    //    {
//    //        string bt = jr["BodyType"].Str();
//    //        if (bt == "Null")
//    //        {
//    //            exits.Add(jr);
//    //        }
//    //    }
//    //    if (eventname == "Scan")
//    //    {
//    //        cache.Add(jr);
//    //    }
//    //}


//    //if (jr["event"].Str() == "FSDJump")
//    //{
//    //    string sys = jr["StarSystem"].Str();
//    //    JObject jsf = jr["SystemFaction"].Object();
//    //    if (jsf != null)
//    //    {
//    //        bool name = jsf.Contains("Name");
//    //        bool state = jsf.Contains("FactionState");
//    //        if (!name)
//    //            Console.WriteLine($"{ln} - {sys} - SystemFaction no Name {Environment.NewLine} {jr.ToString(true)}");
//    //        if (!state)
//    //            Console.WriteLine($"{ln}  - {sys} - SystemFaction no Faction State {Environment.NewLine} {jr.ToString(true)}");
//    //        if ( name && state)
//    //            Console.WriteLine($"{ln}  - {sys} - Has both {Environment.NewLine} {jr.ToString(true)}");
//    //    }
//    //    else
//    //    {
//    //        Console.WriteLine($"{ln} - {sys} - no SystemFaction");
//    //    }

//    //}

//    //if (jr["event"].Str() == "Scan")
//    //{
//    //    string body = jr["BodyName"].Str();

//    //    //string ts = jr["TerraformState"].Str();
//    //    //if (ts.HasChars())
//    //    //{
//    //    //    if (!dict.TryGetValue(ts, out string v))
//    //    //    {
//    //    //        Console.WriteLine("TS " + ts);
//    //    //        dict[ts] = "1";
//    //    //    }

//    //    //}

//    //    //JArray jrings = jr["Rings"].Array();
//    //    //if ( jrings != null )
//    //    //{
//    //    //    foreach( var e in jrings)
//    //    //    {
//    //    //        string cls = e["RingClass"].Str();
//    //    //        if (cls != null)
//    //    //            dict[cls] = "1";
//    //    //    }

//    //    //}

//    //    double? absmag = jr["AbsoluteMagnitude"].DoubleNull();

//    //    if ( absmag != null && body == "Sirius")
//    //    {
//    //        Console.WriteLine($"{body} = {absmag}");
//    //        Console.WriteLine($"{jr.ToString()}");
//    //    }
//    //}

//    //}
//    //   else
//    //     Console.WriteLine("Bad Journal line" + error);
//}

