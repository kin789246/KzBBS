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
        private StreamSocket clientSocket;
        private HostName serverHost;
        private string serverPort;
        private bool connected = false;
        public CancellationTokenSource cts { get; set; }
        public StreamSocket ClientSocket { get { return this.clientSocket; } }
        public bool IsConnected { get { return this.connected; } }

        //public TelnetSocket(string host, string port)
        //{
        //    clientSocket = new StreamSocket();
        //    serverHost = new HostName(host);
        //    serverPort = port;
        //}

        public TelnetSocket()
        {
            clientSocket = new StreamSocket();
            cts = new CancellationTokenSource();
        }

        public async Task Connect(string host, string port)
        {
            serverHost = new HostName(host);
            serverPort = port;
            if (connected)
            {
                ShowMessage("本來就連線了阿!");
                return;
            }
            // Try to connect to the PTT
            try
            {
                await clientSocket.ConnectAsync(serverHost, serverPort).AsTask(cts.Token);
                connected = true;
            }

            catch (Exception exception)
            {
                ShowMessage(exception.Message);
                Disconnect();
                throw;
                // If this is an unknown status, 
                // it means that the error is fatal and retry will likely fail.
                //if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                //{
                //    ShowMessage("檢查一下你的網路是不是沒有連線.");
                //}

                //ShowMessage(exception.Message);
                // Could retry the connection, but for this simple example
                // just close the socket.

                //closing = true;
                //clientSocket.Dispose();
                //clientSocket = null;
                //connected = false;
                //ShowMessage("已經斷線");
            }
        }

        public void Disconnect()
        {
            if (connected)
            {
                clientSocket.Dispose();
                clientSocket = null;
                connected = false;
                //ShowMessage("已經斷線");
            }
            onSocketDisconnect(new EventArgs());
        } 

        public async static void ShowMessage(string msg)
        {
            var messageDialog = new MessageDialog(msg);
            messageDialog.Title = "訊息通知";
            await messageDialog.ShowAsync();
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
