/*
 * Copyright © 2023 - 2023 robbyxp @ github.com
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

using EliteDangerousCore;
using System;
using System.Linq;

namespace EDDTest
{
    static public partial class EDDIData
    {
        static public void CheckSignalsvsEDDI()
        {
            foreach (var sig in EDDI.GetSignalSources())
            {
                string translation = Identifiers.Get("$"+sig.Name,true);

                if (translation == null)
                {
                    Console.WriteLine($"EDDI Signal {sig.Name} not found");
                }
                else
                {
                  //  Console.WriteLine($"EDDI Signal {sig.Name} matches {translation}");
                }

                if ( sig.AltName != null)
                {
                    translation = Identifiers.Get("$"+sig.AltName, true);
                    if (translation == null)
                    {
                        Console.WriteLine($"EDDI Signal {sig.AltName} not found");
                    }
                }
            }
        }

    }
}

