﻿using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Runtime.InteropServices;
using BaseUtils.JSON;

namespace EDDNRecorder
{
    public partial class EDDNRecorder : Form
    {
        private string appdatapath;
        private bool beta = false;

        public EDDNRecorder()
        {
            InitializeComponent();
            appdatapath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EDDNRecorder");

            checkBoxFollow.Checked = true;
            checkBoxWrapBody.Checked = false;

            if (!Directory.Exists(appdatapath))
                Directory.CreateDirectory(appdatapath);
        }

        Thread opthread = null;

        private void buttonLive_Click(object sender, EventArgs e)
        {
            opthread = new Thread(GetEDDNData);
            opthread.Start();
            buttonLive.Enabled = buttonBeta.Enabled = false;
        }

        private void buttonBeta_Click(object sender, EventArgs e)
        {
            beta = true;
            opthread = new Thread(GetEDDNData);
            opthread.Start();
            buttonLive.Enabled = buttonBeta.Enabled = false;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            CloseThread = true;
            if ( opthread != null )
                opthread.Join();
        }

        private bool CloseThread = false;

        private void GetEDDNData()
        {
            System.Diagnostics.Debug.WriteLine("Connecting..");
            using (StreamWriter file = new StreamWriter(Path.Combine(appdatapath, "EDDN" + DateTime.Now.ToString("yyyy-dd-MM-HH-mm-ss") + ".log")))
            {
                using (NetMQContext ctx = NetMQContext.Create())
                {
                    using (var subscriber = ctx.CreateSubscriberSocket())
                    {
                        string endpoint = beta ? "tcp://beta.eddn.edcd.io:9510" : "tcp://eddn.edcd.io:9500";
                        BeginInvoke((MethodInvoker)delegate
                        {
                            dgv.Rows.Add(new object[] { "", "","","","", endpoint });
                        });

                        file.WriteLine($"Listening to {endpoint}");

                        subscriber.Connect(endpoint);
                        subscriber.Subscribe(Encoding.Unicode.GetBytes(string.Empty));

                        System.Diagnostics.Debug.WriteLine("Connected");

                        while (!CloseThread)
                        {
                            byte[] response;
                            try
                            {
                                response = subscriber.ReceiveFrameBytes();
                            }
                            catch (NetMQException)
                            {
                                return;
                            }

                            System.Diagnostics.Debug.WriteLine("Received");

                            var decompressedFileStream = new MemoryStream();
                            using (decompressedFileStream)
                            {
                                var stream = new MemoryStream(response);

                                // Don't forget to ignore the first two bytes of the stream (!)
                                stream.ReadByte();
                                stream.ReadByte();
                                using (var decompressionStream = new DeflateStream(stream, CompressionMode.Decompress))
                                {
                                    decompressionStream.CopyTo(decompressedFileStream);
                                }

                                decompressedFileStream.Position = 0;
                                var sr = new StreamReader(decompressedFileStream);
                                var myStr = sr.ReadToEnd();

                                file.WriteLine(myStr);

                                JToken tk = JToken.Parse(myStr, JToken.ParseOptions.CheckEOL);
                                if( tk != null )
                                {
                                    string schema = tk["$schemaRef"].Str().ReplaceIfStartsWith("https://eddn.edcd.io/schemas/");
                                    JObject header = tk["header"].Object();
                                    JObject message = tk["message"].Object();

                                    object[] rowt = { header["gatewayTimestamp"].Str(), schema, header["softwareName"].Str(), header["softwareVersion"].Str()
                                                            , header["uploaderID"].Str(), message.ToString() };

                                    BeginInvoke((MethodInvoker)delegate
                                    {
                                        dgv.Rows.Add(rowt);
                                        var row = dgv.Rows[dgv.Rows.Count - 1];
                                        if (checkBoxFollow.Checked )
                                            dgv.CurrentCell = row.Cells[0];
                                        if (checkBoxWrapBody.Checked)
                                        {
                                            row.Cells[5].Style.WrapMode = DataGridViewTriState.True;
                                            dgv.AutoResizeRow(row.Index, DataGridViewAutoSizeRowMode.AllCells);
                                        }
                                    });
                                }

                                decompressedFileStream.Position = 0;
                                decompressedFileStream.Close();
                            }
                        }
                    }
                }
            }
        }

        private void checkBoxWrapBody_CheckedChanged(object sender, EventArgs e)
        {
            //Column6.DefaultCellStyle.WrapMode = checkBoxWrapBody.Checked ? DataGridViewTriState.True : DataGridViewTriState.False;
        }

    }
}
