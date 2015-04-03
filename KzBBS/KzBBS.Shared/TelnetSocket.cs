using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;

namespace KzBBS
{
    class TelnetSocket
    {
        public static TelnetSocket PTTSocket = new TelnetSocket();

        private StreamSocket clientSocket;
        private HostName serverHost;
        private string serverPort;
        private bool connected = false;
        private bool connecting = false;

        public CancellationTokenSource cts { get; set; }
        public StreamSocket ClientSocket { get { return clientSocket; } }
        public bool IsConnected { get { return connected; } }
        public bool Connecting { get { return connecting; } }

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

            catch (Exception exception)
            {
                ShowMessage(exception.Message);
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
        }

        static Windows.ApplicationModel.Resources.ResourceLoader loader =
            new Windows.ApplicationModel.Resources.ResourceLoader();
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
            if(SocketDisconnect !=null)
            {
                SocketDisconnect(this,e);
            }
        }
    }
}
