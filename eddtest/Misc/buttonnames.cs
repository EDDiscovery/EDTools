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
using System.Xml.Linq;

namespace EDDTest
{
    // buttonnames "C:\Code\edcd\elitecustombuttonnames\files\INTO Bindings\DeviceButtonMaps" "C:\Code\EDDiscovery\UnitTest\Bindings\handcrafted-defkeynames.txt" >c:\code\map.txt
    public static class ButtonNames
    {
        public static void Process(CommandArgs args)
        {
            string path = args.Next();
            FileInfo[] allFiles = Directory.EnumerateFiles(path, "*.buttonMap", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.FullName).ToArray();

            JObject outer = new JObject();
            outer["Version"] = "1.0.0.0";
            JArray devices = new JArray();
            outer["Devices"] = devices;

            string ourbindingfile = args.Next();
            JObject ourdefbindingsfile = ourbindingfile.ReadJSONFile(JToken.ParseOptions.AllowTrailingCommas).Object();
            if ( ourdefbindingsfile == null)
            {

            }

            foreach (var fi in allFiles)
            {
                string devicename = Path.GetFileNameWithoutExtension(fi.FullName);

                //if (devicename != "334400D4")  continue;
                //  if (devicename != "231D0200")  continue;
                // if (devicename != "T16000M")  continue;
                // if (devicename != "054C0CE6")  continue;
              //   if (devicename != "334401F8") continue;

                // pick out our defbindings as we use that to set our custom better names

                string[] lines = FileHelpers.TryReadAllLinesFromFile(fi.FullName);
                string friendlyname = null;
                for (int i = 0; i < 10; i++)
                {
                    if (lines[i].StartsWith("<!--"))
                    {
                        if (lines[i].Length < 10)
                            i++;
                        friendlyname = lines[i].Replace("<!--","").Replace("-->","").Trim();
                        break;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"{devicename} Friendly name {friendlyname}");

                // find in our handmade file the device, if there

                JObject ourkeys = null;
                if (ourdefbindingsfile != null)
                {
                    JArray devicelist = ourdefbindingsfile["Devices"].Array();
                    foreach (JObject devic in devicelist)
                    {
                        if (devic["Device"].Str() == devicename)
                        {
                            ourkeys = devic["Keys"].Object();
                            break;
                        }
                    }
                }

                JObject jd = new JObject();
                devices.Add(jd);
                jd["Device"] = devicename;
                jd["Name"] = friendlyname;

                JObject keys = new JObject();

                XElement map = XElement.Load(fi.FullName);


                // here we store the root attributes in case we want to write the XML back out

                int maxbutton = -1;
                int maxpov = -1;
                bool[] axispresent = new bool[8];

                foreach (XElement key in map.Elements())
                {
                    JObject jkey = new JObject();
                    string keyname = key.Name.ToString();
                    if ( keyname.Contains("Slider2"))      // def checked control bindings in elite, its called V
                    {
                    //    System.Diagnostics.Debug.WriteLine($"Joy_Slider in {devicename}");
                        keyname = "Joy_VAxis";
                    }

                    string value = null, hint = null, icon = null;

                    // if use our handcrafted one

                    if (ourkeys != null && ourkeys.Contains(keyname))
                    {
                        value = ourkeys[keyname]["Name"].Str();
                        System.Diagnostics.Debug.WriteLine($"Replace {keyname} with {value}");
                        hint = ourkeys[keyname]["Hint"].Str();
                        icon = ourkeys[keyname]["Icon"].StrNull();
                    }
                    else
                    {
                        value = key.Value.Trim();


                        int iconpos = value.IndexOf("[");
                        if (iconpos >= 0)
                        {
                            int iconfinish = value.IndexOf("]", iconpos + 1);
                            if (iconfinish >= 0)
                            {
                                icon = value.Substring(iconpos, iconfinish - iconpos + 1);      // incl

                                value = value.Substring(0, iconpos).Trim() + " " + value.Substring(iconfinish + 1).Trim();
                                value = value.Trim();

                                if (!value.HasChars())
                                    jkey["IconOnly"] = true;
                            }
                        }

                        value = value.Replace("RGNX", "VKB");
                    }

                    // if we have our mapping, then use name and hint

                    jkey["Name"] = value.Trim();
                    if (hint != null)
                        jkey["Hint"] = hint;
                    if ( icon != null )
                        jkey["Icon"] = icon;

                    keys.Add(keyname, jkey);
                    //System.Diagnostics.Debug.WriteLine($"{devicename} : {name} = {value}");

                    int? jbut = keyname.Substring(4).InvariantParseIntNull();
                    if (jbut > 0)
                        maxbutton = Math.Max(jbut.Value, maxbutton);
                    int? povnum = keyname.StartsWith("Joy_POV") ? keyname.Substring(7,1).InvariantParseIntNull() : null;
                    if (povnum > 0)
                        maxpov = Math.Max(povnum.Value, maxpov);
                    if (keyname.Contains("Joy_XAxis"))
                        axispresent[0] = true;
                    if (keyname.Contains("Joy_YAxis"))
                        axispresent[1] = true;
                    if (keyname.Contains("Joy_ZAxis"))
                        axispresent[2] = true;
                    if (keyname.Contains("Joy_RXAxis"))
                        axispresent[3] = true;
                    if (keyname.Contains("Joy_RYAxis"))
                        axispresent[4] = true;
                    if (keyname.Contains("Joy_RZAxis"))
                        axispresent[5] = true;
                    if (keyname.Contains("Joy_UAxis"))
                        axispresent[6] = true;
                    if (keyname.Contains("Joy_VAxis"))
                        axispresent[7] = true;
                }

                if (maxbutton > 0)
                    jd["Buttons"] = maxbutton;
                if (maxpov > 0)
                    jd["POV"] = maxpov;

                string axis = "";
                for (int i = 0; i < axispresent.Length; i++)
                {
                    if (axispresent[i])
                        axis = axis.AppendPrePad(new string[] { "X", "Y", "Z", "RX", "RY", "RZ", "U", "V" }[i], ","); 
                }

                if (axis.HasChars())
                    jd["Axis"] = axis;

                jd["Keys"] = keys;

                // detect duplicates

                HashSet<string> names = new HashSet<string>();
                HashSet<string> duplicates = new HashSet<string>();
                foreach (var kvp in keys)
                {
                    string value = kvp.Value["Name"].Str();
                    if (names.Contains(value))
                    {
                        duplicates.Add(value);
                    }
                    names.Add(value);
                }

                // process duplicates
                foreach (var kvp in keys)
                {
                    string value = kvp.Value["Name"].Str();
                    string icon = kvp.Value["Icon"].StrNull();
                    if (duplicates.Contains(value))
                    {
                        string newname = null;
                        if (icon == null)
                        {
                            newname = kvp.Key;
                          //  System.Diagnostics.Debug.WriteLine($"Duplicate name No ICON in {devicename}: `{value}` -> `{newname}`");
                        }
                        else
                        {
                            newname = value + " " + icon.Replace("ps4", "").Replace("x52pro", "").Replace("x52", "").Replace("hts4", "").Replace("Pad", "").Replace("Pro", "")
                                            .Replace("xb1", "").Replace("x360", "").Replace("[","").Replace("]","").SplitCapsWordFull();
                            newname = newname.Trim();   
                           // System.Diagnostics.Debug.WriteLine($"Duplicate name in {devicename}: `{value}` -> `{newname}`");
                        }
                        kvp.Value["Name"] = newname;
                    }
                }

            }

            // add any in our binding files to the game

            if (ourdefbindingsfile != null)
            {
                JArray devicelist = ourdefbindingsfile["Devices"].Array();
                List<JObject> toadd = new List<JObject>();  
                foreach (JObject devic in devicelist)
                {

                    var found = devices.Find(x => x["Device"].Str() == devic["Device"].Str());
                    if ( found == null )
                    {
                        toadd.Add(devic);
                    }
                }

                foreach (JObject devic in toadd)
                    devices.Add(devic);
            }


            //         System.Diagnostics.Debug.WriteLine(outer.ToString(true));
            Console.WriteLine(outer.ToString(true));
        }
    }
}
