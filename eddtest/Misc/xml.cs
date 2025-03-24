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
 */

using QuickJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace EDDTest
{
    static class XMLHelpers
    {
        public static void Dump(XElement x, int level, int format)
        {
            string pretext = "                                       ".Substring(0, level * 3);
            if (format == 0)
                Console.WriteLine(level + pretext + x.NodeType + " " + x.Name.LocalName + (x.Value.HasChars() ? (" : " + x.Value) : ""));
            else if (format == 1)
            {
                if (level > 1)
                    Console.Write(",\"" + x.Value + "\"");
            }

            if (x.HasAttributes)
            {
                foreach (XAttribute y in x.Attributes())
                {
                    if (format == 0)
                        Console.WriteLine(level + pretext + "  attr " + y.Name + " = " + y.Value);
                    else if (!y.Name.ToString().StartsWith("{http"))
                        Console.Write("\"$" + y.Value.ToString().ToLower() + "\"");
                }
            }

            if (x.HasElements)
            {
                foreach (XElement y in x.Elements())
                {
                    //Console.WriteLine(level + pretext + x.Name.LocalName + " desc " + y.Name.LocalName);
                    Dump(y, level + 1, format);
                    //Console.WriteLine(level + pretext + x.Name.LocalName + " Out desc " + y.Name.LocalName);
                }
            }

            if (level == 1)
                Console.WriteLine(",");

        }
    }
}
