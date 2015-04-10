using KzBBS.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace KzBBS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TelnetPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        UIElement cursor;
        public TelnetPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            TelnetConnect.connection.LinesPropChanged += telnetConnect_LinesPropChanged;
            Window.Current.SizeChanged += Current_SizeChanged;
            DetermineSize();

            if (!PTTDisplay.PTTMode)
            { //set the cursor
                PTTCanvas.Children.Add(PTTDisplay.showBlinking("_", Colors.White, TelnetANSIParser.bg, PTTDisplay._fontSize / 2
                        , TelnetANSIParser.curPos.X * PTTDisplay._fontSize, TelnetANSIParser.curPos.Y * PTTDisplay._fontSize / 2, 1));
                cursor = PTTCanvas.Children[PTTCanvas.Children.Count - 1];
            }
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            DetermineSize();
        }

        StatusBar topStatusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
        double factor = 1;
        private async void DetermineSize()
        {
            Rect winSize = Window.Current.Bounds;

            if (winSize.Width > winSize.Height)
            {
                await topStatusBar.HideAsync();

                PTTDisplay.isPortrait = false;
                factor = (winSize.Width / 640 > winSize.Height / 384) ? winSize.Height / 384 : winSize.Width / 640;
                PTTDisplay._fontSize = 15 * factor;
                PTTDisplay.chtOffset = 1 * factor;
                BBSStackPanel.Width = 600 * factor;
                BBSStackPanel.Height = 360 * factor;
                operationBoard.Width = 600 * factor;
                operationBoard.Height = 360 * factor;
                PTTCanvas.Width = 600 * factor;
                PTTCanvas.Height = 360 * factor;

                sendCmd.Height = 22 * factor;
                sendCmd.MinWidth = 12 * factor;
                sendCmd.MinHeight = 15 * factor;
                sendCmd.FontSize = 12 * factor;

                BBSListView.Width = 600 * factor;
                BBSListView.Height = 360 * factor;
            }
            else
            {
                PTTDisplay.isPortrait = true;
                factor = winSize.Height / 600;
                //TelnetViewbox.Width = winSize.Width;
                //TelnetViewbox.Height = winSize.Height;
                //Canvas.SetLeft(TelnetViewbox, 0);
                PTTDisplay._fontSize = 12.5 * factor;
            }
        }

        void telnetConnect_LinesPropChanged(object sender, EventArgs e)
        {
            onDataChange();
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (PTTDisplay.Lines.Count != 0)
            {
                onDataChange();
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void cmdOrText_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Shift) return;
            if (e.Key == Windows.System.VirtualKey.Space && sendCmd.Text == " ")
            {
                sendCmd.Text = "";
                return;
            }
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await TelnetConnect.sendCommand("\r");
                    statusBar.Text = e.Key.ToString() + " sent";
                }
                else
                {
                    byte[] cmd = Big5Util.ToBig5Bytes(sendCmd.Text);
                    await TelnetConnect.sendCommand(cmd);
                    statusBar.Text = sendCmd.Text + " sent";
                    sendCmd.Text = "";
                }
                return;
            }
            if (ctrlChecked.IsChecked == true)
            {
                if (64 < (int)e.Key && (int)e.Key < 91)
                {
                    int uletter = (int)e.Key - 64;
                    byte[] cmd = { (byte)uletter };
                    await TelnetConnect.sendCommand(cmd);
                    statusBar.Text = "Ctrl + " + e.Key.ToString() + " sent";
                    sendCmd.Text = "";
                }
                ctrlChecked.IsChecked = false;
                return;
            }
            if (sendCmd.Text.Length == 1)
            {
                if (sendCmd.Text[0] < 256)
                {
                    await TelnetConnect.sendCommand(sendCmd.Text);
                    statusBar.Text = sendCmd.Text + " sent";
                    sendCmd.Text = "";
                }
            }
        }

        async void cmdOrText_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Shift) return;
            if (e.Key == Windows.System.VirtualKey.Up)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await TelnetConnect.sendCommand(new byte[] { 27, 91, 65 });
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Down)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await TelnetConnect.sendCommand(new byte[] { 27, 91, 66 });
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Left)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await TelnetConnect.sendCommand(new byte[] { 27, 91, 68 });
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Right)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await TelnetConnect.sendCommand(new byte[] { 27, 91, 67 });
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Home)
            {
                await TelnetConnect.sendCommand(new byte[] { 27, 91, 49, 126 });  //ESC [ 1 ~
                statusBar.Text = "Home key sent";
            }
            else if (e.Key == Windows.System.VirtualKey.End)
            {
                await TelnetConnect.sendCommand(new byte[] { 27, 91, 52, 126 }); //ESC [ 4 ~
                statusBar.Text = "End key sent";
            }
            else if (e.Key == Windows.System.VirtualKey.PageUp)
            {
                await TelnetConnect.sendCommand(new byte[] { 27, 91, 53, 126 });  //ESC [ 5 ~
                statusBar.Text = "PageUp key sent";
            }
            else if (e.Key == Windows.System.VirtualKey.PageDown)
            {
                await TelnetConnect.sendCommand(new byte[] { 27, 91, 54, 126 }); //ESC [ 6 ~
                statusBar.Text = "PageDown key sent";
            }
            else if (e.Key == Windows.System.VirtualKey.Space)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await TelnetConnect.sendCommand(" ");
                    statusBar.Text = "space key sent";
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Back)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await TelnetConnect.sendCommand("\b");
                    statusBar.Text = "backspace key sent";
                }
            }
        }

        private async void Enter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(sendCmd.Text))
            {
                await TelnetConnect.sendCommand("\r");
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
                        await TelnetConnect.sendCommand(cmd);
                        statusBar.Text = "Ctrl + " + sendCmd.Text + " sent!";
                        sendCmd.Text = "";
                        ctrlChecked.IsChecked = false;
                    }
                }
                else
                {
                    byte[] cmd = Big5Util.ToBig5Bytes(sendCmd.Text);

                    await TelnetConnect.sendCommand(cmd);
                    sendCmd.Text = "";
                    statusBar.Text = "";
                }
            }
        }

        private async void hotKey_Click(object sender, RoutedEventArgs e)
        {
            ButtonBase bn = sender as ButtonBase;
            if (bn == null) return;
            string cmd = bn.Content as string;

            if (cmd.Length == 1 && char.IsLetterOrDigit(cmd[0]))
            {
                await TelnetConnect.sendCommand(cmd);
            }
            else if (cmd[0] == '^')
            {
                int send = cmd[1] - 96;
                await TelnetConnect.sendCommand(new byte[] { (byte)send });
            }
            else
            {
                switch (cmd)
                {
                    case "SPACE":
                        await TelnetConnect.sendCommand(" ");
                        break;
                    case "←":
                        await TelnetConnect.sendCommand("\b");
                        break;
                    case "▲":
                        await TelnetConnect.sendCommand(new byte[] { 27, 91, 65 });
                        break;
                    case "▼":
                        await TelnetConnect.sendCommand(new byte[] { 27, 91, 66 });
                        break;
                    case "◄":
                        await TelnetConnect.sendCommand(new byte[] { 27, 91, 68 });
                        loadCount = 0;
                        break;
                    case "►":
                        await TelnetConnect.sendCommand(new byte[] { 27, 91, 67 });
                        loadCount = 0;
                        break;
                    case "PgUp":
                        await TelnetConnect.sendCommand(new byte[] { 27, 91, 53, 126 }); //Esc [ 5 ~
                        break;
                    case "PgDn":
                        await TelnetConnect.sendCommand(new byte[] { 27, 91, 54, 126 }); //ESC [ 6 ~
                        break;
                    case "/":
                        await TelnetConnect.sendCommand("/");
                        break;
                    case "[":
                        await TelnetConnect.sendCommand("[");
                        break;
                    case "]":
                        await TelnetConnect.sendCommand("]");
                        break;
                    case "=":
                        await TelnetConnect.sendCommand("=");
                        break;
                    case "#":
                        await TelnetConnect.sendCommand("#");
                        break;
                    case "Home":
                        await TelnetConnect.sendCommand(new byte[] { 27, 91, 49, 126 }); //ESC [ 1 ~
                        break;
                    case "End":
                        await TelnetConnect.sendCommand(new byte[] { 27, 91, 52, 126 }); //ESC [ 4 ~
                        break;
                    default:
                        break;
                }
            }
        }

        private void getInput_Click(object sender, RoutedEventArgs e)
        {
            sendCmd.Focus(FocusState.Programmatic);
        }

        static int loadCount = 0;
        private async void onDataChange()
        {
            if (PTTDisplay.PTTMode)
            {
                if (PTTDisplay.currentMode == BBSMode.ArticleBrowse)
                {


                    if (PTTDisplay.LastMode != BBSMode.ArticleBrowse)
                    {
                        BBSListView.Items.Clear();
                        PTTCanvas.Children.Clear();
                        topStackPanel.Children.Clear();
                        bottomStackPanel.Children.Clear();
                        PTTCanvas.Children.Add(PTTDisplay.showWord("Loading...", TelnetANSIParser.nGray, PTTDisplay._fontSize / 2 * 10, 0, 0, 0));
                        PTTDisplay.CurrPage.Clear();
                        PTTDisplay.CurrPage = PTTDisplay.Lines.ToList();
                        if (!PTTDisplay.CurrPage[PTTDisplay.CurrPage.Count - 1].Text.Contains("100%"))
                        {
                            PTTDisplay.CurrPage.RemoveAt(PTTDisplay.CurrPage.Count - 1);
                        }
                    }

                    PTTDisplay.articleAddToCurrPage();
                    if (!PTTDisplay.Lines[PTTDisplay.Lines.Count - 1].Text.Contains("100%") && loadCount < 5)
                    {  //keep pagedown until 100%
                        await TelnetConnect.sendCommand(new byte[] { 27, 91, 54, 126 });
                        //await TelnetConnect.sendCommand("j");
                        //await Task.Delay(200);
                        loadCount++;
                        return;
                    }
                    PTTCanvas.Children.Clear();
                    PTTDisplay.CurrPage.Add(PTTDisplay.Lines[PTTDisplay.Lines.Count - 1]);
                }
                //else if (PTTDisplay.currentMode == BBSMode.BoardList)
                //{

                //}
                else
                {
                    PTTDisplay.CurrPage.Clear();
                    PTTDisplay.CurrPage = PTTDisplay.Lines.ToList();
                }
                PTTDisplay.ShowBBSListView(topStackPanel, BBSListView, bottomStackPanel, PTTDisplay.CurrPage);
            }
            else
            {
                PTTDisplay.showBBS(PTTCanvas, PTTDisplay.Lines);
                Canvas.SetLeft(sendCmd, TelnetANSIParser.curPos.Y * PTTDisplay._fontSize / 2);
                Canvas.SetTop(sendCmd, TelnetANSIParser.curPos.X * PTTDisplay._fontSize);
                Canvas.SetLeft(cursor, TelnetANSIParser.curPos.Y * PTTDisplay._fontSize / 2);
                Canvas.SetTop(cursor, TelnetANSIParser.curPos.X * PTTDisplay._fontSize);
            }
        }

        private async void BBSListVitemItem_Click(object sender, ItemClickEventArgs e)
        {
            Canvas lineCanvas = e.ClickedItem as Canvas;
            if (lineCanvas != null)
            {
                string id = "";
                if (lineCanvas.Tag != null)
                {
                    id = lineCanvas.Tag.ToString();
                }
                if (!string.IsNullOrEmpty(id))
                {
                    if (Regex.IsMatch(lineCanvas.Tag.ToString(), @"[a-zA-Z]+")) //letter
                    //if (PTTDisplay.currentMode == BBSMode.MainList)
                    {
                        await TelnetConnect.sendCommand(id + "\r");
                    }
                    else if (Regex.IsMatch(lineCanvas.Tag.ToString(), @"\d+")) //numbers
                    //else if (PTTDisplay.currentMode == BBSMode.BoardList || PTTDisplay.currentMode == BBSMode.Essence)
                    {
                        //updatePage = false;
                        if (PTTDisplay.currentMode == BBSMode.MainList)
                        {
                            await TelnetConnect.sendCommand(id + "\r");
                        }
                        else
                        {
                            await TelnetConnect.sendCommand(id + "\r\r");
                        }
                        //if (PTTDisplay.currentMode == BBSMode.ArticleBrowse)
                        //{
                        //    await TelnetConnect.sendCommand("Q");
                        //    PTTLine line = PTTDisplay.Lines.First(x => x.No == 19);
                        //    if (line != null)
                        //    {
                        //       foreach( var text in line.Text.Split(' '))
                        //       {
                        //           if(Regex.IsMatch(text, @"#\w+\s"))
                        //           {
                        //               Debug.WriteLine("文章代碼: " + text);
                        //           }
                        //       }
                        //    }
                        //    await TelnetConnect.sendCommand(" ");
                        //}
                        //onDataChange();
                        //updatePage = true;
                    }
                }
            }
            else
            {
                if (PTTDisplay.currentMode == BBSMode.PressAnyKey || PTTDisplay.currentMode == BBSMode.AnimationPlay)
                {
                    await TelnetConnect.sendCommand(" ");
                }
            }
        }
    }
}
