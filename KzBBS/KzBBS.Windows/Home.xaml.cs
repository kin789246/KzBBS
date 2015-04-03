using KzBBS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using System.Collections.Concurrent;
using Windows.ApplicationModel.Store;
using Windows.Storage;
using Windows.ApplicationModel;
using Windows.UI.ViewManagement;
using Windows.UI.Popups;
using Windows.Data.Xml.Dom;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace KzBBS
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class Home : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public static Home Current;
        private double telnetFontSize = 30;
        public Home()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            //Current = this;

            if (!Big5Util.TableSeted)
            {
                //Big5Util.generateTable();
            }
            PTTDisplay._fontSize = telnetFontSize;

            Window.Current.SizeChanged += Current_SizeChanged;
            DetermineCanvasSize();
            NavigationBoard.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            hotKeyBoard.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            pushKey.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            boundControlBtns.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            appCanvas.ManipulationDelta += swipeUI_ManipulationDelta;
            appCanvas.ManipulationCompleted += swipeUI_ManipulationCompleted;
            appCanvas.ManipulationMode = ManipulationModes.TranslateRailsX | ManipulationModes.TranslateRailsY
            | ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            PTTCanvas.DoubleTapped += PTTCanvas_DoubleTapped;
            PTTCanvas.Tapped += PTTCanvas_Tapped;

            Application.Current.Resuming += Current_Resuming;

            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;
            versionText.Text = String.Format("Version: {0}.{1}.{2}.{3}\n",
                version.Major, version.Minor, version.Build, version.Revision);

            disconnBtn.IsEnabled = false;
        }

        async void PTTCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (PTTDisplay.currentMode == BBSMode.BoardList)
            {
                string getID = "";
                //Point current = e.GetPosition(TelnetViewbox);
                //if (current.Y > TelnetViewbox.Height) return;
                Point current = e.GetPosition(PTTCanvas);
                if (current.Y > PTTCanvas.Height) return;

                int lineNum = (int)(current.Y / telnetFontSize);
                if (lineNum >= 23) return;
                if (PTTDisplay.menuList[lineNum] == "") return;
                //contains "★"
                getID = PTTDisplay.menuList[lineNum];
                //if (getID.Length > 40)
                //{ getID = PTTDisplay.menuList[lineNum].Substring(0, 40); }
                //if (Regex.IsMatch(getID, @"(\s+\u2605\s+\S)") && getID.Length > 10)
                //{ getID = PTTDisplay.menuList[lineNum].Substring(0, 9); }

                string command = PTTDisplay.getSelectedID(getID);
                if (command == "") return;

                int voffset = (int)TelnetANSIParser.curPos.X - lineNum;
                if (voffset > 0) //up arrow
                {
                    for (int r = 0; r < voffset; r++)
                    { await sendCommand(new byte[] { 27, 91, 65 }); }
                }
                else //down arrow
                {
                    voffset = -voffset;
                    for (int r = 0; r < voffset; r++)
                    { await sendCommand(new byte[] { 27, 91, 66 }); }
                }

                //int number = 0;
                //if (command == "★")
                //{
                //    string lastCommand = PTTDisplay.getSelectedID(PTTDisplay.menuList[PTTDisplay.lastNum]);
                //    if (lastCommand != "")
                //    { number = lineNum - PTTDisplay.lastNum + Convert.ToInt32(lastCommand); }
                //    command = number.ToString();
                //}

                //if (Regex.IsMatch(command, @"[A-Z]"))
                //{
                //    await sendCommand(command);
                //}
                //else
                //{
                //    await sendCommand(command + "\r");
                //}

                //statusBar.Text = command + " sent";
            }
            e.Handled = true;
        }

        async void PTTCanvas_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            statusBar.Text = "double tapped";
            if (string.IsNullOrEmpty(sendCmd.Text))
            {
                await sendCommand("\r");
                statusBar.Text = "";
            }
            else
            {
                if (sendCmd.Text.Length == 1 && ctrlChecked.IsChecked == true)
                {
                    string upper = sendCmd.Text.ToUpper();
                    char ctrlWord = upper[0];
                    if (64 < ctrlWord && ctrlWord < 128)
                    {
                        int uletter = ctrlWord - 64;
                        byte[] cmd = { (byte)uletter };
                        await sendCommand(cmd);
                        statusBar.Text = "Ctrl + " + sendCmd.Text + " sent!";
                        sendCmd.Text = "";
                        ctrlChecked.IsChecked = false;
                    }
                }
                else
                {
                    byte[] cmd = Big5Util.ToBig5Bytes(sendCmd.Text);

                    await sendCommand(cmd);
                    sendCmd.Text = "";
                    statusBar.Text = "";
                }
            }
        }

        async void Current_Resuming(object sender, object e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //remove the cursor
                if (pointToCursor != null)
                { PTTCanvas.Children.Remove(pointToCursor); }
                //Set the cursor
                PTTCanvas.Children.Add(PTTDisplay.showBlinking("_", Colors.White, TelnetANSIParser.bg, telnetFontSize / 2
                    , TelnetANSIParser.curPos.X * telnetFontSize, TelnetANSIParser.curPos.Y * telnetFontSize / 2, 1));
                pointToCursor = PTTCanvas.Children[PTTCanvas.Children.Count - 1];
                //move textbox
                Canvas.SetLeft(sendCmd, TelnetANSIParser.curPos.Y * telnetFontSize / 4
                    + canvasOffset);
                Canvas.SetTop(sendCmd, (TelnetANSIParser.curPos.X - 1) * telnetFontSize / 2 + Canvas.GetTop(PTTCanvas));
            });
        }

        int iUp = 0, iDown = 0, iLeft = 0, iRight = 0;
        async void swipeUI_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            //Debug.WriteLine(e.Delta.Translation);
            if (e.Delta.Translation.X == 0)
            {
                if (e.Delta.Translation.Y > 0)
                {
                    iDown++;
                    if (iDown % 10 == 0)
                    {
                        statusBar.Text = "slide down ";
                        if (PTTDisplay.currentMode == BBSMode.ArticleBrowse)
                        { await sendCommand("j"); }
                        else
                        { await sendCommand(new byte[] { 27, 91, 66 }); }
                    }
                    e.Handled = true;
                }
                if (e.Delta.Translation.Y < 0)
                {
                    iUp++;
                    if (iUp % 10 == 0)
                    {
                        statusBar.Text = "slide up ";
                        if (PTTDisplay.currentMode == BBSMode.ArticleBrowse)
                        { await sendCommand("k"); }
                        else
                        { await sendCommand(new byte[] { 27, 91, 65 }); }
                    }
                    e.Handled = true;
                }
            }
            else
            {
                if (e.Delta.Translation.X > 0)
                {
                    iRight++;
                    if (iRight % 10 == 0)
                    {
                        statusBar.Text = "swipe right ";
                        byte[] cmd = { 27, 91, 67 };
                        await sendCommand(cmd);
                        e.Complete();
                    }
                    e.Handled = true;
                }
                if (e.Delta.Translation.X < 0)
                {
                    iLeft++;
                    if (iLeft % 10 == 0)
                    {
                        statusBar.Text = "swipe left ";
                        byte[] cmd = { 27, 91, 68 };
                        await sendCommand(cmd);
                        e.Complete();
                    }
                    e.Handled = true;
                }
            }
        }

        void swipeUI_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            iUp = 0;
            iDown = 0;
            iLeft = 0;
            iRight = 0;
        }

        void controlUI_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            UIElement board = sender as UIElement;
            if (board == null) return;
            checkUIBoardBound(board);
            e.Handled = true;
        }

        void controlUI_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            UIElement board = sender as UIElement;
            if (board == null) return;
            double uiL = Canvas.GetLeft(board);
            double uiT = Canvas.GetTop(board);
            Canvas.SetLeft(board, uiL + e.Delta.Translation.X);
            Canvas.SetTop(board, uiT + e.Delta.Translation.Y);
            e.Handled = true;
        }

        double factor = 1;
        double canvasOffset = 10;
        async private void DetermineCanvasSize()
        {
            Rect winSize = Window.Current.Bounds;
            appCanvas.Width = winSize.Width;
            appCanvas.Height = winSize.Height;
            swipeUI.Width = winSize.Width;
            swipeUI.Height = winSize.Height;
            
            aboutPanel.Measure(new Size(double.MaxValue, double.MaxValue));
            Canvas.SetLeft(aboutPanel, winSize.Width / 2 - aboutPanel.DesiredSize.Width / 2);
            Canvas.SetTop(aboutPanel, winSize.Height / 2 - aboutPanel.DesiredSize.Height / 2);

            buttonsPanel.Measure(new Size(double.MaxValue, double.MaxValue));
            Canvas.SetTop(buttonsPanel, winSize.Height - buttonsPanel.DesiredSize.Height);

            checkUIBoardBound(connCtrlUI);
            checkUIBoardBound(NavigationBoard);

            if (winSize.Width > winSize.Height)
            {
                PTTDisplay.isPortrait = false;
                factor = (winSize.Width / 1280 > winSize.Height / 800) ? winSize.Height / 800 : winSize.Width / 1280;
                telnetFontSize = 30 * factor;
                PTTDisplay._fontSize = 30 * factor;
                clipB.FontSize = 30 * factor;
                clipB.LineHeight = 30 * factor;
                PTTDisplay.chtOffset = 5 * factor;
                PTTCanvas.Width = 1200 * factor;
                PTTCanvas.Height = 720 * factor;
                //canvasOffset = (winSize.Width - PTTCanvas.Width) / 2;
                Canvas.SetLeft(PTTCanvas, canvasOffset);

                //boundControlBtns.Visibility = Windows.UI.Xaml.Visibility.Visible;
                boundControlBtns.Width = 1200 * factor;
                boundControlBtns.Height = 720 * factor;
                Canvas.SetLeft(boundControlBtns, canvasOffset);
                Canvas.SetTop(boundControlBtns, Canvas.GetTop(PTTCanvas));

                Canvas.SetLeft(hotkeyPanel, PTTCanvas.Width - 10);

                sendCmd.Height = 33 * factor;
                sendCmd.MinWidth = 15 * factor;
                sendCmd.MinHeight = 33 * factor;
                sendCmd.FontSize = 22 * factor;
            }
            else
            {
                PTTDisplay.isPortrait = true;
                factor = winSize.Height / 1200;
                //TelnetViewbox.Width = winSize.Width;
                //TelnetViewbox.Height = winSize.Height;
                //Canvas.SetLeft(TelnetViewbox, 0);
                telnetFontSize = 25 * factor;
                PTTDisplay._fontSize = 25 * factor;
                clipB.FontSize = 25 * factor;
                clipB.LineHeight = 25 * factor;
                PTTCanvas.Width = winSize.Width;
                PTTCanvas.Height = winSize.Height;
                canvasOffset = 0;
                Canvas.SetLeft(PTTCanvas, 0);
                boundControlBtns.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ////reset BBSPage
                //for (int line = 0; line < 24; line++)
                //{
                //    if (TelnetANSIParser.BBSPage[line] == null) continue;
                //    foreach (TelnetData block in TelnetANSIParser.BBSPage[line])
                //    {
                //        foreach (UIElement ui in block.BBSUI)
                //        { PTTCanvas.Children.Remove(ui); }
                //        block.BBSUI.Clear();
                //    }
                //}
                //PTTDisplay.showAll(PTTCanvas, TelnetANSIParser.BBSPage);

                //remove the cursor
                if (pointToCursor != null)
                { PTTCanvas.Children.Remove(pointToCursor); }
                //Set the cursor
                PTTCanvas.Children.Add(PTTDisplay.showBlinking("_", Colors.White, TelnetANSIParser.bg, telnetFontSize / 2
                    , TelnetANSIParser.curPos.X * telnetFontSize, TelnetANSIParser.curPos.Y * telnetFontSize / 2, 1));
                pointToCursor = PTTCanvas.Children[PTTCanvas.Children.Count - 1];
                //move textbox
                Canvas.SetLeft(sendCmd, TelnetANSIParser.curPos.Y * telnetFontSize / 2 + canvasOffset);
                Canvas.SetTop(sendCmd, (TelnetANSIParser.curPos.X - 1) * telnetFontSize + Canvas.GetTop(PTTCanvas));
            });
        }

        private void checkUIBoardBound(UIElement UIBoard)
        {
            Rect winSize = Window.Current.Bounds;
            double UBoardL, UBoardT;
            UBoardL = Canvas.GetLeft(UIBoard);
            UBoardT = Canvas.GetTop(UIBoard);
            UIBoard.Measure(new Size(double.MaxValue, double.MaxValue));

            if (UBoardT <= 0)
            { Canvas.SetTop(UIBoard, 0); }
            if (UBoardL <= 0)
            { Canvas.SetLeft(UIBoard, 0); }
            if (UBoardL + UIBoard.DesiredSize.Width >= winSize.Width)
            {
                Canvas.SetLeft(UIBoard, winSize.Width - UIBoard.DesiredSize.Width);
            }
            if (UBoardT + UIBoard.DesiredSize.Height >= winSize.Height)
            {
                Canvas.SetTop(UIBoard, winSize.Height - UIBoard.DesiredSize.Height);
                if (winSize.Height > winSize.Width)
                    Canvas.SetTop(UIBoard, winSize.Height - UIBoard.DesiredSize.Height - 40);
            }
        }

        void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            DetermineCanvasSize();
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // Restore values stored in app data.
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey("nextTimeChkBoxChecked"))
            {
                if (roamingSettings.Values["nextTimeChkBoxChecked"].ToString() == "true")
                {
                    aboutPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    aboutClose = true;
                    nextTimeChkBox.IsChecked = true;
                }
                else if(roamingSettings.Values["nextTimeChkBoxChecked"].ToString() == "false")
                {
                    aboutPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    aboutClose = false;
                    nextTimeChkBox.IsChecked = false;
                }
            }

            if (roamingSettings.Values.ContainsKey("telnetAccount"))
            {
                tAccount.Text = roamingSettings.Values["telnetAccount"].ToString();
                clearSaved.IsEnabled = true;
            }
            if (roamingSettings.Values.ContainsKey("telnetPassword"))
            {
                tPwd.Password = roamingSettings.Values["telnetPassword"].ToString();
            }
            if(roamingSettings.Values.ContainsKey("telnetAddress"))
            { tIP.Text = roamingSettings.Values["telnetAddress"].ToString(); }
            else
            { tIP.Text = "ptt.cc"; }
            if(roamingSettings.Values.ContainsKey("telnetPort"))
            { tPort.Text = roamingSettings.Values["telnetPort"].ToString(); }
            else
            { tPort.Text = "23"; }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void ClientAddLine(string text)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                statusBar.Text = text;
            }
            );
        }

        static byte[] rawdata = new byte[0];
        private async void ClientWaitForMessage()
        {
            int MAXBuffer = 6000;
            DataReader reader = new DataReader(TelnetSocket.PTTSocket.ClientSocket.InputStream);
            reader.InputStreamOptions = InputStreamOptions.Partial;
            try
            {
                while (true)
                {
                    uint Message = await reader.LoadAsync((uint)MAXBuffer).AsTask(TelnetSocket.PTTSocket.cts.Token);

                    rawdata = new byte[Message];
                    reader.ReadBytes(rawdata);
                    if (rawdata.Length != 0)
                    {
                        await goTouchVersion(rawdata);
                    }
                    else
                    {
                        TelnetSocket.PTTSocket.cts.Cancel();
                    }
                }
            }
            catch (Exception exception)
            {
                //TelnetSocket.ShowMessage(exception.Message);
                if (TelnetSocket.PTTSocket.IsConnected)
                {
                    TelnetSocket.PTTSocket.Disconnect();
                }

                Debug.WriteLine("Read stream failed with error: " + exception.Message);
                return;
            }
        }

        static Windows.ApplicationModel.Resources.ResourceLoader loader = 
            new Windows.ApplicationModel.Resources.ResourceLoader();
        bool autoLogin = false;
        public async Task OnConnect(string tIP, string tPort)
        {
            TelnetSocket.PTTSocket.SocketDisconnect += PTTSocket_SocketDisconnect;

            if (!TelnetSocket.PTTSocket.IsConnected)
            {
                //connStatus.Text = "連線中...";
                connStatus.Text = loader.GetString("connecting");
                try
                {
                    await TelnetSocket.PTTSocket.Connect(tIP, tPort);
                    //await Task.Factory.StartNew(ClientWaitForMessage);
                    ClientWaitForMessage();
                    //connStatus.Text = "已連線";
                    connStatus.Text = loader.GetString("Conn");
                    connStatus.Foreground = new SolidColorBrush(Colors.Green);
                    if (!string.IsNullOrEmpty(tAccount.Text))
                    {
                        autoLogin = true;
                        rememberAcPd.IsChecked = false;
                    }
                }
                catch
                {
                    return;
                }
            }
            else
            {
                //ShowMessage("已經連線了好嗎!");
                ShowMessage(loader.GetString("alreadyconnect"));
                //this.Frame.GoBack();
            }
        }

        static UIElement pointToCursor;
        private async Task goTouchVersion(byte[] rdata)
        {
            TelnetANSIParser.HandleAnsiESC(rdata);
            //PTTDisplay.LoadFromSource(TelnetANSIParser.BBSPage);
            //this.DefaultViewModel["PTTLines"] = PTTDisplay.Lines;
  
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //remove last cursor
                if (pointToCursor != null)
                { PTTCanvas.Children.Remove(pointToCursor); }

                //PTTDisplay.DisplayLanscape(PTTCanvas, TelnetANSIParser.BBSPage);
                //PTTDisplay._currentMode = PTTDisplay.checkMode();
                PTTDisplay.showAll(PTTCanvas, TelnetANSIParser.BBSPage);

                clipB.Inlines.Clear();
                PTTDisplay.getText();
                PTTDisplay.generateHyperlink(PTTDisplay.forClipBoard, clipB, Colors.Transparent);
                //Set the cursor
                PTTCanvas.Children.Add(PTTDisplay.showBlinking("_", Colors.White, TelnetANSIParser.bg, telnetFontSize / 2
                    , TelnetANSIParser.curPos.X * telnetFontSize, TelnetANSIParser.curPos.Y * telnetFontSize / 2, 1));
                pointToCursor = PTTCanvas.Children[PTTCanvas.Children.Count - 1];

                //Canvas.SetLeft(sendCmd, TelnetANSIParser.curPos.Y * telnetFontSize / 2
                //    + (Window.Current.Bounds.Width - PTTCanvas.Width) / 2);
                Canvas.SetLeft(sendCmd, TelnetANSIParser.curPos.Y * telnetFontSize / 2 + canvasOffset);
                Canvas.SetTop(sendCmd, TelnetANSIParser.curPos.X * telnetFontSize + Canvas.GetTop(PTTCanvas));

                //if (PTTDisplay.isPushable)
                //{ pushKey.Visibility = Windows.UI.Xaml.Visibility.Visible; }
                //else
                //{ pushKey.Visibility = Windows.UI.Xaml.Visibility.Collapsed; }
            });
            if ((PTTDisplay.forClipBoard.Contains("guest") || PTTDisplay.forClipBoard.Contains("勇者代號"))
                && autoLogin == true)
            {
                await sendCommand(tAccount.Text + "\r" + tPwd.Password + "\r");
                autoLogin = false;
            }
            
        }

        private void displayPage()
        {
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 80; j++)
                {
                    if (TelnetANSIParser.BBSPage[i, j] == null) 
                        TelnetANSIParser.BBSPage[i, j] = new TelnetData();
                    TextBlock tb = new TextBlock();
                    tb.FontSize = 30;
                    tb.LineHeight = 30;
                    tb.Foreground = new SolidColorBrush(Colors.White);
                    tb.DataContext = TelnetANSIParser.BBSPage[i, j];
                    tb.SetBinding(TextBlock.TextProperty,
                        new Binding() { Path = new PropertyPath("Text") });
                    //tb.SetBinding(TextBlock.ForegroundProperty, 
                    //    new Binding() { Path = new PropertyPath("ForeColor") });
                    Canvas.SetLeft(tb, j * 30 / 2);
                    Canvas.SetTop(tb, i * 30);
                    PTTCanvas.Children.Add(tb);
                }
            }
        }

        private async void cmdOrText_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Shift) return;
            if(e.Key == Windows.System.VirtualKey.Control)
            { 
                ctrlChecked.IsChecked = false;
                return;
            }
            //check space command
            if (e.Key == Windows.System.VirtualKey.Space && sendCmd.Text == " ")
            {
                sendCmd.Text = "";
                bskey = true;
                return;
            }
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await sendCommand("\r");
                    statusBar.Text = e.Key.ToString() + " sent";
                }
                else
                {
                    //if(sendCmd.Text.Length > 1)
                    //{
                    byte[] cmd = Big5Util.ToBig5Bytes(sendCmd.Text);
                    await sendCommand(cmd);
                    statusBar.Text = sendCmd.Text + " sent";
                    sendCmd.Text = "";
                    //}
                }
                bskey = true;
                return;
            }
            //check ctrl + ? combination key
            if (ctrlChecked.IsChecked == true)
            {
                if (64 < (int)e.Key && (int)e.Key < 91)
                {
                    int uletter = (int)e.Key - 64;
                    byte[] cmd = { (byte)uletter };
                    await sendCommand(cmd);
                    statusBar.Text = "Ctrl + " + e.Key.ToString() + " sent";
                    sendCmd.Text = "";
                    bskey = true;
                    return;
                }
            }
            //check single letter command
            if (sendCmd.Text.Length == 1)
            {
                if (sendCmd.Text[0] < 256)
                {
                    await sendCommand(sendCmd.Text);
                    statusBar.Text = sendCmd.Text + " sent";
                    sendCmd.Text = "";
                    bskey = true;
                }
                return;
            }
            
            if(string.IsNullOrEmpty(sendCmd.Text))
            { bskey = true; }
            else
            { bskey = false; }

        }

        bool bskey = false;
        async void cmdOrText_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Shift) return;
            if (e.Key == Windows.System.VirtualKey.Control)
            {
                ctrlChecked.IsChecked = true;
                return;
            }
            if (e.Key == Windows.System.VirtualKey.Up)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await sendCommand(new byte[] { 27, 91, 65 });
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Down)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await sendCommand(new byte[] { 27, 91, 66 });
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Left)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await sendCommand(new byte[] { 27, 91, 68 });
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Right)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await sendCommand(new byte[] { 27, 91, 67 });
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Home)
            {
                await sendCommand(new byte[] { 27, 91, 49, 126 });  //ESC [ 1 ~
                statusBar.Text = "Home key sent";
            }
            else if (e.Key == Windows.System.VirtualKey.End)
            {
                await sendCommand(new byte[] { 27, 91, 52, 126 }); //ESC [ 4 ~
                statusBar.Text = "End key sent";
            }
            else if (e.Key == Windows.System.VirtualKey.PageUp)
            {
                await sendCommand(new byte[] { 27, 91, 53, 126 });  //ESC [ 5 ~
                statusBar.Text = "PageUp key sent";
            }
            else if (e.Key == Windows.System.VirtualKey.PageDown)
            {
                await sendCommand(new byte[] { 27, 91, 54, 126 }); //ESC [ 6 ~
                statusBar.Text = "PageDown key sent";
            }
            else if (e.Key == Windows.System.VirtualKey.Space)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await sendCommand(" ");
                    statusBar.Text = "space key sent";
                    sendCmd.Text = "";
                    bskey = true;
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Back)
            {
                if (bskey)
                {
                    await sendCommand("\b");
                    statusBar.Text = "backspace key sent";
                    bskey = false;
                }
            }
        }
        private async Task sendCommand(byte[] interpretByte)
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
                ShowMessage(exception.Message);
                TelnetSocket.PTTSocket.Disconnect();
                //ClientAddLine("Send failed with message: " + exception.Message);
            }
        }

        private async Task sendCommand(string sourceString)
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
                string source = exception.Message;
                ShowMessage(source);
                TelnetSocket.PTTSocket.Disconnect();
                //ClientAddLine("Send failed with message: " + exception.Message);
            }
        }

        async void PTTSocket_SocketDisconnect(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //connStatus.Text = "未連線";
                connStatus.Text = loader.GetString("unConn");
                connStatus.Foreground = new SolidColorBrush(Colors.DarkRed);
                connBtn.IsEnabled = true;
                PTTCanvas.Children.Clear();
                PTTDisplay.forClipBoard = "";
                TelnetANSIParser.resetAllSetting();
            });
        }

        private byte[] interpreteToByte(string sourceString)
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

        private async void Enter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(sendCmd.Text))
            {
                await sendCommand("\r");
                statusBar.Text = "";
            }
            else
            {
                if (sendCmd.Text.Length == 1 && ctrlChecked.IsChecked == true)
                {
                    string upper = sendCmd.Text.ToUpper();
                    char ctrlWord = upper[0];
                    if (64 < ctrlWord && ctrlWord < 128)
                    {
                        int uletter = ctrlWord - 64;
                        byte[] cmd = { (byte)uletter };
                        await sendCommand(cmd);
                        statusBar.Text = "Ctrl + " + sendCmd.Text + " sent!";
                        sendCmd.Text = "";
                        ctrlChecked.IsChecked = false;
                    }
                }
                else
                {
                    byte[] cmd = Big5Util.ToBig5Bytes(sendCmd.Text);

                    await sendCommand(cmd);
                    sendCmd.Text = "";
                    statusBar.Text = "";
                }
            }
        }

        bool connMiniSize = false;
        private void connMini_Click(object sender, RoutedEventArgs e)
        {
            if (connMiniSize)
            {
                connCtrlUI.Visibility = Windows.UI.Xaml.Visibility.Visible;
                connMiniSize = false;
                //connButton.Content = "隱藏連線";
                connButton.Content = loader.GetString("hideconn2");
            }
            else
            {
                connCtrlUI.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                connMiniSize = true;
                //connButton.Content = "顯示連線";
                connButton.Content = loader.GetString("showconn2");
            }
            checkUIBoardBound(connCtrlUI);
        }

        bool miniSize = true;
        private void miniMax_Click(object sender, RoutedEventArgs e)
        {
            if (miniSize)
            {
                hotKeyBoard.Visibility = Windows.UI.Xaml.Visibility.Visible;
                miniSize = false;
                //advButton.Content = "隱藏進階";
                advButton.Content = loader.GetString("hideadvance2");
            }
            else
            {
                hotKeyBoard.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                miniSize = true;
                //advButton.Content = "顯示進階";
                advButton.Content = loader.GetString("showadvance2");
            }
            checkUIBoardBound(NavigationBoard);
        }

        bool NaviMini = true;
        private void naviMini_Click(object sender, RoutedEventArgs e)
        {
            if (NaviMini)
            {
                NavigationBoard.Visibility = Windows.UI.Xaml.Visibility.Visible;
                NaviMini = false;
                //navButton.Content = "隱藏操作";
                navButton.Content = loader.GetString("hidehotkey2");
            }
            else
            {
                NavigationBoard.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                NaviMini = true;
                //navButton.Content = "顯示操作";
                navButton.Content = loader.GetString("showhotkey2");
            }
            checkUIBoardBound(NavigationBoard);
        }

        private async void buy_Click(object sender, RoutedEventArgs e)
        {
            //StorageFolder proxyDataFolder = await Package.Current.InstalledLocation.GetFolderAsync("ApiData");
            //StorageFile proxyFile = await proxyDataFolder.GetFileAsync("WindowsStoreProxy.xml");
            //await CurrentAppSimulator.ReloadSimulatorAsync(proxyFile);
            //ListingInformation listing = await CurrentAppSimulator.LoadListingInformationAsync();
            //var product1 = listing.ProductListings["support1USD"];
            //LicenseInformation licenseInfo = CurrentAppSimulator.LicenseInformation;
            LicenseInformation licenseInfo = CurrentApp.LicenseInformation;
            //var productionLicense = licenseInfo.ProductLicenses["support1USD"];
            
            if (licenseInfo.IsTrial)
            {
                //the customer doesn't buy this app
                try
                {
                    //await CurrentAppSimulator.RequestAppPurchaseAsync(true);
                    await CurrentApp.RequestAppPurchaseAsync(true);
                    if (!licenseInfo.IsTrial)
                    { //ShowMessage("感謝你的購買");
                        ShowMessage(loader.GetString("thanksforbuy"));
                    }
                }
                catch (Exception)
                { //ShowMessage("購買失敗");
                    ShowMessage(loader.GetString("buyfailed"));
                }
            }
            else
            {
                //ShowMessage("感謝, 你已經買過了.");
                ShowMessage(loader.GetString("alreadybuy"));
            }
        }

        private async void connect_Click(object sender, RoutedEventArgs e)
        {
            #region test ListView
            //Uri dataUri = new Uri("ms-appx:///bahamut_sample.txt");
            //StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            //string sample = await FileIO.ReadTextAsync(file);
            //List<byte> rdata = new List<byte>();
            //foreach (string item in sample.Split(' '))
            //{
            //    if (item == "\r" || item == "\n" || item == "\r\n")
            //    {
            //        continue;
            //    }
            //    rdata.Add(byte.Parse(item));
            //}
            //await goTouchVersion(rdata.ToArray());
            #endregion
            if (string.IsNullOrEmpty(tIP.Text) || string.IsNullOrEmpty(tPort.Text)) return;
            connBtn.IsEnabled = false;
            disconnBtn.IsEnabled = true;
            await OnConnect(tIP.Text, tPort.Text);
            if (TelnetSocket.PTTSocket.IsConnected)
            {
                connCtrlUI.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                connMiniSize = true;
                //connButton.Content = "顯示連線";
                connButton.Content = loader.GetString("showconn2");
            }
        }
        private void disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (TelnetSocket.PTTSocket.Connecting)
            {
                //    //ShowMessage("沒有連線就沒有斷線");
                //    ShowMessage(loader.GetString("noconnnodisconn"));
                TelnetSocket.PTTSocket.cts.Cancel();
            }
            else
            {
                TelnetSocket.PTTSocket.Disconnect();
            }
            disconnBtn.IsEnabled = false;
            //OnDisconnect();
        }

        bool canvasHide = false;
        string _fontFamily = PTTDisplay.ansi_fontFamily;
        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (PTTDisplay.forClipBoard == "") return;
            if (canvasHide)
            {
                appCanvas.ManipulationDelta += swipeUI_ManipulationDelta;
                appCanvas.ManipulationCompleted += swipeUI_ManipulationCompleted;
                PTTCanvas.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //boundControlBtns.Visibility = Windows.UI.Xaml.Visibility.Visible;
                sendCmd.Visibility = Windows.UI.Xaml.Visibility.Visible;
                canvasHide = false;
                swipeUI.Children.Clear();
                //selButton.Content = "選取文字";
                selButton.Content = loader.GetString("selWords2");
            }
            else
            {
                TextBlock clipBSel = new TextBlock();
                appCanvas.ManipulationDelta -= swipeUI_ManipulationDelta;
                appCanvas.ManipulationCompleted -= swipeUI_ManipulationCompleted;
                PTTCanvas.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                //boundControlBtns.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                sendCmd.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                clipBSel.FontSize = telnetFontSize;
                clipBSel.FontFamily = new FontFamily(_fontFamily);
                clipBSel.LineStackingStrategy = LineStackingStrategy.BaselineToBaseline;
                clipBSel.LineHeight = telnetFontSize;
                clipBSel.IsTextSelectionEnabled = true;
                clipBSel.Foreground = new SolidColorBrush(Colors.LightGray);
                //string test = "abchttp://www.windowsphone.com/es-es/store/app/%E7%A5%9E%E5%9F%9F%E6%97%A0%E5%8F% xxx";
                
                //PTTDisplay.getText();
                PTTDisplay.generateHyperlink(PTTDisplay.forClipBoard, clipBSel, Colors.LightBlue);
                Canvas clipCS = new Canvas();
                clipCS.Width = Window.Current.Bounds.Width;
                clipCS.Height = Window.Current.Bounds.Height;
                clipCS.Children.Add(clipBSel);
                ScrollViewer clipSV = new ScrollViewer();
                //Canvas.SetLeft(clipSV, Canvas.GetLeft(TelnetViewbox));
                Canvas.SetLeft(clipSV, Canvas.GetLeft(PTTCanvas));
                Canvas.SetTop(clipSV, Canvas.GetTop(PTTCanvas));
                clipSV.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                clipSV.ZoomMode = ZoomMode.Enabled;
                clipSV.MinZoomFactor = 1;
                clipSV.Width = Window.Current.Bounds.Width;
                clipSV.Height = Window.Current.Bounds.Height;
                clipSV.Content = clipCS;
                swipeUI.Children.Add(clipSV);
                canvasHide = true;
                //selButton.Content = "回到瀏覽";
                selButton.Content = loader.GetString("backTo");
            }
        }

        private async void remember_Checked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tAccount.Text) || string.IsNullOrEmpty(tPwd.Password))
            {
                //ShowMessage("帳號密碼都要輸入的啦! 請重發, 謝謝.");
                ShowMessage(loader.GetString("plsreinput"));
                rememberAcPd.IsChecked = false;
                return;
            }
            bool isYes = true;
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey("telnetAccount") || roamingSettings.Values.ContainsKey("telnetPassword"))
            {
                //confirm overwrite
                //isYes = await confirmDialog("已經存在帳號: " + roamingSettings.Values["telnetAccount"]
                //    + ", 要覆蓋嗎?");
                isYes = await confirmDialog(loader.GetString("existaccount") + roamingSettings.Values["telnetAccount"]
                    + loader.GetString("needoverwrite"));
            }
            if (isYes)
            {
                roamingSettings.Values["telnetAccount"] = tAccount.Text;
                roamingSettings.Values["telnetPassword"] = tPwd.Password;
                roamingSettings.Values["telnetAddress"] = tIP.Text;
                roamingSettings.Values["telnetPort"] = tPort.Text;
                clearSaved.IsEnabled = true;
                //ShowMessage("位址, 帳號, 密碼已存");
                ShowMessage(loader.GetString("itsaved"));
            }
            else
            { //ShowMessage("位址, 帳號, 密碼未存入"); 
                ShowMessage(loader.GetString("itdoesntsaved"));
            }
        }

        private async void clearSaved_Click(object sender, RoutedEventArgs e)
        {
            bool isYes = true;
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey("telnetAccount") && roamingSettings.Values.ContainsKey("telnetPassword"))
            {
                //isYes = await confirmDialog("要刪除帳號: " + roamingSettings.Values["telnetAccount"]
                //    + " 嗎?");
                isYes = await confirmDialog(loader.GetString("todeleteacct") + roamingSettings.Values["telnetAccount"]
                    + loader.GetString("yesornot"));
                if (isYes)
                {
                    roamingSettings.Values.Remove("telnetAccount");
                    roamingSettings.Values.Remove("telnetPassword");
                    roamingSettings.Values.Remove("telnetAddress");
                    roamingSettings.Values.Remove("telnetPort");
                    clearSaved.IsEnabled = false;
                }
                else
                { //ShowMessage("帳號密碼未刪除");
                    ShowMessage(loader.GetString("notdeleted"));
                }
            }
            else
            {
                //ShowMessage("找不到已儲存的帳號密碼.");
                ShowMessage(loader.GetString("cantfind"));
            }
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

        public async static void ShowMessage(string msg)
        {
            var loadString = new Windows.ApplicationModel.Resources.ResourceLoader();
            var messageDialog = new MessageDialog(msg);
            //messageDialog.Title = "訊息通知";
            messageDialog.Title = loader.GetString("infoNotify");
            await messageDialog.ShowAsync();
        }

        bool aboutClose = false;
        private void About_Click(object sender, RoutedEventArgs e)
        {
            if (aboutClose)
            {
                aboutPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                aboutClose = false;
                checkUIBoardBound(aboutPanel);
            }
            else
            {
                aboutPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                aboutClose = true;
            }
        }

        private void aboutClose_Click(object sender, RoutedEventArgs e)
        {
            aboutPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            aboutClose = true;
        }
        private async void hotKey_Click(object sender, RoutedEventArgs e)
        {
            ButtonBase bn = sender as ButtonBase;
            if (bn == null) return;
            string cmd = bn.Content as string;
            if (cmd.Length == 1 && char.IsLetterOrDigit(cmd[0]))
            {
                await sendCommand(cmd);
            }
            else if (cmd[0] == '^')
            {
                int send = cmd[1] - 96;
                await sendCommand(new byte[] { (byte)send });
            }
            else
            {
                switch (cmd)
                {
                    case "SPACE":
                        await sendCommand(" ");
                        break;
                    case "←":
                        await sendCommand("\b");
                        break;
                    case "▲":
                        await sendCommand(new byte[] { 27, 91, 65 });
                        break;
                    case "▼":
                        await sendCommand(new byte[] { 27, 91, 66 });
                        break;
                    case "◄":
                        await sendCommand(new byte[] { 27, 91, 68 });
                        break;
                    case "►":
                        await sendCommand(new byte[] { 27, 91, 67 });
                        break;
                    case "PgUp":
                        await sendCommand(new byte[] { 27, 91, 53, 126 }); //Esc [ 5 ~
                        break;
                    case "PgDn":
                        await sendCommand(new byte[] { 27, 91, 54, 126 }); //ESC [ 6 ~
                        break;
                    case "/":
                        await sendCommand("/");
                        break;
                    case "[":
                        await sendCommand("[");
                        break;
                    case "]":
                        await sendCommand("]");
                        break;
                    case "=":
                        await sendCommand("=");
                        break;
                    case "#":
                        await sendCommand("#");
                        break;
                    case "Home":
                        await sendCommand(new byte[] { 27, 91, 49, 126 }); //ESC [ 1 ~
                        break;
                    case "End":
                        await sendCommand(new byte[] { 27, 91, 52, 126 }); //ESC [ 4 ~
                        break;
                    default:
                        break;
                }
            }
        }

        private void nextTime_Checked(object sender, RoutedEventArgs e)
        {
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["nextTimeChkBoxChecked"] = "true";
        }

        private void nextTime_Unchecked(object sender, RoutedEventArgs e)
        {
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["nextTimeChkBoxChecked"] = "false";
        }

        private void getInput_Click(object sender, RoutedEventArgs e)
        {
            sendCmd.Focus(FocusState.Programmatic);
        }

        private void push_Click(object sender, RoutedEventArgs e)
        {
            statusBar.Text = "push key pressed";
        }

        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            return;
        }
    }
}
