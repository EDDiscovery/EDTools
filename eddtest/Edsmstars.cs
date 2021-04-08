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

using BaseUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EDDTest
{
    public class EDSMStars
    {
        public static void Process(CommandArgs args)
        {
            string filename = args.Next();

            using (StreamReader sr = new StreamReader(filename))         // read directly from file..
            {
                using (JsonTextReader jr = new JsonTextReader(sr))
                {
                    Parse(jr,args.Next());
                }
            }
        }

        public class SectorRecord
        {
            public string name;
            public int total;
            public double xmin;
            public double xmax;
            public double zmin;
            public double zmax;
            public int maxn1;
            public int maxn2;
            public SectorRecord(string n)
            {
                name = n;
                total = 0;
                xmin = Double.MaxValue;
                zmin = Double.MaxValue;
                xmax = Double.MinValue;
                zmax = Double.MinValue;
                maxn1 = maxn2 = 0;
            }
        }


        public class DBRecord
        {
            public string part1;
            public string part2;
            public double x;
            public double y;
            public double z;
            public DBRecord(string p1, string p2, double x, double y, double z)
            {
                this.part1 = p1;
                this.part2 = p2;
                this.x = x; this.y = y; this.z = z;
            }
        }

        public static void Parse(JsonTextReader jr, string outfile)
        {
            int limit = 100000;

            List<SectorRecord> sectorrecord = new List<SectorRecord>();
            List<string> na = new List<string>();

            while (limit-->0)
            {
                if (jr.Read())
                {
                    if (jr.TokenType == JsonToken.StartObject)
                    {
                        EDSMDumpSystem sys = EDSMDumpSystem.Deserialize(jr);
                        // Console.Write("System " + sys.name + " @ " + sys.coords.x + "," + sys.coords.y + "," + sys.coords.z);

                        if (sys.name == "CD-49 11413")
                        {

                        }

                        EliteNameClassifier el = new EliteNameClassifier();
                        el.Classify(sys.name);

                        if (true)
                        {
                            if (el.IsStandard)
                                Console.WriteLine(sys.name + " - > " + el.SectorName + ":" + el.L1 + " " + el.L2 + " " + el.L3 + " " + el.MassCode + " " + el.N1 + " " + el.N2);
                            else
                                Console.WriteLine(sys.name + " * > " + (el.SectorName??"NS") + ":" + el.StarName);
                        }

                        string scl = el.SectorName ?? "NA";

                        SectorRecord f = sectorrecord.Find(x => x.name == scl);
                        if ( f == null )
                        {
                            f = new SectorRecord(scl);
                            sectorrecord.Add(f);
                        }

                        f.total++;
                        f.xmax = Math.Max(f.xmax, sys.coords.x);
                        f.zmax = Math.Max(f.zmax, sys.coords.z);
                        f.xmin = Math.Min(f.xmin, sys.coords.x);
                        f.zmin = Math.Min(f.zmin, sys.coords.z);
                        f.maxn1 = Math.Max(f.maxn1, el.N1);
                        f.maxn2 = Math.Max(f.maxn2, el.N2);

                    }
                }
            }

            sectorrecord.Sort(delegate (SectorRecord left, SectorRecord right) { return left.total-right.total; });
            na.Sort();

            using (StreamWriter writer = new StreamWriter(outfile))
            {
                int maxn1 = 0;
                int maxn2 = 0;

                foreach (var f in sectorrecord)
                {
                    double xsize = f.xmax - f.xmin;
                    double zsize = f.zmax - f.zmin;
                    double area = xsize * zsize;
                    writer.Write("Sector " + f.name+ " number " + f.total + " X:" + f.xmin + ".." + f.xmax + " Z:" + f.zmin + ".." + f.zmax + " n1:" + f.maxn1 + " n2:" + f.maxn2 + " A" + area.ToString("0"));
                    if (area > 10000 * 10000)
                        writer.Write(" V LARGE");
                    else if (area > 1000 * 1000)
                        writer.Write(" LARGE");
                    else if (area > 5000 * 5000)
                        writer.Write(" MED");
                    else
                        writer.Write(" SMALL");

                    writer.WriteLine();

                    maxn1 = Math.Max(maxn1, f.maxn1);
                    maxn2 = Math.Max(maxn2, f.maxn2);
                }

                writer.WriteLine("Max n1 = " + maxn1 + " Max n2 = " + maxn2);
                writer.WriteLine("Unnormalised");

                foreach ( var name in na)
                { 
                    writer.WriteLine( name );
                }
            }

        }


        private class EDSMDumpSystem
        {
            public static EDSMDumpSystem Deserialize(JsonReader rdr)
            {
                EDSMDumpSystem s = new EDSMDumpSystem();

                while (rdr.Read() && rdr.TokenType == JsonToken.PropertyName)
                {
                    string name = rdr.Value as string;
                    switch (name)
                    {
                        case "name": s.name = rdr.ReadAsString(); break;
                        case "id": s.id = rdr.ReadAsInt32() ?? 0; break;
                        case "date": s.date = rdr.ReadAsDateTime() ?? DateTime.MinValue; break;
                        case "coords": s.coords = EDSMDumpSystemCoords.Deserialize(rdr); break;
                        default: rdr.Read(); JToken.Load(rdr); break;
                    }
                }

                return s;
            }

            public string name;
            public long id;
            public DateTime date;
            public EDSMDumpSystemCoords coords;
        }

        private class EDSMDumpSystemCoords
        {
            public static EDSMDumpSystemCoords Deserialize(JsonReader rdr)
            {
                EDSMDumpSystemCoords c = new EDSMDumpSystemCoords();

                if (rdr.TokenType != JsonToken.StartObject)
                    rdr.Read();

                while (rdr.Read() && rdr.TokenType == JsonToken.PropertyName)
                {
                    string name = rdr.Value as string;
                    switch (name)
                    {
                        case "x": c.x = rdr.ReadAsDouble() ?? Double.NaN; break;
                        case "y": c.y = rdr.ReadAsDouble() ?? Double.NaN; break;
                        case "z": c.z = rdr.ReadAsDouble() ?? Double.NaN; break;
                    }
                }

                return c;
            }

            public double x;
            public double y;
            public double z;
        }


        //We  need to, write a db handler which can:
        //read in the json input, keep the last time, etc, to know how to update it 
        //provide compatible lookups..
        //then we can replace large parts of the system db.
    }


    public class EliteNameClassifier
    {
        public string SectorName;        // set only for surveys and standard names
        public string StarName;          // set for surveys and non standard names

        public char L1;                 // standard naming
        public char L2;
        public char L3;
        public char MassCode;
        public int N1;
        public int N2;

        public bool IsStandard { get { return L1 != ' '; } }
        public bool IsSurvey { get { return L1 == ' ' && SectorName != null; } }

        public EliteNameClassifier()
        {

        }

        public void Classify(string starname)
        {
            L1 = ' ';

            string[] nameparts = starname.Split(' ');

            for (int i = 0; i < nameparts.Length - 1; i++)
            {
                if (i > 0 && nameparts[i].Length == 4 && nameparts[i][2] == '-' && char.IsLetter(nameparts[i][0]) && char.IsLetter(nameparts[i][1]) && char.IsLetter(nameparts[i][3]))
                {
                    string p = nameparts[i + 1];
                    int slash = nameparts[i + 1].IndexOf("-");

                    N1 = N2 = 0;

                    if (slash >= 0)
                    {
                        N1 = p.Substring(1, slash - 1).InvariantParseInt(-1);
                        N2 = p.Substring(slash + 1).InvariantParseInt(-1);
                    }
                    else
                    {
                        N2 = p.Substring(1).InvariantParseInt(-1);
                    }

                    if (N1 >= 0 && N2 >= 0)     // accept
                    {
                        MassCode = p[0];
                        L1 = nameparts[i][0];
                        L2 = nameparts[i][1];
                        L3 = nameparts[i][3];
                        SectorName = nameparts[0];
                        for (int j = 1; j < i; j++)
                            SectorName = SectorName + " " + nameparts[j];
                    }

                    break;
                }
            }

            if (L1 == ' ')
            {
                string[] surveys = new string[] { "HIP", "2MASS", "HD", "LTT", "TYC", "NGC", "HR", "LFT", "LHS", "LP", "Wolf" };

                if (surveys.Contains(nameparts[0]))
                {
                    SectorName = nameparts[0];
                    StarName = starname.Substring(nameparts[0].Length + 1);
                }
                else
                {
                    StarName = starname;
                }
            }
        }
    }

}
