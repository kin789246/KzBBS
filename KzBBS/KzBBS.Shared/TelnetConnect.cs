using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace KzBBS
{
    class TelnetConnect
    {
        public static TelnetConnect connection = new TelnetConnect();
        public string address { get; set; }
        public string port { get; set; }
        public string account { get; set; }
        public string password { get; set; }
        public bool autoLogin = false;

        static Windows.ApplicationModel.Resources.ResourceLoader loader =
            new Windows.ApplicationModel.Resources.ResourceLoader();

        internal void OnDisconnect()
        {
            if (TelnetSocket.PTTSocket.Connecting)
            {
                TelnetSocket.PTTSocket.cts.Cancel();
            }
            else
            {
                TelnetSocket.PTTSocket.Disconnect();
            }
            connection.autoLogin = false;
        }

        public async Task OnConnect(string tIP, string tPort)
        {
            await Big5Util.generateTable();
            if (!TelnetSocket.PTTSocket.IsConnected)
            {
                try
                {
                    await TelnetSocket.PTTSocket.Connect(tIP, tPort);
                    //Task.Factory.StartNew(ClientWaitForMessage);
                    ClientWaitForMessage();
                }
                catch
                {
                    throw;
                }
            }
            else
            {
                //ShowMessage("已經連線了好嗎!");
               TelnetSocket.ShowMessage(loader.GetString("alreadyconnect"));
            }
        }

        static byte[] rawdata = new byte[0];
        static List<byte> finalRawData = new List<byte>();
        private async void ClientWaitForMessage()
        {
            finalRawData.Clear();
            int MAXBuffer = 6000;
            DataReader reader = new DataReader(TelnetSocket.PTTSocket.ClientSocket.InputStream);
            reader.InputStreamOptions = InputStreamOptions.Partial;
            while (true)
            {
                try
                {
                    uint Message = await reader.LoadAsync((uint)MAXBuffer);
                    rawdata = new byte[Message];
                    reader.ReadBytes(rawdata);
                    if (rawdata.Length != 0)
                    {
                        TelnetANSIParser.HandleAnsiESC(rawdata);
                        //save the last char received
                        string lastChar0 = Big5Util.ToUni(new byte[] { rawdata[rawdata.Length - 1] });
                        //Debug.WriteLine(lastChar0);
                        if(string.IsNullOrEmpty(lastChar0))
                        {
                            PTTDisplay.lastChar = ' ';
                        }
                        else
                        {
                            PTTDisplay.lastChar = lastChar0[0];
                        }
                        
                        PTTDisplay.pttDisplay.LoadFromSource(TelnetANSIParser.BBSPage);
                    }
                }
                catch (Exception exception)
                {
                    if (TelnetSocket.PTTSocket.IsConnected)
                    {
                        TelnetSocket.PTTSocket.Disconnect();
                    }
                    Debug.WriteLine("Read stream failed with error: " + exception.Message);
                    return;
                    //throw;
                }
            }
        }

        public event EventHandler LinesPropChanged;
        public virtual void onLinesChanged(EventArgs e)
        {
            if (LinesPropChanged != null)
            {
                LinesPropChanged(this, e);
            }
        }

        public static async Task sendCommand(byte[] interpretByte)
        {
            if (!TelnetSocket.PTTSocket.IsConnected) return;
            DataWriter writer = new DataWriter(TelnetSocket.PTTSocket.ClientSocket.OutputStream);
            writer.WriteBytes(interpretByte);

            try
            {
                await writer.StoreAsync();
                writer.DetachStream();
            }
            catch (Exception exception)
            {
                TelnetSocket.PTTSocket.Disconnect();
                Debug.WriteLine(exception.Message);
            }
        }

        public static async Task sendCommand(string sourceString)
        {
            if (!TelnetSocket.PTTSocket.IsConnected) return;
            byte[] interpretByte = interpreteToByte(sourceString);
            DataWriter writer = new DataWriter(TelnetSocket.PTTSocket.ClientSocket.OutputStream);
            writer.WriteBytes(interpretByte);

            try
            {
                await writer.StoreAsync();
                writer.DetachStream();
            }
            catch (Exception exception)
            {
                TelnetSocket.PTTSocket.Disconnect();
                Debug.WriteLine(exception.Message);
            }
        }

        public static byte[] interpreteToByte(string sourceString)
        {
            List<byte> interpretByte = new List<byte>();
            if (string.IsNullOrEmpty(sourceString))
            {
                interpretByte.Add(0);
                return interpretByte.ToArray();
            }

            foreach (var x in sourceString)
            {
                interpretByte.Add((byte)x);
            }
            return interpretByte.ToArray();
        }
    }
}
