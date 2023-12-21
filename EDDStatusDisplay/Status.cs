using QuickJSON;
using System;
using System.IO;
using System.Windows.Forms;

namespace EDDStatusDisplay
{
    public partial class Status : Form
    {
        Timer tm = new Timer();
        public Status()
        {
            InitializeComponent();
            tm.Interval = 200;
            tm.Tick += Tm_Tick;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            tm.Start();
        }

        private void Tm_Tick(object sender, EventArgs e)
        {
            string user = Environment.GetEnvironmentVariable("USERNAME");

            string path = @"c:\users\" + user + @"\saved games\frontier developments\elite dangerous\";
            string watchfile = Path.Combine(path, "status.json");

            string laststatus = "";

            string nextstatus = null;

            Stream stream = null;
            try
            {
                stream = File.Open(watchfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                StreamReader reader = new StreamReader(stream);

                nextstatus = reader.ReadToEnd();

                stream.Close();
            }
            catch
            { }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }

            if (nextstatus != null && nextstatus != laststatus)
            {
                JToken j = JToken.Parse(nextstatus);

                if (j != null)
                {
                    SetStatus(j);
                }
            }
        }

        void SetStatus(QuickJSON.JToken json)
        {
            ulong f1 = json["Flags"].ULong();
            ulong f2 = json["Flags2"].ULong();
            double? lat = json["Latitude"].DoubleNull();
            double? lon = json["Longitude"].DoubleNull();
            labelLatLong.Text = lat.HasValue && lon.HasValue ? ("Pos: " + lat.ToStringInvariant("0.####") + " , " + lon.ToStringInvariant("0.####")) : "";
            double ? temp = json["Temperature"].DoubleNull();
            labelTemperature.Text = temp.HasValue ? (temp.ToStringInvariant("0.#") + " K") : "";
            labelWeapon.Text = json["SelectedWeapon_Localised"].Str();
            double? g = json["Gravity"].DoubleNull();
            labelGravity.Text = g.HasValue ? (g.ToStringInvariant("0.##") + " G") : "";
            labelLegalState.Text = "Legal State: " + json["LegalState"].Str();
            labelBody.Text = json["BodyName"].Str();
            double? h = json["Health"].DoubleNull();
            labelHealth.Text = h.HasValue ? ((h * 100).ToStringInvariant("0.##") + " H%") : "";
            double? o = json["Oxygen"].DoubleNull();
            labelOxygen.Text = o.HasValue ? ((o * 100).ToStringInvariant("0.##") + " O2%") : "";
            JObject dest = json["Destination"].Object();
            labelDest.Text = dest != null ? ("Dest: " + dest["Name"].Str() + " (" + dest["Body"].Int() + ")") : "";
            double? f = json["Fuel"].I("FuelMain").DoubleNull();
            double? fr = json["Fuel"].I("FuelReservoir").DoubleNull();
            labelFuelMain.Text = f.HasValue ? ("Fuel: " + f.ToStringInvariant("0.##") + " (" + fr.ToStringInvariant("0.##") + ") T") : "";
            double? cg = json["Cargo"].DoubleNull();
            labelCargo.Text = cg.HasValue ? ("Cargo: " + cg.ToStringInvariant("0") + " T") : "";
            double? bal = json["Balance"].DoubleNull();
            labelBalance.Text = bal.HasValue ? ("Balance: " + bal.ToStringInvariant("0") + " cr") : "";
            int gui = json["GuiFocus"].Int();
            labelGUI.Text = "GUIMode: " + ((FocusValues)gui).ToString();
            int fg = json["FireGroup"].Int();
            labelFiregroup.Text = "Firegroup: " + "ABCDEFGHIJKL"[fg];

            ControlList(this, f1, f2);
        }

        void ControlList(Control c, ulong f1, ulong f2)
        {
            foreach( Control x in c.Controls)
            {
                if (x is RadioButton && x.Tag != null)
                {
                    if (((string)x.Tag).StartsWith("F1-"))
                    {
                        int bit = ((string)x.Tag).Substring(3).InvariantParseInt(0);
                        bool ch = (f1 & (1UL << bit)) != 0;
                        ((RadioButton)x).Checked = ch;
                        System.Diagnostics.Debug.WriteLine($"F1 bit {bit} set {ch}");
                    }
                    if (((string)x.Tag).StartsWith("F2-"))
                    {
                        int bit = ((string)x.Tag).Substring(3).InvariantParseInt(0);
                        bool ch = (f2 & (1UL << bit)) != 0;
                        ((RadioButton)x).Checked = ch;
                        System.Diagnostics.Debug.WriteLine($"F1 bit {bit} set {ch}");
                    }
                }

                ControlList(x, f1, f2);
            }
        }

        public enum FocusValues
        {
            NoFocus = 0,
            SystemPanel = 1,
            TargetPanel = 2,
            CommsPanel = 3, // top
            RolePanel = 4,  // bottom
            StationServices = 5,
            GalaxyMap = 6,
            SystemMap = 7,
            Orrey = 8,        //3.3
            FSSMode = 9, //3.3
            SAAMode = 10,//3.3
            Codex = 11,//3.3
        }
    }
}
