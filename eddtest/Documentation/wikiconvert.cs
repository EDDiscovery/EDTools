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
using System.IO;
using System.Linq;

namespace EDDTest
{
    public static class WikiConvert
    {
        static public void Convert(string path, string imgpath)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(Path.GetDirectoryName(path), Path.GetFileName(path), SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            foreach (var fi in allFiles)
            {
                using (Stream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    string outname = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(fi.FullName)+ ".out");
                    bool changed = false;

                    using (Stream fout = new FileStream(outname, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using (StreamWriter sw = new StreamWriter(fout))
                        {
                            using (StreamReader sr = new StreamReader(fs))
                            {
                                string s;
                                while ((s = sr.ReadLine()) != null)
                                {
                                    StringParser sp = new StringParser(s);

                                    while (sp.Find("!["))
                                    {
                                        int spos = sp.Position;

                                        sp.MoveOn(2);
                                        string helptext = sp.NextWord("]");
                                        if (sp.IsCharMoveOn(']') && sp.IsCharMoveOn('('))
                                        {
                                            string ip = sp.NextWord(")");

                                            if (sp.IsCharMoveOn(')'))
                                            {
                                                System.Diagnostics.Debug.WriteLine("Image Tag {0} {1}", helptext, ip);

                                                if (ip.Contains("imgur"))
                                                {
                                                    int lastslash = ip.LastIndexOf('/');
                                                    string imagename = ip.Substring(lastslash + 1);

                                                    string sourcefilename = Path.Combine(imgpath, imagename);
                                                    if (File.Exists(sourcefilename))
                                                    {
                                                        string destfilename = Path.Combine(Path.GetDirectoryName(path), "images\\" + imagename);

                                                        File.Copy(sourcefilename, destfilename, true);

                                                        sp.Replace(spos, sp.Position - spos, "[[/images/" + imagename + "|" + helptext + "]]");
                                                        changed = true;
                                                    }
                                                    else
                                                        Console.WriteLine("Can't find source " + sourcefilename + " ref in " + fi.FullName);
                                                }
                                                else
                                                    Console.WriteLine("Not referencing imgur in " + ip + " ref in " + fi.FullName);
                                            }
                                        }
                                    }

                                    sw.WriteLine(sp.Line);

                                    //System.Diagnostics.Debug.WriteLine("New line: '" + sp.Line);
                                }
                            }
                        }
                    }

                    if ( changed)
                    {
                        Console.WriteLine("Changed : " + fi.FullName);
                    }
                    else
                    {
                        Console.WriteLine("No changes to : " + fi.FullName);
                        File.Delete(outname);
                    }
                }
            }
        }
    }
}

