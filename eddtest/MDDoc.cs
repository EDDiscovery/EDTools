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
        static public void FindDocLinks(FileInfo[] files, string typename, string existingfile, string repname)           
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
                                if (repname != null)
                                {
                                    int lastdot = id.LastIndexOf(".");
                                    Console.WriteLine($"T:{id}|{repname}+{id.Substring(lastdot + 1)}");
                                }
                                else
                                    Console.WriteLine($"T:{id}|{id}");
                            }
                        }
                    }

                }
            }

        }

        // Post process the default document .md files to make them more neat

        static public void PostProcessMD(FileInfo[] files, bool removerootnamespace)            // overall index of items
        {
            foreach (var fi in files)
            {
                var utc8nobom = new UTF8Encoding(true);        // give it the default UTF8 with BOM encoding, it will detect BOM or UCS-2 automatically

                List<string> lines = new List<string>();

                Encoding fileenc;

                string filename = Path.GetFileNameWithoutExtension(fi.FullName);
                int pos = filename.LastIndexOf("_");
                string classname = pos >= 0 ? filename.Substring(pos + 1) : "";
                string rootname = filename.IndexOf("_") > 0 ? filename.Substring(0, filename.IndexOf("_")) : "";
                string namespacet = pos >= 0 ? filename.Substring(0, pos) : "";
                Console.WriteLine($"Processing {filename} namespace `{namespacet}` root `{rootname}` class `{classname}`");

                using (StreamReader sr = new StreamReader(fi.FullName, utc8nobom))         // read directly from file.. presume UTF8 no bom
                {

                    fileenc = sr.CurrentEncoding;

                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // fix references to new naming system

                        if (line.IndexOf("[") < line.IndexOf("]") && line.IndexOf("(") < line.IndexOf(")") && line.IndexOf("[") < line.IndexOf("("))
                        {
                            int lb = line.IndexOf("[");

                            StringParser s = new StringParser(line.Substring(lb));

                            string name = s.NextWord("(");          // pattern is [text] (file fancyname)
                            s.GetChar();
                            string link = s.NextWord(" ");
                            string fancyname = s.NextWord(")");
                            s.GetChar();

                            if (name != null && link != null && fancyname != null && !link.Contains("http"))
                            {
                                if (removerootnamespace)
                                {
                                    link = link.ReplaceIfStartsWith(rootname + "_", "");        // normal links have root _
                                    link = link.ReplaceIfStartsWith(rootname + "-", "");        // class list has name-
                                }

                                link = link.Replace("_", ".");
                                string l = line.Substring(0, lb) + name + "(" + link + " " + fancyname + ")" + s.LineLeft;
                                //System.Diagnostics.Debug.WriteLine($"{name} . {link} = `{l}`");
                                line = l;
                            }
                        }

                        if ( line.Equals("### Constructors") || line.Equals("### Enums" ) || line.Equals("### Methods") || line.Equals("### Fields") || 
                            line.Equals("### Properties") || line.Equals("### Interfaces"))
                        {
                            lines.Add("***");
                            lines.Add(line.Replace("###", "#"));
                        }
                        else if (line.StartsWith("## "))
                        {
                            lines.Add("***");
                            lines.Add(line.Replace("##", "###"));
                        }
                        else if (line.StartsWith("### "))
                        {
                            lines.Add(line.Replace("###", "####"));
                        }
                        else if (line.StartsWith("#### "))
                        {
                            lines.Add(line.Replace("####", "#####"));
                        }
                        else if (line.StartsWith("| Classes | |"))
                        {
                            lines.Add("***");
                            lines.Add("# Classes");
                            lines.Add(line);
                        }
                        else
                            lines.Add(line);

                    }
                }

                using (StreamWriter sw = new StreamWriter(fi.FullName + ".ins", false, fileenc))         // read directly from file.. presume UTF8 no bom
                {
                    foreach (var l in lines)
                    {
                        sw.WriteLine(l);
                    }
                }

                if (true)
                {
                    File.Delete(fi.FullName);
                    string finalfile;

                    if (removerootnamespace)
                    {
                        finalfile = Path.Combine(Path.GetDirectoryName(fi.FullName), filename.ReplaceIfStartsWith(rootname + "_", "").Replace("_", ".") + ".md");
                    }
                    else
                    {
                        finalfile = Path.Combine(Path.GetDirectoryName(fi.FullName), filename.Replace("_", ".") + ".md");
                    }
                    File.Delete(finalfile);
                    File.Move(fi.FullName + ".ins", finalfile);
                }
            }

        }
    }
}

