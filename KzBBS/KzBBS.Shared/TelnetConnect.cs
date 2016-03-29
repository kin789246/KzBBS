using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Popups;

namespace KzBBS
{
    public class TelnetConnect
    {
        private StreamSocket clientSocket;
        private HostName serverHost;
        private string serverPort;
        private bool connected = false;
        private bool connecting = false;

        public CancellationTokenSource cts { get; set; }
        public StreamSocket ClientSocket { get { return clientSocket; } }
        public bool IsConnected { get { return connected; } }
        public bool Connecting { get { return connecting; } }
        public string address { get; set; }
        public string port { get; set; }
        public string account { get; set; }
        public string password { get; set; }
        public bool autoLogin = false;

        static Windows.ApplicationModel.Resources.ResourceLoader loader =
            new Windows.ApplicationModel.Resources.ResourceLoader();
        public async Task Connect(string host, string port)
        {
            serverHost = new HostName(host);
            serverPort = port;
            clientSocket = new StreamSocket();
            cts = new CancellationTokenSource();
            try
            {
                connecting = true;
                await clientSocket.ConnectAsync(serverHost, serverPort).AsTask(cts.Token);
                connecting = false;
                connected = true;
            }
            catch
            {
                //ShowMessage(exception.Message);
                Disconnect();
                throw;
            }
        }

        public void Disconnect()
        {
            if (connected || connecting)
            {
                connected = false;
                connecting = false;
            }
            onSocketDisconnect(new EventArgs());
            clientSocket.Dispose();
        }
        
        public async static void ShowMessage(string msg)
        {
            var messageDialog = new MessageDialog(msg);
            //messageDialog.Title = "訊息通知";
            messageDialog.Title = loader.GetString("infoNotify");
            await messageDialog.ShowAsync();
        }

        public static async Task<bool> confirmDialog(string msg)
        {
            bool isYes = false;
            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(msg);
            //messageDialog.Title = "作決定吧";
            messageDialog.Title = loader.GetString("todecide");
            // Add commands and set their callbacks;
            //messageDialog.Commands.Add(new UICommand("好",
            //    new UICommandInvokedHandler((cmd) => isYes = true)));
            //messageDialog.Commands.Add(new UICommand("不好",
            //    new UICommandInvokedHandler((cmd) => isYes = false)));
            messageDialog.Commands.Add(new UICommand(loader.GetString("sayok"),
                new UICommandInvokedHandler((cmd) => isYes = true)));
            messageDialog.Commands.Add(new UICommand(loader.GetString("sayno"),
                new UICommandInvokedHandler((cmd) => isYes = false)));
            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;
            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 1;
            // Show the message dialog
            await messageDialog.ShowAsync();
            return isYes;
        }

        public event EventHandler SocketDisconnect;
        protected virtual void onSocketDisconnect(EventArgs e)
        {
            if (SocketDisconnect != null)
            {
                SocketDisconnect(this, e);
            }
        }
        internal void OnDisconnect()
        {
            if (Connecting)
            {
                cts.Cancel();
            }
            else
            {
                Disconnect();
            }
            autoLogin = false;
        }

        public async Task OnConnect(string tIP, string tPort)
        {
            await Big5Util.generateTable();
            if (!IsConnected)
            {
                try
                {
                    await Connect(tIP, tPort);
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
                TelnetConnect.ShowMessage(loader.GetString("alreadyconnect"));
            }
        }

        static byte[] rawdata = new byte[0];
        static List<byte> finalRawData = new List<byte>();
        private async void ClientWaitForMessage()
        {
            finalRawData.Clear();
            int MAXBuffer = 6000;
            DataReader reader = new DataReader(ClientSocket.InputStream);
            reader.InputStreamOptions = InputStreamOptions.Partial;
            while (true)
            {
                try
                {
                    uint Message = await reader.LoadAsync((uint)MAXBuffer);
                    if (Message == 0)
                    {
                        //if disconnected
                        //debug
                        Debug.WriteLine("Exit the while loop");
                        return;
                    }
                    rawdata = new byte[Message];
                    reader.ReadBytes(rawdata);
                    TelnetANSIParser.HandleAnsiESC(rawdata);
                    //save the last char received
                    string lastChar0 = Big5Util.ToUni(new byte[] { rawdata[rawdata.Length - 1] });
                    //Debug.WriteLine(lastChar0);
                    if (string.IsNullOrEmpty(lastChar0))
                    {
                        PTTDisplay.lastChar = ' ';
                    }
                    else
                    {
                        PTTDisplay.lastChar = lastChar0[0];
                    }

                    PTTDisplay.pttDisplay.LoadFromSource(TelnetANSIParser.BBSPage);
                }
                catch (Exception exception)
                {
                    if (IsConnected)
                    {
                        Disconnect();
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

        public async Task sendCommand(byte[] interpretByte)
        {
            if (!IsConnected) return;
            DataWriter writer = new DataWriter(ClientSocket.OutputStream);
            writer.WriteBytes(interpretByte);

            try
            {
                await writer.StoreAsync();
                writer.DetachStream();
            }
            catch (Exception exception)
            {
                Disconnect();
                Debug.WriteLine(exception.Message);
            }
        }

        public async Task sendCommand(string sourceString)
        {
            if (!IsConnected) return;
            byte[] interpretByte = interpreteToByte(sourceString);
            DataWriter writer = new DataWriter(ClientSocket.OutputStream);
            writer.WriteBytes(interpretByte);

            try
            {
                await writer.StoreAsync();
                writer.DetachStream();
            }
            catch (Exception exception)
            {
                Disconnect();
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
