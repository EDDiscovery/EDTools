using NetMQ;
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

namespace EDDNRecorder
{
    public partial class EDDNRecorder : Form
    {

        private string appdatapath;

        public EDDNRecorder()
        {
            InitializeComponent();
            appdatapath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EDDNRecorder");

            if (!Directory.Exists(appdatapath))
                Directory.CreateDirectory(appdatapath);
        }

        Thread opthread = null;

        private void button1_Click(object sender, EventArgs e)
        {
            opthread = new Thread(GetEDDNData);
            opthread.Start();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            CloseThread = true;
            opthread.Join();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_VSCROLL = 277;
        private const int SB_PAGEBOTTOM = 7;

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
                        subscriber.Connect("tcp://eddn.edcd.io:9500");
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
                                BeginInvoke((MethodInvoker)delegate 
                                    {
                                        richTextBox1.AppendText(myStr.Substring(0, 400) + Environment.NewLine);
                                        SendMessage(richTextBox1.Handle, WM_VSCROLL, (IntPtr)SB_PAGEBOTTOM, IntPtr.Zero);
                                      

                                    });
                                //richTextBox1.Select(richTextBox1.Text.Length, richTextBox1.Text.Length);
                                decompressedFileStream.Position = 0;
                                //var serializer = new DataContractJsonSerializer(typeof(EddnRequest));
                                //var rootObject = (EddnRequest)serializer.ReadObject(decompressedFileStream);
                                //var message = rootObject.Message;
                                /*                    Log.Debug(rootObject.SchemaRef);
                                                    Log.DebugFormat(
                                                        "Station: {0}, Item: {1}, BuyPrice: {2}",
                                                        message.StationName,
                                                        message.ItemName,
                                                        message.BuyPrice);
                                                        */
                                decompressedFileStream.Close();
                            }
                        }
                    }
                }
            }
        }
    }
}
