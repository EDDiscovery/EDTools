/*
 * Copyright 2015 - 2025 robbyxp @ github.com
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
using QuickJSON;
using System;
using System.IO;

namespace EDDTest
{
    public partial class JournalCreator
    {
        bool keydelay = false;
        int msdelay = 0;

        public void JournalEntry( CommandArgs args)
        {
            if (args.Left == 1)
            {
                string name = args.Next();
                Console.WriteLine(Help(name, true));
                return;
            }

            filename = args.Next();
            cmdrname = args.Next();

            if (args.Left == 0)
            {
                Console.WriteLine(Help("",true));
                return;
            }

            Process(args, 0);
        }

        private enum RetCodes { Failed, End, EndLoop };

        private RetCodes Process(CommandArgs args, int repeatcount)
        {
            while (args.Left > 0)       // play thru all of the entries
            {
                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                    return RetCodes.Failed;

                if ( args.Peek.ToLower() == "endloop")      // when in a process loop, indicate endloop to upper level
                {
                    return RetCodes.EndLoop;
                }
                else if (args.PeekAndRemoveIf("Loop"))
                {
                    int? loopcount = args.IntNull();
                    if (loopcount.HasValue)
                    {
                        CommandArgs posatloop = new CommandArgs(args);
                        int ourrepeatcount = 0;

                        while (loopcount.Value > 0)
                        {
                            loopcount--;
                            while (args.Left > 0)
                            {
                                var ret = Process(args,ourrepeatcount++);

                                if (ret == RetCodes.Failed)
                                    return ret;

                                if ( ret == RetCodes.EndLoop)
                                {
                                    args.Remove();
                                    if (loopcount.Value > 0)
                                        args = new CommandArgs(posatloop);      // reset pos
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Missing loop count");
                        return RetCodes.Failed;
                    }
                }
                else if (args.PeekAndRemoveIf("msDelay"))
                {
                    int? v = args.IntNull();
                    if (v.HasValue)
                        msdelay = v.Value;
                    else
                    {
                        Console.WriteLine("Missing millisecond delay");
                        return RetCodes.Failed;
                    }
                }
                else if (args.PeekAndRemoveIf("pause"))
                {
                    int? v = args.IntNull();
                    if (v.HasValue)
                        System.Threading.Thread.Sleep(v.Value);
                    else
                    {
                        Console.WriteLine("Missing Pause delay ms");
                        return RetCodes.Failed;
                    }
                }
                else if (args.PeekAndRemoveIf("KeyDelay"))
                {
                    keydelay = true;
                }
                else if (args.PeekAndRemoveIf("NoKeyDelay"))
                {
                    keydelay = false;
                }
                else if (args.PeekAndRemoveIf("stargrid"))
                {
                    string path = args.Next();
                    stargrid = new CSVFile();
                    if (path == null || !stargrid.Read(path))
                    {
                        Console.WriteLine("Missing or bad star grid csv file");
                        return RetCodes.Failed;
                    }
                }
                else if (args.PeekAndRemoveIf("gameversion"))
                {
                    gameversion = args.Next();
                    if (gameversion == null)
                    {
                        Console.WriteLine("Missing or bad gameversion");
                        return RetCodes.Failed;
                    }
                }
                else if (args.PeekAndRemoveIf("build"))
                {
                    build = args.Next();
                    if (build == null)
                    {
                        Console.WriteLine("Missing or bad build");
                        return RetCodes.Failed;
                    }
                }
                else if (args.PeekAndRemoveIf("nogameversiononloadgame"))
                {
                    nogameversiononloadgame = true;
                }
                else if (args.PeekAndRemoveIf("horizons"))
                {
                    odyssey = false;
                }
                else if (args.PeekAndRemoveIf("3.8"))
                {
                    gameversion = "3.8.1.2";
                    odyssey = false;
                    nogameversiononloadgame = true;
                }
                else if (args.PeekAndRemoveIf("beta"))
                {
                    gameversion = "2.2 (Beta 2)";
                }
                else if (args.PeekAndRemoveIf("dayoffset"))
                {
                    int days = args.Int();
                    JournalExtensions.DateTimeOffset = new TimeSpan(days, 0, 0, 0);
                }
                else
                {
                    if (!createJournalEntryWrapped(args, repeatcount, keydelay, msdelay))
                        return RetCodes.Failed;
                }
            }

            return RetCodes.End;
        }


        public bool createJournalEntryWrapped(CommandArgs args, int repeatcount, bool keydelay, int msdelay)
        {
            bool ok = createJournalEntry(args, repeatcount);
            if (ok)
            {
                if (keydelay)
                {
                    Console.WriteLine("Press a key (Escape quit)");
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        return false;
                }
                else if (msdelay > 0)
                {
                    System.Threading.Thread.Sleep(msdelay);
                }
            }
            
            return ok;
        }
    }
}
