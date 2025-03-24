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
 *
 */

using BaseUtils;
using QuickJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EDDTest
{

    public static class MergeCSharp
    {
        public static void Merge(CommandArgs args)
        {
            string outputtext = "";
            HashSet<string> usinglines = new HashSet<string>();
            List<string> definelines = new List<string>();

            string path;
            while ((path = args.Next()) != null)
            {
                FileInfo[] allFiles = Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                foreach (var fi in allFiles)
                {
                    using (StreamReader sr = new StreamReader(fi.FullName))         // read directly from file.. presume UTF8 no bom
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            string lt = line.Trim();
                            if (line != "" && !lt.StartsWith("//") && !lt.StartsWith("*") && !lt.StartsWith("/*") && !lt.StartsWith("*/"))
                            {
                                if (lt.StartsWith("using") && lt.IndexOf("(") == -1)
                                {
                                    usinglines.Add(line);
                                }
                                else
                                if (lt.StartsWith("#define"))
                                {
                                    definelines.Add(line);
                                }
                                else
                                {
                                    outputtext += line + Environment.NewLine;
                                }
                            }

                        }
                    }
                }
            }

            Console.WriteLine(String.Join(Environment.NewLine, definelines));
            Console.WriteLine(String.Join(Environment.NewLine, usinglines.ToList()));
            Console.WriteLine(outputtext);
        }
    }
}
