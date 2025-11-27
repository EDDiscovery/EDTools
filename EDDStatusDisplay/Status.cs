using BaseUtils;
using QuickJSON;
using System;
using System.IO;
using System.Windows.Forms;

namespace EDDStatusDisplay
{
    public partial class Status : Form
    {
        Timer tm = new Timer();
        string path, watchfile;
        string laststatus = "";

        public Status()
        {
            InitializeComponent();
            tm.Interval = 200;
            tm.Tick += Tm_Tick;

            string user = Environment.GetEnvironmentVariable("USERNAME");
            path = @"c:\users\" + user + @"\saved games\frontier developments\elite dangerous\";
            watchfile = Path.Combine(path, "status.json");

            foreach( var x in destlist)
            {
                StringParser sp = new StringParser(x);
                string s = sp.NextWord(",");
                comboBoxSelDest.Items.Add(s);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            tm.Start();
        }

        private void Tm_Tick(object sender, EventArgs e)
        {
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
                laststatus = nextstatus;

                JToken j = JToken.Parse(nextstatus);

                if (j != null)
                {
                    ReadStatus(j);
                }
            }
        }

        void ReadStatus(QuickJSON.JToken json)
        {
            flags = json["Flags"].ULong();
            flags2 = json["Flags2"].ULong();

            System.Diagnostics.Debug.WriteLine($"Flags {flags} {flags2}");
            for (int i = 0; i < 32; i++) { if ((flags & (1UL << i)) != 0) System.Diagnostics.Debug.WriteLine($"  Flags1 +{i} "); }
            for (int i = 0; i < 32; i++) { if ((flags2 & (1UL << i)) != 0) System.Diagnostics.Debug.WriteLine($"  Flags2 +{i} "); }

            lat = json["Latitude"].DoubleNull();
            lon = json["Longitude"].DoubleNull();
            labelLatLong.Text = lat.HasValue && lon.HasValue ? ("Pos: " + lat.ToStringInvariant("0.####") + " , " + lon.ToStringInvariant("0.####")) : "";

            heading = json["Heading"].DoubleNull();
            labelHeading.Text = heading.HasValue ? ("H: " + heading.ToStringInvariant("0.0")) : "";

            altitude = json["Altitude"].DoubleNull();
            labelAltitude.Text = altitude.HasValue ? ("A: " + altitude.ToStringInvariant("0.0")) : "";

            planetradius = json["PlanetRadius"].DoubleNull();
            labelRadius.Text = planetradius.HasValue ? ("R: " + planetradius.ToStringInvariant("0.0")) : "";

            temperature = json["Temperature"].DoubleNull();
            labelTemperature.Text = temperature.HasValue ? (temperature.ToStringInvariant("0.#") + " K") : "";

            SelectedWeapon = json["SelectedWeapon"].Str();
            SelectedWeaponLoc = labelWeapon.Text = json["SelectedWeapon_Localised"].Str();

            gravity = json["Gravity"].DoubleNull();
            labelGravity.Text = gravity.HasValue ? (gravity.ToStringInvariant("0.##") + " G") : "";

            legalstate = json["LegalState"].StrNull();
            labelLegalState.Text = "Legal State: " + (legalstate??"-");
            
            bodyname = json["BodyName"].Str();
            labelBody.Text = bodyname;

            health = json["Health"].DoubleNull();
            labelHealth.Text = health.HasValue ? ((health * 100).ToStringInvariant("0.##") + " H%") : "";

            oxygen = json["Oxygen"].DoubleNull();
            labelOxygen.Text = oxygen.HasValue ? ((oxygen * 100).ToStringInvariant("0.##") + " O2%") : "";

            destination = json["Destination"].Object();
            labelDest.Text = destination != null ? ("Dest: " + destination["Name_Localised"].Str().Alt(destination["Name"].Str()) + " (" + destination["Body"].Long() + ")") : "Select Dest";

            fuel = json["Fuel"].I("FuelMain").DoubleNull();
            fuelres = json["Fuel"].I("FuelReservoir").DoubleNull();
            labelFuelMain.Text = fuel.HasValue ? ("Fuel: " + fuel.ToStringInvariant("0.##") + " (" + fuelres.ToStringInvariant("0.##") + ") T") : "";

            cargo = json["Cargo"].DoubleNull();
            labelCargo.Text = cargo.HasValue ? ("Cargo: " + cargo.ToStringInvariant("0") + " T") : "";

            bal = json["Balance"].DoubleNull();
            labelBalance.Text = bal.HasValue ? ("Balance: " + bal.ToStringInvariant("0") + " cr") : "";

            gui = json["GuiFocus"].IntNull();
            labelGUI.Text = gui.HasValue ? ("GUI: " + ((FocusValues)gui).ToString()) : "";

            fg = json["FireGroup"].IntNull();
            labelFiregroup.Text = fg.HasValue ? ("Firegroup: " + "ABCDEFGHIJKL"[fg.Value]) : "";

            pips = json["Pips"].Array();
            if (pips != null)
                labelPips.Text = "Pips:" + (pips[0].Double() / 2).ToStringInvariant() + "/" + (pips[1].Double() / 2).ToStringInvariant() + "/" + (pips[2].Double() / 2).ToStringInvariant();
            else
                labelPips.Text = "No Pips";

            UpdateControls(this, flags, flags2);
        }

        void UpdateControls(Control c, ulong f1, ulong f2)
        {
            foreach (Control x in c.Controls)
            {
                if (x is RadioButton rb && x.Tag != null)
                {
                    rb.Checked = Bit(x.Tag as string);
                }

                UpdateControls(x, f1, f2);
            }
        }

        void WriteStatus()
        {
            JSONFormatter qj = new JSONFormatter();

            qj.Object().UTC("timestamp").V("event", "Status");
            qj.V("Flags", (long)flags);

            System.Diagnostics.Debug.WriteLine($"Write Flags {flags} {flags2}");
            for (int i = 0; i < 32; i++) { if ((flags & (1UL << i)) != 0) System.Diagnostics.Debug.WriteLine($"  Flags1 +{i} "); }
            for (int i = 0; i < 32; i++) { if ((flags2 & (1UL << i)) != 0) System.Diagnostics.Debug.WriteLine($"  Flags2 +{i} "); }

            if (flags != 0 || flags2 != 0)
            {
                qj.V("Flags2", (long)flags2);

                if ((flags2 & (1 << (int)StatusFlags2ShipType.OnFoot)) != 0)
                {
                    if ( oxygen.HasValue)
                        qj.V("Oxygen", oxygen.Value);
                    if ( health.HasValue)
                        qj.V("Health", health.Value);
                    if ( temperature.HasValue)
                        qj.V("Temperature", temperature.Value);
                    if (SelectedWeapon!=null)
                        qj.V("SelectedWeapon", SelectedWeapon);
                    if (SelectedWeaponLoc.HasChars())
                        qj.V("SelectedWeapon_Localised", SelectedWeaponLoc);
                    if ( gravity.HasValue)
                        qj.V("Gravity", gravity.Value);
                }
                else
                {
                    if ( pips!=null)
                        qj.Array("Pips").V(pips[0].Double()).V(pips[1].Double()).V(pips[2].Double()).Close();
                    if ( fg != null)
                        qj.V("FireGroup", fg.Value);
                    if ( gui.HasValue)
                        qj.V("GuiFocus", gui.Value);
                }

                if ((flags & (1 << (int)StatusFlags1ShipType.InMainShip)) != 0 || (flags & (1 << (int)StatusFlags1ShipType.InSRV)) != 0)
                {
                    if ( fuel!=null)
                        qj.Object("Fuel").V("FuelMain", fuel.Value).V("FuelReservoir", 0.32).Close();
                    if ( cargo.HasValue)
                        qj.V("Cargo", cargo.Value);
                }

                if ( legalstate!=null)
                    qj.V("LegalState", legalstate);

                if (lat != null && lon !=null)
                {
                    qj.V("Latitude", lat.Value);
                    qj.V("Longitude", lon.Value);
                    if (heading.HasValue)
                        qj.V("Heading", heading.Value);

                    if (altitude.HasValue)
                        qj.V("Altitude", altitude.Value);
                }

                if (bodyname.HasChars())
                    qj.V("BodyName", bodyname);

                if (planetradius.HasValue)
                    qj.V("PlanetRadius", planetradius.Value);

                if (bal != null)
                    qj.V("Balance", bal.Value);

                if (destination != null)
                {
                    qj.Object("Destination").V("System", destination["System"].Long()).V("Body", destination["Body"].Int()).V("Name", destination["Name"].Str());
                    if (destination.Contains("Name_Localised"))
                        qj.V("Name_Localised", destination["Name_Localised"].Str());
                    qj.Close();
                }

            }


            qj.Close();

            string j = qj.Get();
            System.Diagnostics.Debug.WriteLine(j);
            File.WriteAllText(watchfile, j);
        }

        bool Bit(string ctrl, bool? setit = null)
        {
            int bit = ctrl.Substring(3).InvariantParseInt(0);

            if (ctrl.StartsWith("F1-"))
            {
                if (setit == true)
                    flags |= (1UL << bit);
                else if (setit == false)
                    flags &= ~(1UL << bit);
                else
                    return (flags & (1UL << bit)) != 0;
            }
            else
            {
                if (setit == true)
                    flags2 |= (1UL << bit);
                else if (setit == false)
                    flags2 &= ~(1UL << bit);
                else
                    return (flags2 & (1UL << bit)) != 0;
            }
            return false;
        }

        private void radioButton_MouseDown(object sender, MouseEventArgs e)
        {
            RadioButton x = sender as RadioButton;
            x.Checked = !x.Checked;
            Bit(x.Tag as string, x.Checked);
            WriteStatus();
        }

        void Clear()
        {
            flags = flags2 = 0;
            cargo = null;
            fuel = fuelres = null;
            lat = null; lon = null;
            heading = altitude = planetradius = null;
            temperature = null;
            gravity = null;
            health = null;
            oxygen = null;
            SelectedWeapon = SelectedWeaponLoc = null;
            bodyname = null;
            legalstate = null;
            destination = null;
            bal = null;
            gui = 0;
            fg = null;
            pips = null;
        }

        private void NormalSpace()
        {
            Clear();
            // sep25: { "Flags":150994952, "Flags2":0, "Pips":[2,2,8], "FireGroup":1, "GuiFocus":0, "Fuel":{ "FuelMain":12.300001, "FuelReservoir":0.434984 }, "Cargo":105.000000, "LegalState":"Clean", "Balance":921167072 }

            flags = (1UL << (int)StatusFlags1ShipType.InMainShip) | (1UL << (int)StatusFlags1All.ShieldsUp);
            pips = new JArray() { 2, 8, 2 };
            fg = 1;
            gui = 0;
            fuel = 16.0; fuelres = 0.25;
            cargo = 20;
            legalstate = "Clean";
            bal = 29029000;
        }

        private void Supercruise()
        {
            Clear();

            // sep 25 { "Flags":16777240, "Flags2":0, "Pips":[2,2,8], "FireGroup":1, "GuiFocus":0, "Fuel":{ "FuelMain":12.300001, "FuelReservoir":0.174431 }, "Cargo":105.000000, "LegalState":"Clean", "Balance":921167072, "Destination":{ "System":5306465653474, "Body":58, "Name":"Bluford Orbital" } }

            flags = (1UL << (int)StatusFlags1ShipType.InMainShip) | (1UL << (int)StatusFlags1Ship.Supercruise) | (1UL << (int)StatusFlags1All.ShieldsUp);
            pips = new JArray() { 2, 8, 2 };
            fg = 1;
            gui = 0;
            fuel = 16.0; fuelres = 0.25;
            cargo = 20;
            legalstate = "Clean";
            bal = 29029000;
        }

        private void LatLon(double alt)
        {
            lat = 2.3; lon = 100.5; heading = 20; 
            flags |= (1UL << (int)StatusFlags1All.HasLatLong);
            if ( alt>=0)
                altitude = alt;
            if (alt>= 100000)
                flags |= (1UL << (int)StatusFlags1ReportedInOtherEvents.AltitudeFromAverageRadius);
            planetradius = 2796748.25;
        }

        private void Destination()
        {
            destination = new JObject() { ["system"] = 5306464653474, ["Name"] = "Fred Port", ["Body"] = 20 };
        }

        private void Docked()
        {
            Clear();
            // sep25 {"Flags":16842765, "Flags2":0, "Pips":[4, 1, 7], "FireGroup":1, "GuiFocus":0, "Fuel":{ "FuelMain":9.340002, "FuelReservoir":0.304527 }, "Cargo":105.000000, "LegalState":"Clean", "Balance":921167072 }
            flags = (1UL << (int)StatusFlags1Ship.Docked) |
                    (1UL << (int)StatusFlags1Ship.LandingGear) |
                            (1UL << (int)StatusFlags1Ship.FsdMassLocked) |
                            (1UL << (int)StatusFlags1All.ShieldsUp) |
                    (1UL << (int)StatusFlags1ShipType.InMainShip);
            pips = new JArray() { 2, 8, 2 };
            fg = 1;
            gui = 0;
            fuel = 16.0; fuelres = 0.25;
            cargo = 20;
            legalstate = "Clean";
            bal = 29029000;
        }

        private void ShipLanded(string bodyname)
        {
            Clear();
            flags = (1UL << (int)StatusFlags1ShipType.InMainShip) |
                        (1UL << (int)StatusFlags1Ship.Landed) |
                        (1UL << (int)StatusFlags1Ship.LandingGear) |
                        (1UL << (int)StatusFlags1Ship.FsdMassLocked) |
                        (1UL << (int)StatusFlags1All.ShieldsUp) |
                        (1UL << (int)StatusFlags1All.HasLatLong);
            pips = new JArray() { 2, 8, 2 };
            fg = 1;
            gui = 0;
            fuel = 16.0; fuelres = 0.25;
            cargo = 20;
            legalstate = "Clean";
            LatLon(0);
            this.bodyname = bodyname;
            bal = 29029000;
        }


        private void SRV(string bodyname)
        {
            Clear();
            flags = (1UL << (int)StatusFlags1All.ShieldsUp) |
                    (1UL << (int)StatusFlags1All.Lights) |
                    (1UL << (int)StatusFlags1All.HasLatLong) |
                    (1UL << (int)StatusFlags1ShipType.InSRV);
            pips = new JArray() { 2, 8, 2 };
            fg = 1;
            gui = 0;
            fuel = 0; fuelres = 0.25;
            cargo = 1;
            legalstate = "Clean";
            LatLon(0);
            this.bodyname = bodyname;
            bal = 29029000;
            planetradius = 2796748.25;
        }




        private void OnFoot(bool station, bool hangar, bool socialspace, bool planet,
                            bool breathable, bool exterior, bool cold,
                            string bodyname, double? gravity = null,string selweapon = null)
        {
            Clear();
            flags = 0;
            flags2 |= (1UL << (int)StatusFlags2ShipType.OnFoot);
            flags2 |= (1UL << (int)StatusFlags2ShipType.OnFoot);
            if (station)
                flags2 |= (1UL << (int)StatusFlags2ShipType.OnFootInStation);
            if (hangar)
                flags2 |= (1UL << (int)StatusFlags2ShipType.OnFootInHangar);
            if (planet)
                flags2 |= (1UL << (int)StatusFlags2ShipType.OnFootOnPlanet);
            if ( cold )
                flags2 |= (1UL << (int)StatusFlags2ReportedInOtherMessages.Cold);
            if (socialspace)
                flags2 |= (1UL << (int)StatusFlags2ShipType.OnFootInSocialSpace);
            if (breathable)
                flags2 |= (1UL << (int)StatusFlags2Events.BreathableAtmosphere);
            if (exterior)
                flags2 |= (1UL << (int)StatusFlags2ShipType.OnFootExterior);


            oxygen = 1.0;
            health = 0.9;
            temperature = 293;
            this.gravity = gravity;
            SelectedWeapon = selweapon;// "$humanoid_fists_name;";
            SelectedWeaponLoc = selweapon.HasChars() ? (selweapon + "_Loc") : null;
            this.bodyname = bodyname;
            bal = 29029000;
        }

        private void buttonNormalSpace_Click(object sender, EventArgs e)
        {
            //sept25 { "Flags":150994952, "Flags2":0, "Pips":[2,2,8], "FireGroup":1, "GuiFocus":0, "Fuel":{ "FuelMain":12.300001, "FuelReservoir":0.434984 }, "Cargo":105.000000, "LegalState":"Clean", "Balance":921167072 }
            //synth { "Flags":150994952, "Flags2":0, "Pips":[2,2,8], "FireGroup":1, "GuiFocus":0, "Fuel":{ "FuelMain":12.300001, "FuelReservoir":0.434984 }, "Cargo":105.000000, "LegalState":"Clean", "Balance":921167072 }

            NormalSpace();
            WriteStatus();
        }

        private void buttonNormalSpaceCompass_Click(object sender, EventArgs e)
        {
            //sept25 { "Flags":18874376, "Flags2":0, "Pips":[4,8,0], "FireGroup":0, "GuiFocus":0, "Fuel":{ "FuelMain":15.020000, "FuelReservoir":0.368908 }, "Cargo":4.000000, "LegalState":"Clean", "Latitude":11.867844, "Longitude":122.273544, "Heading":222, "Altitude":3525, "BodyName":"LHS 3447 B 3 a", "PlanetRadius":605678.875000, "Balance":921177172, "Destination":{ "System":5306465653474, "Body":41, "Name":"Polubotok's Zenith" } }
            // synth { "Flags":18874376,"Flags2":0,"Pips":[2, 8, 2],"FireGroup":1,"GuiFocus":0,"Fuel":{ "FuelMain":16,"FuelReservoir":0.32},"Cargo":20,"LegalState":"Clean","Latitude":2.3,"Longitude":100.5,"Heading":20,"Altitude":1000,"PlanetRadius":2796748.25,"Balance":29029000}
            NormalSpace();
            LatLon(1000);
            WriteStatus();
        }

        private void buttonSupercruise_Click(object sender, EventArgs e)
        {
            //sept25 { "Flags":16777240, "Flags2":0, "Pips":[2,2,8], "FireGroup":1, "GuiFocus":0, "Fuel":{ "FuelMain":12.300001, "FuelReservoir":0.174431 }, "Cargo":105.000000, "LegalState":"Clean", "Balance":921167072, "Destination":{ "System":5306465653474, "Body":58, "Name":"Bluford Orbital" } }
            // synth {"Flags":16777240,"Flags2":0,"Pips":[2,8,2],"FireGroup":1,"GuiFocus":0,"Fuel":{"FuelMain":16,"FuelReservoir":0.32},"Cargo":20,"LegalState":"Clean","Balance":29029000,"Destination":{"System":2928282,"Body":20,"Name":"Fred Port"}}
            Supercruise();
            Destination();
            WriteStatus();
        }

        private void buttonSupercruiseCompass_Click(object sender, EventArgs e)
        {
            // sept25 : { "Flags":555745304, "Flags2":0, "Pips":[4,8,0], "FireGroup":0, "GuiFocus":0, "Fuel":{ "FuelMain":15.510000, "FuelReservoir":0.483695 }, "Cargo":4.000000, "LegalState":"Clean", "Latitude":-65.938652, "Longitude":-167.029770, "Heading":132, "Altitude":240090, "BodyName":"LHS 3447 B 3 a", "PlanetRadius":605678.875000, "Balance":921177172, "Destination":{ "System":5306465653474, "Body":41, "Name":"Alvarado Industrial Assembly" } }
            // synth { "Flags":555745304,"Flags2":0,"Pips":[2, 8, 2],"FireGroup":1,"GuiFocus":0,"Fuel":{ "FuelMain":16,"FuelReservoir":0.32},"Cargo":20,"LegalState":"Clean","Latitude":2.3,"Longitude":100.5,"Heading":20,"Altitude":100000,"PlanetRadius":2796748.25,"Balance":29029000}
            Supercruise();
            LatLon(100000);
            WriteStatus();
        }

        private void buttonDockedStarport_Click(object sender, EventArgs e)
        {
            // sept25 { "Flags":16842765, "Flags2":0, "Pips":[4,1,7], "FireGroup":1, "GuiFocus":0, "Fuel":{ "FuelMain":9.340002, "FuelReservoir":0.304527 }, "Cargo":105.000000, "LegalState":"Clean", "Balance":921167072 }
            // synth "Flags":16842765,"Flags2":0,"Pips":[2,8,2],"FireGroup":1,"GuiFocus":0,"Fuel":{"FuelMain":16,"FuelReservoir":0.32},"Cargo":20,"LegalState":"Clean","Balance":29029000}
            Docked();
            WriteStatus();
        }

        private void buttonOnFootStarportHangar_Click(object sender, EventArgs e)
        {
            // sept25 { "Flags":5, Flags2":90121, "Oxygen":1.000000, "Health":1.000000, "Temperature":293.000000, "SelectedWeapon":"", "LegalState":"Clean", "BodyName":"Bluford Orbital", "Balance":921167072 }
            // synth { "Flags":5,"Flags2":90121,"Oxygen":1,"Health":0.9,"Temperature":293,"SelectedWeapon":"","BodyName":"Bluford Orbital","Balance":29029000}

            OnFoot(true, true, true, false, true,  false, false, "Bluford Orbital", null, "");
            flags |= (1UL << (int)StatusFlags1Ship.Docked) | (1UL << (int)StatusFlags1Ship.LandingGear);
            WriteStatus();
        }

        private void buttonOnFootStarportSocialSpace_Click(object sender, EventArgs e)
        {
            // sept25 {  "Flags":5, "Flags2":81929, "Oxygen":1.000000, "Health":1.000000, "Temperature":293.000000, "SelectedWeapon":"", "LegalState":"Clean", "BodyName":"Bluford Orbital", "Balance":921167072 }
            // synth { "Flags":5,"Flags2":81929,"Oxygen":1,"Health":0.9,"Temperature":293,"SelectedWeapon":"","BodyName":"Bluford Orbital","Balance":29029000}
            OnFoot(true, false, true,  true,true, false, false, "Bluford Orbital", null, "");
            flags |= (1UL << (int)StatusFlags1Ship.Docked) | (1UL << (int)StatusFlags1Ship.LandingGear);
            flags2 = 81929;
            WriteStatus();

        }

        private void buttonNormalSpaceLanded_Click(object sender, EventArgs e)
        {
            // sept25 { "Flags":18939918, "Flags2":0, "Pips":[4,8,0], "FireGroup":0, "GuiFocus":0, "Fuel":{ "FuelMain":15.020000, "FuelReservoir":0.311128 }, "Cargo":4.000000, "LegalState":"Clean", "Latitude":11.698049, "Longitude":122.323784, "Heading":19, "Altitude":0, "BodyName":"LHS 3447 B 3 a", "PlanetRadius":605678.875000, "Balance":921177172, "Destination":{ "System":5306465653474, "Body":41, "Name":"Polubotok's Zenith" } }
            // synth { "Flags":18939918,"Flags2":0,"Pips":[2,8,2],"FireGroup":1,"GuiFocus":0,"Fuel":{"FuelMain":16,"FuelReservoir":0.32},"Cargo":20,"LegalState":"Clean","Latitude":2.3,"Longitude":100.5,"Heading":20,"Altitude":0,"BodyName":"Nervi 2g","PlanetRadius":2796748.25,"Balance":29029000}

            ShipLanded("Nervi 2g");
            WriteStatus();
        }

        private void buttonOnFootPlanet_Click(object sender, EventArgs e)
        {
            // sept25 { "Flags":2097158,"Flags2":33041, "Oxygen":1.000000, "Health":1.000000, "Temperature":81.453110, "SelectedWeapon":"$humanoid_fists_name;", "SelectedWeapon_Localised":"Unarmed", "Gravity":0.035564,
            //                          "LegalState":"Clean", "Latitude":11.698016, "Longitude":122.323990, "Heading":19, "BodyName":"LHS 3447 B 3 a", "PlanetRadius":605678.875000, "Balance":921177172 }
            // synth { Flags":2097158,"Flags2":33041,"Oxygen":1,"Health":0.9,"Temperature":293,"SelectedWeapon":"$humanoid_fists_name;","SelectedWeapon_Localised":"$humanoid_fists_name;_Loc","Gravity":0.035564,"Latitude":2.3,"Longitude":100.5,"Heading":20,"BodyName":"Nervi 2f","PlanetRadius":2796748.25,"Balance":29029000}
            OnFoot(false, false, false, true, false, true, true, "Nervi 2f", 0.035564, "$humanoid_fists_name;");
            LatLon(-1);
            flags |= (1UL << (int)StatusFlags1Ship.Landed) | (1UL << (int)StatusFlags1Ship.LandingGear);
            System.Diagnostics.Debug.Assert(flags == 2097158 && flags2 == 33041);
            WriteStatus();
        }

        private void buttonOnFootPlanetNoShip_Click(object sender, EventArgs e)
        {
            // sept25 { "Flags":2097152, "Flags2":33041, "Oxygen":1.000000, "Health":1.000000, "Temperature":86.102501, "SelectedWeapon":"$humanoid_fists_name;", "SelectedWeapon_Localised":"Unarmed", "Gravity":0.035564,
            //      "LegalState":"Clean", "Latitude":11.697256, "Longitude":122.324417, "Heading":157, "BodyName":"LHS 3447 B 3 a", "PlanetRadius":605678.875000, "Balance":921177172 }
            // synth  {"Flags":2097152,"Flags2":33041,"Oxygen":1,"Health":0.9,"Temperature":293,"SelectedWeapon":"$humanoid_fists_name;","SelectedWeapon_Localised":"$humanoid_fists_name;_Loc","Gravity":0.035564,"Latitude":2.3,"Longitude":100.5,"Heading":20,"BodyName":"Nervi 2f","PlanetRadius":2796748.25,"Balance":29029000}
            OnFoot(false, false, false, true, false, true, true,"Nervi 2f", 0.035564, "$humanoid_fists_name;");
            LatLon(-1);
            System.Diagnostics.Debug.Assert(flags == 2097152 && flags2 == 33041);
            WriteStatus();
        }


        private void buttonSRVShipLanded_Click(object sender, EventArgs e)
        {
            // sept25 { "Flags":69206286, "Flags2":0, "Pips":[4,4,4], "FireGroup":0, "GuiFocus":0, "Fuel":{ "FuelMain":0.000000, "FuelReservoir":0.498857 }, "Cargo":0.000000, "LegalState":"Clean", "Latitude":11.680736, "Longitude":122.328880, "Heading":206, "Altitude":0, "BodyName":"LHS 3447 B 3 a", "PlanetRadius":605678.875000, "Balance":921177172 }
            // synth { "Flags":69206286,"Flags2":0,"Pips":[2,8,2],"FireGroup":1,"GuiFocus":0,"Fuel":{"FuelMain":0,"FuelReservoir":0.32},"Cargo":1,"LegalState":"Clean","Latitude":2.3,"Longitude":100.5,"Heading":20,"Altitude":0,"BodyName":"Nervi 2f","PlanetRadius":2796748.25,"Balance":29029000}
            SRV("Nervi 2f");
            flags |= (1UL << (int)StatusFlags1Ship.Landed) | (1UL << (int)StatusFlags1Ship.LandingGear);
            WriteStatus();
        }

        private void buttonFighter_Click(object sender, EventArgs e)
        {
            //TBD
            Clear();
            flags = (1UL << (int)StatusFlags1ShipType.InFighter) | (1UL << (int)StatusFlags1All.ShieldsUp);
            pips = new JArray() { 2, 8, 2 };
            WriteStatus();
        }

        private void buttonDockedInstallation_Click(object sender, EventArgs e)
        {
            //sept25 {"Flags":18939917, "Flags2":0, "Pips":[4,8,0], "FireGroup":0, "GuiFocus":0, "Fuel":{ "FuelMain":16.000000, "FuelReservoir":0.490000 }, "Cargo":4.000000, "LegalState":"Clean", "Latitude":11.701438, "Longitude":122.224075, "Heading":0, "Altitude":0, "BodyName":"LHS 3447 B 3 a", "PlanetRadius":605678.875000, "Balance":921176216 }
            // synth {"Flags":18939917,"Flags2":0,"Pips":[2,8,2],"FireGroup":1,"GuiFocus":0,"Fuel":{"FuelMain":16,"FuelReservoir":0.32},"Cargo":20,"LegalState":"Clean","Latitude":2.3,"Longitude":100.5,"Heading":20,"Altitude":0,"BodyName":"Nervi 2g","PlanetRadius":2796748.25,"Balance":29029000}
            Docked();
            LatLon(0);
            bodyname = "Nervi 2g";
            WriteStatus();
        }

        private void buttonOnFootInstallation_Click(object sender, EventArgs e)
        {
            //sep25 { "Flags":2097157, "Flags2":33041, "Oxygen":1.000000, "Health":1.000000, "Temperature":81.163666, "SelectedWeapon":"$humanoid_fists_name;", "SelectedWeapon_Localised":"Unarmed", "Gravity":0.035564, "LegalState":"Clean", "Latitude":11.701600, "Longitude":122.224075, "Heading":81, "BodyName":"LHS 3447 B 3 a", "PlanetRadius":605678.875000, "Balance":921176216 }
            // synth {"Flags":2097152,"Flags2":33041,"Oxygen":1,"Health":0.9,"Temperature":293,"SelectedWeapon":"$humanoid_fists_name;","SelectedWeapon_Localised":"$humanoid_fists_name;_Loc","Gravity":0.34,"Latitude":2.3,"Longitude":100.5,"Heading":20,"BodyName":"Nervi 2g","PlanetRadius":2796748.25,"Balance":29029000}
            OnFoot(false, false, false, true, false, true, true, "Nervi 2g", 0.34, "$humanoid_fists_name;");
            LatLon(-1);
            flags |= (1UL << (int)StatusFlags1Ship.Docked) | (1UL << (int)StatusFlags1Ship.LandingGear);
            System.Diagnostics.Debug.Assert(flags == 2097157 && flags2 == 33041);
            WriteStatus();
        }

        private void buttonOnFootInstallationInside_Click(object sender, EventArgs e)
        {
            // sept25 { "Flags":2097157, "Flags2":65553, "Oxygen":1.000000, "Health":1.000000, "Temperature":293.000000, "SelectedWeapon":"$humanoid_fists_name;", "SelectedWeapon_Localised":"Unarmed", "Gravity":0.035564, "LegalState":"Clean", "Latitude":11.711300, "Longitude":122.227219, "Heading":-76, "BodyName":"LHS 3447 B 3 a", "PlanetRadius":605678.875000, "Balance":921176216 }
            // synth {"timestamp":"2025-09-08T13:24:55.413Z","event":"Status","Flags":2097157,"Flags2":65553,"Oxygen":1,"Health":0.9,"Temperature":293,"SelectedWeapon":"$humanoid_fists_name;","SelectedWeapon_Localised":"$humanoid_fists_name;_Loc","Gravity":0.34,"Latitude":2.3,"Longitude":100.5,"Heading":20,"BodyName":"Nervi 2g","PlanetRadius":2796748.25,"Balance":29029000}
            OnFoot(false, false, false, true, true, false, false, "Nervi 2g", 0.34, "$humanoid_fists_name;");
            LatLon(-1);
            flags |= (1UL << (int)StatusFlags1Ship.Docked) | (1UL << (int)StatusFlags1Ship.LandingGear);
            System.Diagnostics.Debug.Assert(flags == 2097157 && flags2 == 65553);
            WriteStatus();
        }


        private void buttonOnfootPlanetHangar_Click(object sender, EventArgs e)
        {
            // onfootplanetaryporthangar { "Flags":2097157, "Flags2":90129, "Oxygen":1.000000, "Health":1.000000, "Temperature":293.000000, "SelectedWeapon":"", "Gravity":0.107517, "LegalState":"Clean", "Latitude":-5.637279, "Longitude":40.435890, "Heading":-45, "BodyName":"LHS 3447 B 2 a", "PlanetRadius":1821289.875000, "Balance":921176177 }
            // synth {"Flags":2097157,"Flags2":90129,"Oxygen":1,"Health":0.9,"Temperature":293,"SelectedWeapon":"$humanoid_fists_name;","SelectedWeapon_Localised":"$humanoid_fists_name;_Loc","Gravity":0.34,"Latitude":2.3,"Longitude":100.5,"Heading":20,"BodyName":"Nervi 2g","PlanetRadius":2796748.25,"Balance":29029000}  
            OnFoot(false, true, true, true, true, false, false, "Nervi 2g", 0.34, "$humanoid_fists_name;");
            LatLon(-1);
            flags |= (1UL << (int)StatusFlags1Ship.Docked) | (1UL << (int)StatusFlags1Ship.LandingGear);
            System.Diagnostics.Debug.Assert(flags == 2097157 && flags2 == 90129);
            WriteStatus();
        }



        private void buttonOnFootPlanetSocialSpace_Click(object sender, EventArgs e)
        {
            // { "Flags":2097157, "Flags2":81937, "Oxygen":1.000000, "Health":1.000000, "Temperature":293.000000, "SelectedWeapon":"", "Gravity":0.107517, "LegalState":"Clean", "Latitude":-5.647436, "Longitude":40.424202, "Heading":139, "BodyName":"LHS 3447 B 2 a", "PlanetRadius":1821289.875000, "Balance":921176177 }
            OnFoot(false, false, true, true, true, false, false, "Nervi 2g", 0.34, "$humanoid_fists_name;");
            LatLon(-1);
            flags |= (1UL << (int)StatusFlags1Ship.Docked) | (1UL << (int)StatusFlags1Ship.LandingGear);
            System.Diagnostics.Debug.Assert(flags == 2097157 && flags2 == 81937);
            WriteStatus();
        }

        private void buttonLatLonOn_Click(object sender, EventArgs e)
        {
            LatLon(600);
            WriteStatus();
        }

        private void buttonLatLonOff_Click(object sender, EventArgs e)
        {
            lat = lon = heading = altitude = planetradius = null;
            flags &= ~(1UL << (int)StatusFlags1All.HasLatLong);
            WriteStatus();
        }

        private void buttonSetDest_Click(object sender, EventArgs e)
        {
            Destination();
            WriteStatus();
        }

        private void buttonGUIRight_Click(object sender, EventArgs e)
        {
            gui = gui == null ? 0 : ((gui + 1) % 12);
            WriteStatus();
        }

        private void buttonGUILeft_Click(object sender, EventArgs e)
        {
            gui = gui == null ? 0 : ((gui + 12 -1) % 12);
            WriteStatus();
        }

        //Belt Cluster
        //Body
        // Carrier
        //Orbital Station
        //Resource
        //Settlement
        //Star

        static string[] destlist = new string[]
        {
            "Belt Cluster, 5306465653474, 24, LHS 3447 B A Belt Cluster 5",
            "Body, 5306465653474, 26, LHS 3447 B 1 a",
            "Carrier, 5306465653474, 30, ALEPH BOREAL Q8M-T7K",
            "Starport, 5306465653474, 58, Bluford Orbital",
            "Resource Site, 5306465653474, 32, $MULTIPLAYER_SCENARIO77_TITLE; ,Resource Extraction Site [Low]",
            "Settlement, 5306465653474, 33, Yanez's Hold",
            "Eravate, 5856221467362, 0, Eravate",
        };

        private void comboBoxSelDest_SelectedIndexChanged(object sender, EventArgs e)
        {
            StringParser sp = new StringParser(destlist[comboBoxSelDest.SelectedIndex]);
            destination = new JObject();
            sp.NextWordComma(terminators:"");
            destination["System"] = sp.NextLongComma(" ,");
            destination["Body"] = sp.NextIntComma(" ,");
            destination["Name"] = sp.NextWord(",");
            if (sp.IsCharMoveOn(','))
                destination["Name_Localised"] = sp.NextWord(",");
            WriteStatus();

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

        #region Flags

        private enum StatusFlags1Ship                             // Flags -> Events
        {
            Docked = 0, // (on a landing pad)
            Landed = 1, // (on planet surface)
            LandingGear = 2,
            Supercruise = 4,
            FlightAssist = 5,
            HardpointsDeployed = 6,
            InWing = 7,
            CargoScoopDeployed = 9,
            SilentRunning = 10,
            ScoopingFuel = 11,
            FsdMassLocked = 16,
            FsdCharging = 17,
            FsdCooldown = 18,
            OverHeating = 20,
            BeingInterdicted = 23,
            HUDInAnalysisMode = 27,     // 3.3
            FsdJump = 30,
        }

        public enum StatusFlags1SRV                              // Flags
        {
            SrvHandbrake = 12,
            SrvTurret = 13,
            SrvUnderShip = 14,
            SrvDriveAssist = 15,
            SrvHighBeam = 31,
        }

        public enum StatusFlags1All                             // Flags
        {
            ShieldsUp = 3,
            Lights = 8,
            LowFuel = 19,
            HasLatLong = 21,
            IsInDanger = 22,
            NightVision = 28,             // 3.3
        }
        private enum StatusFlags1ReportedInOtherEvents       // reported via other mechs than flags 
        {
            AltitudeFromAverageRadius = 29, // 3.4, via position
        }

        public enum StatusFlags1ShipType                        // Flags
        {
            InMainShip = 24,
            InFighter = 25,
            InSRV = 26,
            ShipMask = (1 << InMainShip) | (1 << InFighter) | (1 << InSRV),
        }

        public enum StatusFlags2ShipType                   // used to compute ship type
        {
            OnFoot = 0,
            InTaxi = 1,
            InMulticrew = 2,
            OnFootInStation = 3,
            OnFootOnPlanet = 4,
            OnFootInHangar = 13,
            OnFootInSocialSpace = 14,
            OnFootExterior = 15,
        }

        public enum StatusFlags2Events                  // these are bool flags, reported sep.
        {
            AimDownSight = 5,
            GlideMode = 12,
            BreathableAtmosphere = 16,
            SupercruiseOverdrive = 20,
            SupercruiseAssist = 21,
            NPCCrewActive = 22,
        }

        public enum StatusFlags2ReportedInOtherMessages     // these are states reported as part of other messages
        {
            LowOxygen = 6,
            LowHealth = 7,
            Cold = 8,
            Hot = 9,
            VeryCold = 10,
            VeryHot = 11,
            TempBits = (1 << Cold) | (1 << Hot) | (1 << VeryCold) | (1 << VeryHot),
            FSDHyperdriveCharging = 19,         // U14 nov 22
        }


        #endregion

        ulong flags, flags2;
        double? cargo, fuel, fuelres;
        double? lat, lon;
        double? heading, altitude;
        double? planetradius;
        double? temperature;
        double? gravity;
        double? health, oxygen;
        string SelectedWeapon;
        string SelectedWeaponLoc;

        string bodyname;
        string legalstate;
        JObject destination;
        double? bal;
        int? gui;
        int ?fg;
        JArray pips;


    }

    public static class extensions
    {
        public static QuickJSON.JSONFormatter UTC(this QuickJSON.JSONFormatter fmt, string name)
        {
            DateTime utc = DateTime.UtcNow;
            fmt.V(name, utc.ToStringZulu());
            return fmt;
        }
    }


}
