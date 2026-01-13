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
                    int lineno = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lineno++;

                        int replacepos = 0;

                        // fix references to new naming system
                        // its [text](link 'fancyname')

                        while(line.IndexOf("[",replacepos) < line.IndexOf("](",replacepos) && 
                                        line.IndexOf("](", replacepos) < line.IndexOf(")", replacepos))
                        { 
                            int lb = line.IndexOf("[",replacepos);

                            StringParser s = new StringParser(line.Substring(lb));
                            string name = s.NextWord("(");          // pattern is [text] (file fancyname)
                            s.GetChar();
                            string link = s.NextWord(" ");
                            string fancyname = s.NextWord(")");
                            s.GetChar();

                            System.Diagnostics.Debug.WriteLine($"{lineno} : Link: `{name}` to `{link}` `{fancyname}`");

                            System.Diagnostics.Debug.Assert(name != null && link != null && fancyname != null);

                            if (!link.Contains("http"))
                            {
                                if (removerootnamespace)
                                {
                                    link = link.ReplaceIfStartsWith(rootname + "_", "");        // normal links have root _
                                    link = link.ReplaceIfStartsWith(rootname + "-", "");        // class list has name-
                                }

                                string l = line.Substring(0, lb) + name + "(" + link + " " + fancyname + ")";
                                replacepos = l.Length;
                                l += s.LineLeft;
                                line = l;
                                System.Diagnostics.Debug.WriteLine($"... output {line}");
                            }
                            else
                                replacepos = lb + s.Position;
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
                        else if (line.StartsWith("<a name='"))  // just here to pick up in case we ever want to process
                        {
                            // System.Diagnostics.Debug.WriteLine($"{lineno} : Anchor found: {line}");
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

        public static void CheckLinks()
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(".", "*.md", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            foreach (FileInfo file in allFiles)
            {
                string[] filecontents = File.ReadAllLines(file.FullName);
                System.Diagnostics.Debug.WriteLine($"Checking {file.FullName}");

                for ( int i=  0; i < filecontents.Length; i++ )
                {
                    string line = filecontents[i];
                    //System.Diagnostics.Debug.WriteLine($" Line {i+1} : {line}");

                    int nextpos = line.IndexOf("[[", 0);
                    if (nextpos >= 0)
                    {
                        int endpos = line.IndexOf("]]", nextpos);
                        if (endpos == -1)
                        {
                            Console.WriteLine($"{file.FullName}:{i} : Bad [[ ]] pair at {nextpos}");
                            break;
                        }

                        string link = line.Substring(nextpos + 2, endpos - nextpos - 2);
                        int bar = link.IndexOf("|");
                        if (bar == -1)
                        {
                            Console.WriteLine($"{file.FullName}:{i} : Bad | in [[ ]] pair at {nextpos}");
                            break;
                        }

                        string prefix = link.Substring(0, bar);
                        string postfix = link.Substring(bar + 1);
                        System.Diagnostics.Debug.WriteLine($"Read link `{prefix}` `{postfix}`");

                        if (prefix.StartsWith("/images"))
                        {
                            string filename = "." + prefix;
                            if (!File.Exists(filename))
                                Console.WriteLine($"{file.FullName}:{i} : At {nextpos} bad link {link}");

                        }
                        else if (prefix.StartsWith("images/"))
                        {
                            string filename = prefix;
                            if (!File.Exists(filename))
                                Console.WriteLine($"{file.FullName}:{i} : At {nextpos} bad link {link}");
                        }
                        else if (postfix.StartsWith("http"))
                        {

                        }
                        else
                        {
                            int hash = postfix.IndexOf('#');
                            if (hash >= 0)
                                postfix = postfix.Substring(0, hash);   

                            string filename = postfix.Replace(" ", "-") + ".md";

                            if (!File.Exists(filename))
                                Console.WriteLine($"{file.FullName}:{i} At {nextpos} bad link {link}");
                        }
                    }
                }
            }
         } 


        public static void Rename(string from, string to)
        {
            from = from.Replace("-", " ").Replace(".md", "");
            string fromname = from.Replace(" ", "-");
            string fromfile = fromname + ".md";

            if ( to.StartsWith("#"))
                to = to.Substring(1) + "-" + fromname;

            to = to.Replace("-", " ").Replace(".md", "");
            string toname = to.Replace(" ", "-");
            string tofile = toname + ".md";

            Console.WriteLine($"{from.PadRight(30)} - > `{to}`");

            if (File.Exists(fromfile))
            {
                if (!File.Exists(tofile))
                {
                    Console.WriteLine($"Updated {fromfile} -> {tofile}");
                    File.Move(fromfile, tofile);
                    FileInfo[] allFiles = Directory.EnumerateFiles(".", "*.md", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

                    foreach (FileInfo file in allFiles)
                    {
                        string filecontents = File.ReadAllText(file.FullName);
                        string newcontents = filecontents.Replace($"|{from}]", $"|{to}]", StringComparison.InvariantCultureIgnoreCase);
                        newcontents = newcontents.Replace($"|{from}#", $"|{to}#", StringComparison.InvariantCultureIgnoreCase);
                        if (filecontents != newcontents)
                        {
                            File.WriteAllText(file.FullName, newcontents);
                            Console.WriteLine($"Updated {file.FullName}");
                        }
                    }
                }
                else
                    Console.WriteLine($"Cannot rename as as file exists {tofile}");
            }
            else
                Console.WriteLine($"Cannot find file {fromfile}");
        }

        // Only needed to be used once, during development of new code.  Keep for ref
        public static void RenameSection4()
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(".", "4.*.md", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            List<string> names = new List<string>(allFiles.Select(x => x.Name));

            // sort ignoring the prefix number
            names.Sort(delegate (string left, string right) { return left.Substring(5).CompareTo(right.Substring(3)); });

            int number = 1;
            foreach (var item in names)
            {
                //Rename(item, "4." + number.ToString().PadLeft(2) + "-" + item.Substring(5)); // problem if we enter new entries..
                Rename(item, "4." + "-" + item.Substring(5));
                number++;
            }

        }

    }
}

