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
    // journaldumpbytype @S J *.log 1/1/2012
    public static class JournalDumpByType
    {
        private static string DateOf(string j)
        {
            int i1 = j.IndexOf(".");
            int i2 = i1 != -1 ? j.IndexOf(".", i1 + 1) : -1;
            if (i1 >= 0 && i2 >= 0 && char.IsDigit(j[i2+1]))
            {
                string k = j.Substring(i1 + 1, i2 - i1 - 1);
                if (!k.Contains("-"))
                    k = "20" + k.Substring(0, 2) + "-" + k.Substring(2, 2) + "-" + k.Substring(4, 2) + "T" + k.Substring(6, 6);
                return k;
            }
            else
                return j;

        }
        public static void Analyse(CommandArgs args)
        {
            string outtype = args.Next();
            string path = args.Next();
            string filename = args.Next();
            DateTime starttime = args.Next().ParseDateTime(DateTime.MinValue, System.Globalization.CultureInfo.CurrentCulture);
            DateTime endtime = args.Next().ParseDateTime(DateTime.MaxValue, System.Globalization.CultureInfo.CurrentCulture);

            if (path == "J")
            {
                path = @"c:\users\rk\saved games\frontier developments\elite dangerous";
            }
            else if (path == "L")
            {
                path = @"c:\code\logs";
            }

            FileInfo[] allFiles = Directory.EnumerateFiles(path, filename, SearchOption.AllDirectories).Select(f => new FileInfo(f)).
                                Where(g=>g.LastWriteTimeUtc>=starttime && g.LastWriteTimeUtc <= endtime).OrderBy(p => DateOf(Path.GetFileName(p.FullName))).ToArray();

            int filecount = 0;
            Dictionary<string, List<string>> outevents = new Dictionary<string, List<string>>();
            Dictionary<string, bool> created = new Dictionary<string, bool>();

            Console.WriteLine($"Log files {allFiles.Length}");

            for ( int i = 0; i < allFiles.Length; i++ )
            {
                var fi = allFiles[i];

                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        break;
                }

                //System.Diagnostics.Debug.WriteLine($"File {fi.FullName}");

                using (StreamReader sr = new StreamReader(fi.FullName))         // read directly from file.. presume UTF8 no bom
                {
                    int lineno = 1;
                    string line;
                    string gamebuildversion = "Unknown";
                    string commander = "Unknown";
                    string file = fi.FullName;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line != "")
                        {
                            JObject jr = JObject.Parse(line, out string error, JToken.ParseOptions.CheckEOL);

                            if (jr != null)
                            {
                                string eventname = jr["event"].Str();

                                if (!eventname.Contains("\0") && eventname.HasChars())
                                {
                                    if (eventname == "Fileheader")
                                    {
                                        gamebuildversion = jr["timestamp"].Str() + ";" + jr["gameversion"].Str() + ";" + jr["build"].Str();
                                    }
                                    else if (eventname == "Commander")
                                    {
                                        commander = jr["Name"].Str();
                                    }

                                    if (!outevents.TryGetValue(eventname, out List<string> textlist))
                                        outevents[eventname] = textlist = new List<string>();

                                    if (outtype == "@")
                                    {
                                        line = "@\"" + line.Replace("\"", "\"\"") + "\";";
                                    }
                                    if (outtype == "@S")
                                    {
                                        line = "@\"" + line.Replace("\"", "\"\"") + "\";";
                                        int pos = 132;
                                        while (pos < line.Length)
                                        {
                                            while (pos < line.Length && line[pos] != ',')
                                                pos++;

                                            if (pos < line.Length)
                                            {
                                                line = line.Substring(0, pos) + "\" +" + Environment.NewLine + "@\"" + line.Substring(pos);
                                                pos += 4;
                                            }

                                            pos += 132;
                                        }

                                        line += Environment.NewLine;
                                    }

                                    textlist.Add(">" + gamebuildversion + ";" + commander + ";" + file);
                                    textlist.Add(line);
                                }
                            }
                        }

                        lineno++;
                    }
                }


                if (filecount++ % 50 == 0 || i == allFiles.Length-1 )
                {
                    Console.WriteLine($"Writing log {filecount}");
                    foreach (var kvp in outevents)
                    {
                        string outfile = Path.Combine(path, kvp.Key + ".event");

                       // Console.WriteLine($"Updating event file {outfile}");

                        if ( created.TryGetValue(kvp.Key,out bool made) == false)
                        {
                            FileHelpers.DeleteFileNoError(outfile);
                            created[kvp.Key] = true;
                        }

                        File.AppendAllLines(outfile, kvp.Value);
                        kvp.Value.Clear();
                    }
                }
            }

        }
    }
}


        

