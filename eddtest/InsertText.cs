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
    public static class InsertText
    {
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

