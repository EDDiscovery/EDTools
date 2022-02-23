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

    public static class JournalReader
    {
        public static void ReadJournals( string path)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(path, "*.log", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach ( var fi in allFiles)
            {
                using (StreamReader sr = new StreamReader(fi.FullName))         // read directly from file.. presume UTF8 no bom
                {
                    int lineno = 1;
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if ( line != "")
                        {
                            JObject jr = JObject.Parse(line, out string error, JToken.ParseOptions.CheckEOL);

                            if (jr != null)
                            {
                                string ln = jr["timestamp"].Str() + ":" + jr["event"].Str();

                                if ( jr["event"].Str() == "Scan")
                                {
                                    string ts = jr["TerraformState"].Str();
                                    if (ts.HasChars())
                                    {
                                        if (!dict.TryGetValue(ts, out string v))
                                        {
                                            Console.WriteLine("TS " + ts);
                                            dict[ts] = "1";
                                        }

                                    }
                                        
                                }

                            }
                            else
                                Console.WriteLine("Bad Journal line" + error);
                        }

                        lineno++;
                    }
                }
            }

        }
    }
}
