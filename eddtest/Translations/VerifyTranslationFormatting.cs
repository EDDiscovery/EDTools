/*
 * Copyright © 2015 - 2024 robbyxp @ github.com
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

using BaseUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EDDTest
{
    public static class VerifyFormattingClass
    {
        static public string VerifyFormatting(string reportfile, int reportline, string englishtext, string translationtext, string reportid)
        {
            int pos = 0;
            string bad = null;

            while ((pos = englishtext.IndexOf("{", pos)) != -1 && bad == null)      // go thru brackets of eng
            {
                int endpos = englishtext.IndexOf("}", pos);
                if (endpos != -1)
                {
                    string s = englishtext.Substring(pos, endpos - pos + 1);
                    int post = translationtext.IndexOf(s);

                    if (post == -1)       // if not found in string {n}
                        bad = $"Bracket mismatch at {pos}:`{englishtext.Substring(pos)}`:`{s}`";
                }

                pos++;
            }

            pos = 0;
            while ((pos = translationtext.IndexOf("{", pos)) != -1 && bad == null)      // go thru brackets of trans
            {
                int endpos = translationtext.IndexOf("}", pos);
                if (endpos != -1)
                {
                    string s = translationtext.Substring(pos, endpos - pos + 1);
                    int post = englishtext.IndexOf(s);

                    if (post == -1)       // if not found in string {n}
                        bad = $"Bracket mismatch (more in trans) at {pos}:`{englishtext.Substring(pos)}`:`{s}`";
                }

                pos++;
            }

            if (bad == null)
            {
                int engsemicolons = 0;
                for (int i = 0; i < englishtext.Length; i++)
                    engsemicolons += englishtext[i] == ';' ? 1 : 0;

                int foreignsemicolons = 0;
                for (int i = 0; i < translationtext.Length; i++)
                    foreignsemicolons += translationtext[i] == ';' ? 1 : 0;

                if (engsemicolons == 2)     // build format prefix;postfix;format
                {
                    if (foreignsemicolons == engsemicolons)
                    {
                        string e = englishtext.Substring(englishtext.LastIndexOf(';'));
                        string t = translationtext.Substring(translationtext.LastIndexOf(';'));
                        bad = e.Equals(t) ? null : "Field builder format difference";
                    }

                }
                else if (engsemicolons != foreignsemicolons)    // warn if different number
                {
                    bad = "Field builder semicolon count";
                }

            }

            if (bad != null)
            {
                return $"*** {reportfile}:{reportline} : {bad}: {reportid}: '{englishtext}' -> '{translationtext}'";
            }
            else
                return null;
        }
    }

}

