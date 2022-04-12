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

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace EDDTest
{
    public static class BindingsFile
    {
        public static void Bindings(string filename)
        {
            using (Stream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                List<string> bindings = new List<string>();
                List<string> say = new List<string>();
                List<string> saydef = new List<string>();

                using (StreamReader sr = new StreamReader(fs))
                {
                    string s;
                    while ((s = sr.ReadLine()) != null)
                    {
                        int i = s.IndexOf("KEY ", StringComparison.InvariantCultureIgnoreCase);
                        if (i >= 0 && i < 16)
                        {
                            s = s.Substring(i + 4).Trim();
                            if (!bindings.Contains(s))
                                bindings.Add(s);
                        }
                        i = s.IndexOf("Say ", StringComparison.InvariantCultureIgnoreCase);
                        if (i >= 0 && i < 16)
                        {
                            s = s.Substring(i + 4).Trim();
                            if (!say.Contains(s))
                                say.Add(s);
                        }
                        i = s.IndexOf("Static say_", StringComparison.InvariantCultureIgnoreCase);
                        if (i >= 0 && i < 16)
                        {
                            //Console.WriteLine("saw " + s);
                            s = s.Substring(i + 7).Trim();
                            i = s.IndexOf(" ");
                            if (i >= 0)
                                s = s.Substring(0, i);
                            if (!saydef.Contains(s))
                                saydef.Add(s);
                        }
                    }
                }

                bindings.Sort();

                Console.WriteLine("*** Bindings:");
                foreach (string s in bindings)
                {
                    Console.WriteLine(s);
                }
                Console.WriteLine("*** Say definitions:");
                foreach (string s in saydef)
                {
                    Console.WriteLine(s);
                }
                Console.WriteLine("*** Say commands:");
                foreach (string s in say)
                {
                    Console.WriteLine(s);
                }
            }
        }


        public static void DeviceMappings(string filename)
        {
            try
            {
                XElement bindings = XElement.Load(filename);

                System.Diagnostics.Debug.WriteLine("Top " + bindings.NodeType + " " + bindings.Name);

                Console.WriteLine("Dictionary<Tuple<int, int>, string> ctrls = new Dictionary<Tuple<int, int>, string>()" + Environment.NewLine + "{" + Environment.NewLine);

                foreach (XElement x in bindings.Elements())
                {
                    string ctrltype = x.Name.LocalName;

                    Dictionary<Tuple<int, int>, string> pv = new Dictionary<Tuple<int, int>, string>();

                    int pid = 0;
                    int vid = 0;

                    int.TryParse(x.Element("PID").Value, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out pid);
                    int.TryParse(x.Element("VID").Value, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out vid);

                    var key = new Tuple<int, int>(pid, vid);        // does not seem to find duplicates, no idea, but beware!
                    pv.Add(key,"Map");

                    foreach (XElement y in x.Elements())
                    {
                        if (y.Name.LocalName.Equals("Alternative"))
                        {
                            int.TryParse(y.Element("PID").Value, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out pid);
                            int.TryParse(y.Element("VID").Value, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out vid);

                            pv.Add(new Tuple<int, int>(pid, vid),"Map");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("Ctrl " + ctrltype);
                    foreach (var pk in pv)
                        System.Diagnostics.Debug.WriteLine("  " + pk.Key.Item1.ToString("x") + " " + pk.Key.Item2.ToString("x"));

                    foreach (var pk in pv)
                    {
                        Console.WriteLine("        {  new Tuple<int,int>(0x" + pk.Key.Item1.ToString("X") + ", 0x" + pk.Key.Item2.ToString("X") + "), \"" + ctrltype + "\" },");
                    }
                }

                Console.WriteLine("};");
            }

            catch
            {

            }

            //example..
            Dictionary<Tuple<int, int>, string> ct2rls = new Dictionary<Tuple<int, int>, string>()
            {
                { new Tuple<int,int>(1,1), "Fred" },
            };
        }


    }
}
