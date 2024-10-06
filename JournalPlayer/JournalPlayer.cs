using QuickJSON;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JournalPlayer
{
    public partial class JournalPlayerForm : Form
    {
        string SourceFolder { get { return settings["Source"].Str(@"C:\Users\RK\Saved Games\Frontier Developments\Elite Dangerous"); } set { settings["Source"] = value; } }
        string DestFolder { get { return settings["Dest"].Str(@"c:\code\logs\test"); } set { settings["Dest"] = value; } }
        string Pattern { get { return settings["Pattern"].Str("journal*.log"); } set { settings["Pattern"] = value; } }
        string GoToEventName { get { return settings["GoToEventName"].Str(""); } set { settings["GoToEventName"] = value; } }
        string AutoSkip { get { return settings["AutoSkip"].Str("Music;ReservoirReplenished;ShipLocker;SuitLoadout;Backpack;Loadout"); } set { settings["AutoSkip"] = value; } }
        DateTime Starttime { get { return settings["Starttime"].DateTime(new DateTime(2014, 1, 1, 1, 1, 1, 0, DateTimeKind.Utc), System.Globalization.CultureInfo.InvariantCulture); } set { settings["Starttime"] = value.ToStringZulu(); } }
        DateTime Endtime { get { return settings["Endtime"].DateTime(new DateTime(2049, 1, 1, 1, 1, 1, 0, DateTimeKind.Utc), System.Globalization.CultureInfo.InvariantCulture); } set { settings["Endtime"] = value.ToStringZulu(); } }
        bool UseCurrentTime { get { return settings["UseCurrentTime"].Bool(); } set { settings["UseCurrentTime"] = value; } }

        public JournalPlayerForm()
        {
            InitializeComponent();

            string dpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"JournalPlayer");
            BaseUtils.FileHelpers.CreateDirectoryNoError(dpath);
            datafile = Path.Combine(dpath,"settings.json");

            settings = new JObject();

            if ( File.Exists(datafile))
            {
                string text = File.ReadAllText(datafile);
                if (text != null)
                    settings = JObject.Parse(text);
            }

            textBoxDestFolder.Text = DestFolder;
            textBoxSourceFolder.Text = SourceFolder;
            textBoxPattern.Text = Pattern;
            dateTimePickerStartDate.Value = Starttime;
            dateTimePickerEndDate.Value = Endtime;
            checkBoxUseCurrentTime.Checked = UseCurrentTime;
            textBoxAutoSkip.Text = AutoSkip;
            textBoxGotoEntry.Text = GoToEventName;

            this.textBoxSourceFolder.TextChanged += new System.EventHandler(this.textBoxSourceFolder_TextChanged);
            this.textBoxDestFolder.TextChanged += new System.EventHandler(this.textBoxDestFolder_TextChanged);
            this.textBoxPattern.TextChanged += new System.EventHandler(this.textBoxPattern_TextChanged);
            this.dateTimePickerStartDate.ValueChanged += new System.EventHandler(this.dateTimePickerStartDate_ValueChanged);
            this.dateTimePickerEndDate.ValueChanged += new System.EventHandler(this.dateTimePickerEndDate_ValueChanged);
            this.checkBoxUseCurrentTime.CheckedChanged += new System.EventHandler(this.checkboxUseCurrentTime_ValueChanged);
            this.textBoxGotoLineNo.KeyDown += TextBoxGotoLineNo_KeyDown;
            this.textBoxGotoEntry.KeyDown +=  TextBoxGotoEntry_KeyDown;
            this.textBoxAutoSkip.TextChanged += TextBoxAutoSkip_TextChanged;

            Clear();

            tme.Tick += Tme_Tick;

            EliteDangerousCore.MaterialCommodityMicroResourceType.FillTable();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            File.WriteAllText(datafile,settings.ToString(true));
            base.OnClosed(e);
        }

        private void Tme_Tick(object sender, EventArgs e)
        {
            buttonStep_Click(null, null);
        }


        private void textBoxSourceFolder_TextChanged(object sender, EventArgs e)
        {
            Clear();
            SourceFolder = textBoxSourceFolder.Text;

        }
        private void textBoxDestFolder_TextChanged(object sender, EventArgs e)
        {
            Clear();
            DestFolder = textBoxDestFolder.Text;
        }
        private void textBoxPattern_TextChanged(object sender, EventArgs e)
        {
            Clear();
            Pattern = textBoxPattern.Text;
        }

        private void TextBoxAutoSkip_TextChanged(object sender, EventArgs e)
        {
            AutoSkip = textBoxAutoSkip.Text;
        }

        private void checkboxUseCurrentTime_ValueChanged(object sender, EventArgs e)
        {
            Clear();
            UseCurrentTime = checkBoxUseCurrentTime.Checked;
        }
        private void buttonClearDestFolder_Click(object sender, EventArgs e)
        {
            BaseUtils.FileHelpers.DeleteFiles(DestFolder, "*.log", new TimeSpan(0), 0);
            BaseUtils.FileHelpers.DeleteFiles(DestFolder, "*.json", new TimeSpan(0), 0);
        }

        private void dateTimePickerStartDate_ValueChanged(object sender, EventArgs e)
        {
            Clear();
            Starttime = dateTimePickerStartDate.Value;
        }


        private void dateTimePickerEndDate_ValueChanged(object sender, EventArgs e)
        {
            Clear();
            Endtime = dateTimePickerEndDate.Value;
        }

        private void buttonRestart_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            tme.Stop();
        }

        private void Goto(string s)
        {
            tme.Stop();
            tme.Interval = 100;
            stoponevent = s;
            tme.Start();
        }
        private void StepAt(int v)
        {
            tme.Stop();
            tme.Interval = v;
            stoponevent = null;
            tme.Start();
        }

        private void button50ms_Click(object sender, EventArgs e)
        {
            tme.Stop();
            StepAt( 50 );
            tme.Start();
        }

        private void button100ms_Click(object sender, EventArgs e)
        {
            tme.Stop();
            StepAt( 100 );
            tme.Start();
        }

        private void button250ms_Click(object sender, EventArgs e)
        {
            tme.Stop();
            StepAt( 250 );
            tme.Start();
        }

        private void button500ms_Click(object sender, EventArgs e)
        {
            tme.Stop();
            StepAt( 500 );
            tme.Start();
        }
        private void button1s_Click(object sender, EventArgs e)
        {
            tme.Stop();
            StepAt( 1000 );
            tme.Start();
        }

        private void buttonLocation_Click(object sender, EventArgs e)
        {
            Goto( "Location");
        }

        private void buttonScan_Click(object sender, EventArgs e)
        {
            Goto("Scan");
        }

        private void buttonFSDJump_Click(object sender, EventArgs e)
        {
            Goto("FSDJump");
        }

        private void buttonStartJump_Click(object sender, EventArgs e)
        {
            Goto( "StartJump");
        }

        private void buttonMarketBuy_Click(object sender, EventArgs e)
        {
            Goto( "MarketBuy");
        }

        private void TextBoxGotoLineNo_KeyDown(object sender, KeyEventArgs e)
        {
            tme.Stop();

            if (e.KeyCode == Keys.Enter)
                buttonGoToLine_Click(null, null);
        }

        private void buttonGoToLine_Click(object sender, EventArgs e)
        {
            int v = textBoxGotoLineNo.Text.InvariantParseInt(0);

            if (v > 0 && v > curlineno)  // if parsed, and in future
            {
                stoponline = v;
                tme.Interval = 50;
                tme.Start();
            }
            else
                Console.Beep(512, 100);
        }

        private void TextBoxGotoEntry_KeyDown(object sender, KeyEventArgs e)
        {
            tme.Stop();     // stop all anyway

            if (e.KeyCode == Keys.Enter)
                buttonGotoEntry_Click(null, null);
        }

        private void buttonGotoEntry_Click(object sender, EventArgs e)
        {
            if ( textBoxGotoEntry.Text.Length>0)
            {
                GoToEventName = textBoxGotoEntry.Text;
                Goto(textBoxGotoEntry.Text);
            }
            else
                Console.Beep(512, 100);
        }

        private void buttonSelectSourceFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = textBoxSourceFolder.Text;
            fbd.Description = "Select folder where Journal*.log files are found";

            if (fbd.ShowDialog(this) == DialogResult.OK)
                textBoxSourceFolder.Text = fbd.SelectedPath;

        }

        private void buttonSelectDestFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = textBoxDestFolder.Text;
            fbd.Description = "Select folder where Journal*.log files are to be stored";

            if (fbd.ShowDialog(this) == DialogResult.OK)
                textBoxDestFolder.Text = fbd.SelectedPath;

        }

        private void buttonViewInput_Click(object sender, EventArgs e)
        {
            if (textBoxJournalFile.Text.HasChars() && textBoxJournalFile.Text != "None")
            {
                System.Diagnostics.Process.Start(textBoxJournalFile.Text);
            }
        }

        private void Clear()
        {
            if (sr != null)
            {
                sr.Close();
                sr = null;
                fs.Close();
                fs = null;
            }

            files = null;
            stoponevent = null;
            richTextBoxCurrentEntry.Clear();
            richTextBoxNextEntry.Clear();
            textBoxJournalFile.Text = "None";
            richTextBoxCurrentEntry.Text = richTextBoxNextEntry.Text = "";
            textBoxOutputFile.Text = "";
            tme.Stop();
        }

        System.Random rnd = new System.Random(1);
        Dictionary<string, JArray> synthesisedmarkets = new Dictionary<string, JArray>();

        private void WriteEntryInNextBox()
        {
            if (richTextBoxNextEntry.Text.Length > 0)        // if we have a previous next entry
            {
                string outdir = Path.GetDirectoryName(outfilepath);

                JObject json = richTextBoxNextEntry.Tag as JObject;     // pick up next JSON to play

                if (json!=null)
                {
                    string eventn = json["event"].Str().ToLowerInvariant();
                    switch (eventn)
                    {
                        case "cargo":
                            {
                                json["Inventory"] = new JArray()
                            {
                                new JObject { ["Name"] = "drones", ["Name_Localised"] = "Limpet", ["Count"] = 15, ["Stolen"] = 0 }
                            };

                                BaseUtils.FileHelpers.TryWriteToFile(Path.Combine(outdir, "Cargo.json"), json.ToString());

                                break;
                            }
                        case "fcmaterials":
                            {
                                JSONFormatter jf = new JSONFormatter();
                                jf.Array();
                                jf.Object().V("id", 128961524).V("Name", "$aerogel_name;").V("Name_Localised", "Aerogel").V("Price", 500).V("Stock", 0).V("Demand", 54).Close();
                                jf.Object().V("id", 128972283).V("Name", "$airqualityreports_name;").V("Name_Localised", "Air Quality Reports").V("Price", 30000).V("Stock", 1).V("Demand", 0).Close();
                                jf.Object().V("id", 128961526).V("Name", "$carbonfibreplating_name;").V("Name_Localised", "Carbon Fibre Plating").V("Price", 610).V("Stock", 0).V("Demand", 326).Close();

                                json["Items"] = JArray.Parse(jf.Get());
                                BaseUtils.FileHelpers.TryWriteToFile(Path.Combine(outdir, "FCMaterials.json"), json.ToString());

                                break;
                            }
                        case "backpack":
                            {
                                break;
                            }
                        case "resupply":
                            {
                                break;
                            }
                        case "moduleinfo":
                            {
                                JSONFormatter jf = new JSONFormatter();
                                jf.Array()
                                .Object().V("Slot", "MainEngines").V("Item", "int_engine_size5_class5").V("Power", 6.8544).V("Priority", 0).Close()
                                .Object().V("Slot", "Slot01_Size5").V("Item", "int_shieldgenerator_size5_class5").V("Power", 3.64).V("Priority", 0).Close()
                                .Object().V("Slot", "Slot02_Size4").V("Item", "int_shieldcellbank_size4_class4").V("Power", 1.48).V("Priority", 3).Close()
                                .Object().V("Slot", "TinyHardpoint3").V("Item", "hpt_shieldbooster_size0_class5").V("Power", 1.38).V("Priority", 0).Close()
                                .Object().V("Slot", "HugeHardpoint1").V("Item", "hpt_multicannon_gimbal_huge").V("Power", 1.22).V("Priority", 0).Close()
                                .Object().V("Slot", "MediumHardpoint4").V("Item", "hpt_dumbfiremissilerack_fixed_medium").V("Power", 1.2).V("Priority", 0).Close()
                                .Object().V("Slot", "TinyHardpoint4").V("Item", "hpt_shieldbooster_size0_class4").V("Power", 1.15).V("Priority", 0).Close()
                                .Object().V("Slot", "MediumHardpoint1").V("Item", "hpt_beamlaser_gimbal_medium").V("Power", 1.0).V("Priority", 4).Close();

                                json["Items"] = JArray.Parse(jf.Get());
                                BaseUtils.FileHelpers.TryWriteToFile(Path.Combine(outdir, "ModulesInfo.json"), json.ToString());

                                break;
                            }
                        case "navroute":
                            {
                                JSONFormatter jf = new JSONFormatter();
                                jf.Array()
                                .Object().V("StarSystem", "i Bootis").V("SystemAddress", 1281787693419).Array("StarPos").V(-22.375).V(38.84375).V(4).Close().V("StarClass", "G").Close();
                                json["Route"] = JArray.Parse(jf.Get());
                                BaseUtils.FileHelpers.TryWriteToFile(Path.Combine(outdir, "NavRoute.json"), json.ToString());
                                break;
                            }
                        case "shiplocker":
                            {
                                json["Items"] = new JArray()
                            {
                                new JObject { ["Name"] = "chemicalsample", ["Name_Localised"] = "Chemical Sample", ["Count"] = 1, ["OwnerID"] = 0 }
                            };

                                BaseUtils.FileHelpers.TryWriteToFile(Path.Combine(outdir, "ShipLocker.json"), json.ToString());

                                break;
                            }
                        case "market":
                            {
                                string station = json["StationName"].Str();

                                if (!synthesisedmarkets.TryGetValue(station, out JArray ja))        // if we have NOT synthed it, synth it
                                {
                                    JSONFormatter jf = new JSONFormatter();
                                    jf.Array();
                                    var commds = EliteDangerousCore.MaterialCommodityMicroResourceType.GetCommodities(EliteDangerousCore.MaterialCommodityMicroResourceType.SortMethod.None);
                                    foreach (var x in commds)
                                    {
                                        if (rnd.Next(5) == 0)       // randomly pick
                                        {
                                            int buyprice = 100 + rnd.Next(200);
                                            int sellprice = buyprice - 50;
                                            int stock = 12000 + rnd.Next(12000);
                                            jf.Object().V("id", (int)x.FDType).V("Name", "$" + x.FDType.ToString() + "_name;").V("Name_Localised", x.Type.ToString())
                                                            .V("Category", "$MARKET_category_" + x.Type.ToString())
                                                            .V("Category_Localised", x.Type.ToString())
                                                            .V("BuyPrice", buyprice)
                                                            .V("SellPrice", sellprice)
                                                            .V("MeanPrice", (buyprice + sellprice) / 2)
                                                            .V("StockBracket", 0).V("DemandBracket", 3)
                                                            .V("Stock", stock)
                                                            .V("Demand", 273)
                                                            .V("Consumer", true).V("Producer", true).V("Rare", x.Rarity)
                                                            .Close();
                                        }
                                    }

                                    string items = jf.Get();        // get it as string
                                    ja = JArray.Parse(items);       // turn back in JTokens
                                    synthesisedmarkets[station] = ja;   // remember
                                }

                                json["Items"] = ja; // replace and set Items to synthesised list

                                BaseUtils.FileHelpers.TryWriteToFile(Path.Combine(outdir, "Market.json"), json.ToString(true));

                                break;
                            }
                        case "outfitting":
                            {
                                JSONFormatter jf = new JSONFormatter();
                                jf.Array();
                                jf.Object().V("id", 128788702).V("Name", "hpt_atmulticannon_fixed_large").V("BuyPrice", 1151963).Close();
                                jf.Object().V("id", 128793060).V("Name", "hpt_atmulticannon_turret_large").V("BuyPrice", 3726060).Close();
                                jf.Object().V("id", 129022088).V("Name", "hpt_atmulticannon_gimbal_large").V("BuyPrice", 2330699).Close();
                                jf.Object().V("id", 128935980).V("Name", "hpt_multicannon_fixed_medium_advanced").V("BuyPrice", 37050).Close();
                                JArray ja = JArray.Parse(jf.Get());

                                json["Items"] = ja;

                                BaseUtils.FileHelpers.TryWriteToFile(Path.Combine(outdir, "Outfitting.json"), json.ToString());

                                break;
                            }
                        case "shipyard":
                            {
                                JSONFormatter jf = new JSONFormatter();
                                jf.Array();

                                jf.Object().V("id", 128049249).V("ShipType", "sidewinder").V("ShipPrice", 31200).Close();
                                jf.Object().V("id", 128049261).V("ShipType", "hauler").V("ShipPrice", 51402).Close();
                                jf.Object().V("id", 128049273).V("ShipType", "viper").V("ShipType_Localised", "Viper Mk III").V("ShipPrice", 139358).Close();
                                jf.Object().V("id", 128672255).V("ShipType", "viper_mkiv").V("ShipType_Localised", "Viper Mk IV").V("ShipPrice", 426983).Close();
                                jf.Object().V("id", 128671217).V("ShipType", "diamondback").V("ShipType_Localised", "Diamondback Scout").V("ShipPrice", 550221).Close();
                                jf.Object().V("id", 128671831).V("ShipType", "diamondbackxl").V("ShipType_Localised", "Diamondback Explorer").V("ShipPrice", 1847391).Close();
                                jf.Object().V("id", 128672269).V("ShipType", "independant_trader").V("ShipType_Localised", "Keelback").V("ShipPrice", 3048001).Close();
                                jf.Object().V("id", 128049339).V("ShipType", "python").V("ShipPrice", 55553725).Close();
                                jf.Object().V("id", 128049333).V("ShipType", "type9").V("ShipType_Localised", "Type-9 Heavy").V("ShipPrice", 74641946).Close();

                                json["Items"] = JArray.Parse(jf.Get());
                                BaseUtils.FileHelpers.TryWriteToFile(Path.Combine(outdir, "Shipyard.json"), json.ToString());

                                break;
                            }
                         }
                }
                string outline = richTextBoxNextEntry.Text.Substring(richTextBoxNextEntry.Text.IndexOf(": ") + 1) + Environment.NewLine;
                if (!BaseUtils.FileHelpers.TryAppendToFile(outfilepath, outline, true))
                {
                    MessageBox.Show($"Can't write {outfilepath}");
                    tme.Stop();
                }
                richTextBoxCurrentEntry.Text = richTextBoxNextEntry.Text;
            }
        }

        private void buttonStep_Click(object sender, EventArgs e)
        {
            if (files == null )
            {
                if (Directory.Exists(DestFolder))
                {
                    if (Directory.Exists(SourceFolder))
                    {
                        System.Diagnostics.Debug.WriteLine($"Find {Pattern} in {SourceFolder} where Date > {Starttime} < {Endtime}");
                        files = Directory.EnumerateFiles(SourceFolder, Pattern, SearchOption.TopDirectoryOnly)
                            .Select(f => new FileInfo(f)).Where(t => t.LastWriteTime >= Starttime && t.LastWriteTime <= Endtime).OrderBy(p => p.LastWriteTime).ToArray();

                        if (files.Length > 0)
                        {
                            fileentry = 0;
                        }
                        else
                        {
                            files = null;
                            MessageBox.Show($"No log files found in {SourceFolder}");
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Source folder does not exist");
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Dest folder does not exist");
                    return;
                }
            }

            while (true)
            {
                if (fileentry >= files.Length)
                {
                    WriteEntryInNextBox();
                    Clear();
                    MessageBox.Show("No more log files");
                    return;
                }

                if (fs == null)
                {
                    outfilename = UseCurrentTime ? "Journal." + DateTime.UtcNow.ToString("yyyy-MM-ddThhmmss") + ".01.log" : Path.GetFileName(files[fileentry].FullName);

                    string writefile = Path.Combine(DestFolder, outfilename);
                    if (Array.FindIndex(files, x => x.FullName.EqualsIIC(writefile)) >= 0)
                    {
                        MessageBox.Show("File name clash");
                        Clear();
                        return;
                    }

                    fs = new FileStream(files[fileentry].FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    sr = new StreamReader(fs);
                    curlineno = 0;
                    textBoxJournalFile.Text = files[fileentry].FullName;
                }

                string line = sr.ReadLine();
                curlineno++;

                if (line == null)
                {
                    stoponline = 0;     // cancel stop on line
                    sr.Close();
                    sr = null;
                    fs.Close();
                    fs = null;
                    fileentry++;
                }
                else
                {
                    WriteEntryInNextBox();

                    JObject jo = JObject.Parse(line);
                    string eventname = "????????????";

                    if (jo != null)
                    {
                        eventname = jo["event"].Str();
                        if ( UseCurrentTime )
                        {
                            jo["timestamp"] = DateTime.UtcNow.StartOfSecond().ToStringZuluInvariant();
                            line = jo.ToString();
                        }
                    }

                    richTextBoxNextEntry.Tag = jo;

                    richTextBoxNextEntry.Text = curlineno.ToStringInvariant() + ": " + line;
                    outfilepath = Path.Combine(DestFolder, outfilename);        // set here as when we change we need to write to the last file in WriteLast()
                    
                    textBoxOutputFile.Text = outfilepath;

                    string[] skips = AutoSkip.Split(';');

                    if ( Array.IndexOf(skips,eventname) >= 0)       // if autoskip event, do another one
                    {
                        continue;
                    }

                    if (stoponevent != null )
                    {
                        if ( eventname == stoponevent )
                        {
                            tme.Stop();
                            stoponevent = null;
                        }
                    }
                    else if ( stoponline > 0 && curlineno == stoponline )
                    {
                        tme.Stop();
                        stoponline = 0;
                    }

                    break;
                }
            }
        }


        string datafile;
        JObject settings;
        FileInfo[] files;
        int fileentry;
        int curlineno;
        Stream fs;
        StreamReader sr;
        string outfilename;
        string outfilepath;
        Timer tme = new Timer();
        string stoponevent;
        int stoponline;

    }
}
