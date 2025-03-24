﻿/*
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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EDDTest
{
    public static class Speech
    {
        static public void Phoneme(string filename, string fileout)
        {
            using (Stream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (Stream fout = new FileStream(fileout, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        using (StreamWriter sw = new StreamWriter(fout, Encoding.Unicode))
                        {
                            string s;
                            int sn = 1;

                            while ((s = sr.ReadLine()) != null)
                            {
                                StringParser sp = new StringParser(s);

                                if (sp.IsCharMoveOn('{'))
                                {
                                    string name = sp.NextQuotedWord();

                                    if (name != null)
                                    {
                                        string[] namelist = name.Split(' ');

                                        if (sp.IsCharMoveOn(',') && sp.Find("{") && sp.IsCharMoveOn('{'))
                                        {
                                            List<string> strings = new List<string>();

                                            while (sp.PeekChar() == '"')
                                            {
                                                strings.Add(sp.NextQuotedWord());
                                                sp.IsCharMoveOn(',');
                                            }

                                            string o = "Static say_tx_star" + (sn++) + " = \"R;\\b(" + name + ")\\b;";
                                            sw.Write(o);

                                            for (int i = 0; i < strings.Count; i++)
                                            {
                                                o = "<phoneme alphabet='ipa' ph = '" + strings[i] + "'>" + namelist[i] + "</phoneme>";
                                                sw.Write(o);
                                            }

                                            sw.Write("\"");
                                            sw.WriteLine();
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }


    }
}
