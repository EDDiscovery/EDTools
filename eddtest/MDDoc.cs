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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EDDTest
{
    public static class MDDoc
    {
        static public void Process(FileInfo[] files, string typename, string existingfile, string search)            // overall index of items
        {
            List<string> donetkalready = new List<string>();

            if (existingfile != null)
            {
                string[] elines = BaseUtils.FileHelpers.TryReadAllLinesFromFile(existingfile);
                if (elines != null)
                {
                    foreach (var l in elines)
                    {
                        if (l.StartsWith("T:"))
                        {
                            int off = l.IndexOf("|");
                            if (off >= 0)
                                donetkalready.Add(l.Substring(2, off - 2));
                        }
                    }
                }
            }

            foreach (var fi in files)
            {
                var utc8nobom = new UTF8Encoding(false);        // give it the default UTF8 no BOM encoding, it will detect BOM or UCS-2 automatically

                using (StreamReader sr = new StreamReader(fi.FullName, utc8nobom))         // read directly from file.. presume UTF8 no bom
                {
                    List<string> lines = new List<string>();
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }

                    for (int i = 0; i < lines.Count; i++)
                    {
                        line = lines[i];

                        int pos = line.IndexOf(typename);
                        if (pos>=0)
                        {
                            int startpos = pos;
                            while (char.IsLetterOrDigit(line[pos]) || line[pos] == '.')
                                pos++;
                            string id = line.Substring(startpos, pos - startpos);
                            if ( !donetkalready.Contains(id))
                            {
                                donetkalready.Add(id);
                                if (search != null)
                                {
                                    int lastdot = id.LastIndexOf(".");
                                    Console.WriteLine($"T:{id}|{search}+{id.Substring(lastdot + 1)}");
                                }
                                else
                                    Console.WriteLine($"T:{id}|{id}");
                            }
                        }
                    }

                }
            }

        }

        static public void ProcessInsert(FileInfo[] files, string finder, string insert)            // overall index of items
        {
            foreach (var fi in files)
            {
                var utc8nobom = new UTF8Encoding(true);        // give it the default UTF8 with BOM encoding, it will detect BOM or UCS-2 automatically

                bool inserted = false;
                List<string> lines = new List<string>();

                Encoding fileenc;

                using (StreamReader sr = new StreamReader(fi.FullName, utc8nobom))         // read directly from file.. presume UTF8 no bom
                {
                    fileenc = sr.CurrentEncoding;

                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                        if (line.Contains(finder))
                        {
                            inserted = true;
                            lines.Add(insert);
                        }
                    }
                }

                if (inserted)
                {
                    Console.WriteLine($"Update {fi.FullName}");
                    using (StreamWriter sw = new StreamWriter(fi.FullName + ".ins", false, fileenc))         // read directly from file.. presume UTF8 no bom
                    {
                        foreach (var l in lines)
                        {
                            sw.WriteLine(l);
                        }
                    }

                    File.Delete(fi.FullName);
                    File.Move(fi.FullName + ".ins", fi.FullName);
                }
            }

        }

    }
}

