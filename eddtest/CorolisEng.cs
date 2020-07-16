using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDDTest
{
    static public class CoriolisEng
    {
        static private string ProcessModules(string json)
        {
            JObject jo = new JObject();
            jo = JObject.Parse(json);

            Dictionary<Tuple<string, int>, string> engresults = new Dictionary<Tuple<string, int>, string>();

            foreach( var top in jo.Children() )
            {
                JObject inner = top.First as JObject;
                var blueprints = inner["blueprints"] as JObject;

                foreach ( var b in blueprints)      // kvp
                {
                    string bname = b.Key;
                    JObject blueprint = b.Value as JObject;
                //    System.Diagnostics.Debug.WriteLine("Blueprint " + blueprint.Path);

                    var grades = blueprint["grades"] as JObject;

                    foreach (var g in grades)
                    {
                        //System.Diagnostics.Debug.WriteLine("Key " + g.Key + " value " + g.Value);
                        var engineers = g.Value["engineers"];
                        foreach (var e in engineers.Children())
                        {
                            var eng = e.Value<string>();
                            System.Diagnostics.Debug.WriteLine("B " + bname + " g:" + g.Key + " e:" + eng);
                            var keyvp = new Tuple<string, int>(inner.Path + "-" + bname, g.Key.InvariantParseInt(0));
                            if ( engresults.ContainsKey(keyvp))
                            {
                                engresults[keyvp] += "," + eng;
                            }
                            else
                            {
                                engresults[keyvp] = eng;
                            }
                        }
                    }
                }

            }

            string res = "";
            foreach( var vkp in engresults)
            {
                int dash = vkp.Key.Item1.IndexOf('-');
                res += "new EngineeringRecipe( \"" + vkp.Key.Item1.Substring(dash+1) + "\", \"?\", \"" + vkp.Key.Item1.Substring(0,dash) + 
                                "\", \"" + vkp.Key.Item2 + "\", \"" + vkp.Value + "\" )," + Environment.NewLine;
            }
            return res;
        }

        static public string ProcessEng(FileInfo[] allFiles)            // overall index of items
        {
            foreach (var f in allFiles)
            {
                if (f.FullName.Contains("modules",StringComparison.InvariantCultureIgnoreCase))
                {
                    return ProcessModules(File.ReadAllText(f.FullName));
                }
            }

            return "Not found";
        }

    }
}
