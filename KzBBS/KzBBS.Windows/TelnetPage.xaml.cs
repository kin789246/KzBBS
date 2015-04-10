using KzBBS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace KzBBS
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class TelnetPage : Page
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

        UIElement cursor;
        public TelnetPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
            
            PTTDisplay.pttDisplay.LinesPropChanged += telnetConnect_LinesPropChanged;
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

        double factor = 1;
        private void DetermineSize()
        {
            Rect winSize = Window.Current.Bounds;

            if (winSize.Width > winSize.Height)
            {
                PTTDisplay.isPortrait = false;
                factor = (winSize.Width / 1280 > winSize.Height / 800) ? winSize.Height / 800 : winSize.Width / 1280;
                PTTDisplay._fontSize = 30 * factor;
                PTTDisplay.chtOffset = 5 * factor;
                BBSStackPanel.Width = 1200 * factor;
                BBSStackPanel.Height = 720 * factor;
                operationBoard.Width = 1200 * factor;
                operationBoard.Height = 720 * factor;
                PTTCanvas.Width = 1200 * factor;
                PTTCanvas.Height = 720 * factor;

                sendCmd.Height = 33 * factor;
                sendCmd.MinWidth = 15 * factor;
                sendCmd.MinHeight = 33 * factor;
                sendCmd.FontSize = 22 * factor;

                BBSListView.Width = 1200 * factor;
                BBSListView.Height = 720 * factor;
            }
            else
            {
                PTTDisplay.isPortrait = true;
                factor = winSize.Height / 1200;
                //TelnetViewbox.Width = winSize.Width;
                //TelnetViewbox.Height = winSize.Height;
                //Canvas.SetLeft(TelnetViewbox, 0);
                PTTDisplay._fontSize = 25 * factor;
            }
            //PTTDisplay.pt_sendCmd = sendCmd;
        }

        void telnetConnect_LinesPropChanged(object sender, EventArgs e)
        {
            onDataChange();
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
                    await TelnetConnect.sendCommand("\r");
                    statusBar.Text = e.Key.ToString() + " sent";
                }
                else
                {//TODO: send big5 words one by one
                    byte[] cmd = Big5Util.ToBig5Bytes(sendCmd.Text);
                    await TelnetConnect.sendCommand(cmd);
                    statusBar.Text = sendCmd.Text + " sent";
                    sendCmd.Text = "";
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
                    await TelnetConnect.sendCommand(cmd);
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
                    await TelnetConnect.sendCommand(sendCmd.Text);
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
                    loadCount = 0;
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Right)
            {
                if (string.IsNullOrEmpty(sendCmd.Text))
                {
                    await TelnetConnect.sendCommand(new byte[] { 27, 91, 67 });
                    loadCount = 0;
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
                    sendCmd.Text = "";
                    bskey = true;
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Back)
            {
                if (bskey)
                {
                    await TelnetConnect.sendCommand("\b");
                    statusBar.Text = "backspace key sent";
                    bskey = false;
                }
            }
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
                        if (!PTTDisplay.CurrPage[PTTDisplay.CurrPage.Count-1].Text.Contains("100%"))
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
