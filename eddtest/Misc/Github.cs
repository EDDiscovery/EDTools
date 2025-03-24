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

using QuickJSON;
using System;
using System.Collections.Generic;
using System.IO;

namespace EDDTest
{
    public static class GitHub
    {
        struct Info
        {
            public string name;
            public DateTime time;
            public int downloads;
        }
        public static void Stats(string filename)
        {
            //string url = @"https://api.github.com/repos/EDDiscovery/EDDiscovery/releases?per_page=500";

            string json = File.ReadAllText(filename);

            List<Info> releaseinfo = new List<Info>();

            JArray top = JArray.Parse(json);
            foreach( JToken jo in top)
            {
                JObject release = jo.Object();

                Info i = new Info();

                i.name = release["tag_name"].Str();
                i.time = release["published_at"].DateTimeUTC();
                i.downloads = 0;
                JArray assets = release["assets"].Array();
                foreach( JToken asset in assets)
                {
                    JObject assetinfo = asset.Object();
                    if ( assetinfo["name"].Str().Contains(".exe"))
                    {
                        i.downloads = assetinfo["download_count"].Int();
                    }
                }

//                Console.WriteLine("Release " + i.name + " on " + i.time.ToShortDateString() + " " + i.downloads);
                releaseinfo.Add(i);
            }

            for( int  i = releaseinfo.Count -2; i >= 0; i--)
            {
                TimeSpan ts = releaseinfo[i].time - releaseinfo[i + 1].time;
                double downloadsperday = (double)releaseinfo[i].downloads / (double)ts.Days;
                Console.WriteLine("Release " + releaseinfo[i].name + " on " + releaseinfo[i].time.ToShortDateString() + " " + 
                                releaseinfo[i].downloads.ToString("00000") + " days " + ts.Days.ToString("0000") + " Rate " + downloadsperday);
            }


        }
    }
}
