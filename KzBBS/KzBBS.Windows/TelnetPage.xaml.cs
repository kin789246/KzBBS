﻿using KzBBS.Common;
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
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
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
        public static TelnetPage Current;
        public TelnetPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            Window.Current.SizeChanged += Current_SizeChanged;
            DetermineSize();
            if (!PTTDisplay.PTTMode)
            { //set the cursor
                PTTCanvas.Children.Add(PTTDisplay.showBlinking("_", Colors.White, TelnetANSIParser.bg, PTTDisplay._fontSize / 2
                        , TelnetANSIParser.curPos.X * PTTDisplay._fontSize, TelnetANSIParser.curPos.Y * PTTDisplay._fontSize / 2, 1));
                cursor = PTTCanvas.Children[PTTCanvas.Children.Count - 1];
            }

            InputPane inpa = InputPane.GetForCurrentView();
            inpa.Showing += inpa_Showing;
            inpa.Hiding += inpa_Hiding;
            Current = this;
        }

        void inpa_Hiding(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            normalScrollViewer.Height = 720 * factor;
            pttScrollViewer.Height = 800 * factor;
        }

        void inpa_Showing(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            InputPane inpa = sender as InputPane;
            if (inpa != null)
            {
                if (PTTDisplay.PTTMode)
                {
                    pttScrollViewer.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    pttScrollViewer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
                if (normalScrollViewer.Height > inpa.OccludedRect.Height)
                {
                    normalScrollViewer.Height -= inpa.OccludedRect.Height;
                    pttScrollViewer.Height -= inpa.OccludedRect.Height;
                }
            }
        }

        private void SV_sizeChanged(object sender, SizeChangedEventArgs e)
        {
            double cursorPosition = (TelnetANSIParser.curPos.X + 1) * PTTDisplay._fontSize;
            if (cursorPosition > normalScrollViewer.Height)
            {
                normalScrollViewer.ChangeView(null, cursorPosition - normalScrollViewer.Height, null);
                pttScrollViewer.ChangeView(null, cursorPosition - normalScrollViewer.Height, null);
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
                BBSStackPanel.Height = 800 * factor;
                operationBoard.Width = 1200 * factor;
                operationBoard.Height = 720 * factor;
                boundControlBtns.Width = 1200 * factor;
                boundControlBtns.Height = 720 * factor;
                PTTCanvas.Width = 1200 * factor;
                PTTCanvas.Height = 720 * factor;

                sendCmd.Height = 33 * factor;
                sendCmd.MinWidth = 15 * 2 * factor;
                sendCmd.MinHeight = 33 * factor;
                sendCmd.FontSize = 22 * factor;

                BBSListView.Width = 1200 * factor;
                BBSListView.Height = 720 * factor;
                normalScrollViewer.Width = 1200 * factor;
                normalScrollViewer.Height = 720 * factor;
                pttScrollViewer.Width = 1200 * factor;
                pttScrollViewer.Height = 800 * factor;
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
                    //byte[] cmd = Big5Util.ToBig5Bytes(sendCmd.Text);
                    //await TelnetConnect.sendCommand(cmd);
                    foreach (var item in sendCmd.Text)
                    {
                        byte[] cmd = Big5Util.ToBig5Bytes(item.ToString());
                        await TelnetConnect.sendCommand(cmd);
                        statusBar.Text = item + " sent";
                    }
                    //statusBar.Text = sendCmd.Text + " sent";
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
                }
                ctrlChecked.IsChecked = false;
                return;
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
                        break;
                    case "►":
                        await TelnetConnect.sendCommand(new byte[] { 27, 91, 67 });
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

        public void onDataChange()
        {
            if (PTTDisplay.PTTMode)
            {
                buildMenuButton();
                PTTDisplay.ShowBBSListView(topStackPanel, BBSListView, bottomStackPanel, PTTDisplay.Lines, operationBoard);
            }
            else
            {
                PTTDisplay.showBBS(PTTCanvas, PTTDisplay.Lines, operationBoard);
                Canvas.SetLeft(cursor, TelnetANSIParser.curPos.Y * PTTDisplay._fontSize / 2);
                Canvas.SetTop(cursor, TelnetANSIParser.curPos.X * PTTDisplay._fontSize);
            }
        }

        private void buildMenuButton()
        {
            if (PTTDisplay.currentMode == BBSMode.Editor)
            {
                editorMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                articleBrowseMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mainMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                boardListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mailMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else if (PTTDisplay.currentMode == BBSMode.ArticleBrowse)
            {
                editorMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleBrowseMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                mainMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                boardListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mailMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else if (PTTDisplay.currentMode == BBSMode.MainList)
            {
                editorMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleBrowseMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mainMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                articleListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                boardListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mailMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;   
            }
            else if (PTTDisplay.currentMode == BBSMode.ArticleList)
            {
                editorMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleBrowseMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mainMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                boardListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mailMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else if (PTTDisplay.currentMode == BBSMode.BoardList || PTTDisplay.currentMode == BBSMode.ClassBoard)
            {
                editorMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleBrowseMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mainMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                boardListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                mailMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else if (PTTDisplay.currentMode == BBSMode.MailList)
            {
                editorMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleBrowseMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mainMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                boardListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mailMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                editorMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleBrowseMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mainMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                articleListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                boardListMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                mailMenuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;   
            }
        }

        async void mfi_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem mfi = sender as MenuFlyoutItem;
            if (mfi != null)
            {
                switch (mfi.Text)
                {
                    case "檔案處理 (Ctrl+X)":
                        await TelnetConnect.sendCommand(new byte[] { 24 });
                        break;
                    case "說明 (Ctrl+Z)":
                        await TelnetConnect.sendCommand(new byte[] { 26 });
                        break;
                    case "回應 (y)":
                        await TelnetConnect.sendCommand("y");
                        break;
                    case "推文 (X)":
                        await TelnetConnect.sendCommand("X");
                        break;
                    case "同主題第一篇 (=)":
                        await TelnetConnect.sendCommand("=");
                        break;
                    case "同主題前篇 ([)":
                        await TelnetConnect.sendCommand("[");
                        break;
                    case "同主題後篇 (])":
                        await TelnetConnect.sendCommand("]");
                        break;
                    case "說明 (h)":
                        await TelnetConnect.sendCommand("h");
                        break;
                    case "搜尋看板 (s)":
                        await TelnetConnect.sendCommand("s");
                        break;
                    case "發表文章 (Ctrl+P)":
                    case "發新郵件 (Ctrl+P)":
                        await TelnetConnect.sendCommand(new byte[] { 16 });
                        break;
                    case "搜尋文章 (/)":
                    case "搜尋 (/)":
                        await TelnetConnect.sendCommand("/");
                        break;
                    case "搜尋作者 (a)":
                        await TelnetConnect.sendCommand("a");
                        break;
                    case "搜尋文章代碼 (#)":
                        await TelnetConnect.sendCommand("#");
                        break;
                    case "精華區 (z)":
                        await TelnetConnect.sendCommand("z");
                        break;
                    case "資源回收桶 (~)":
                        await TelnetConnect.sendCommand("~");
                        break;
                    default:
                        break;
                }
            }
        }
        private async void BBSListViewItem_Click(object sender, ItemClickEventArgs e)
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
                    PTTLine pl = PTTDisplay.Lines.Find(x => x.UniqueId == id);
                    if (pl != null)
                    {
                        int jumpCount = (int)(TelnetANSIParser.curPos.X - pl.No);
                        await PTTDisplay.upOrDown(jumpCount);
                        await TelnetConnect.sendCommand("\r");
                    }
                }
                if (PTTDisplay.currentMode == BBSMode.PressAnyKey || PTTDisplay.currentMode == BBSMode.AnimationPlay)
                {
                    await TelnetConnect.sendCommand(" ");
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

        PTTLine currentLine;
        private void BBSListViewItem_RTapped(object sender, RightTappedRoutedEventArgs e)
        {
            TextBlock tb = e.OriginalSource as TextBlock;
            if (tb != null)
            {
                Canvas cs = tb.Parent as Canvas;
                if (cs != null && cs.Tag != null)
                {
                    string uniqueId = cs.Tag.ToString();
                    currentLine = PTTDisplay.Lines.FindLast(x => x.UniqueId == uniqueId);
                    if (currentLine == null)
                    {
                        return;
                    }
                    if (PTTDisplay.currentMode == BBSMode.ArticleList)
                    {
                        if (currentLine.Author == PTTDisplay.User)
                        {
                            editor.Visibility = Windows.UI.Xaml.Visibility.Visible;
                            delete.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        }
                        else
                        {
                            editor.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                            delete.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        }
                        articleListMenu.ShowAt(cs);
                    }

                    if (PTTDisplay.currentMode == BBSMode.MailList)
                    {
                        mailListMenu.ShowAt(cs);
                    }
                }
            }
        }
        private async void rt_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem mfi = sender as MenuFlyoutItem;
            if (mfi != null)
            {
                int jumpCount = (int)(TelnetANSIParser.curPos.X - currentLine.No);
                await PTTDisplay.upOrDown(jumpCount);
                switch (mfi.Text)
                {
                    case "推文 (X)":
                        await TelnetConnect.sendCommand("X");
                        break;
                    case "回應 (y)":
                    case "回信 (y)":
                        await TelnetConnect.sendCommand("y");
                        break;
                    case "編輯 (E)":
                        await TelnetConnect.sendCommand("E");
                        break;
                    case "刪除 (d)":
                        await TelnetConnect.sendCommand("d");
                        break;
                    case "同主題串接 (S)":
                        await TelnetConnect.sendCommand("S");
                        break;
                    case "轉錄 (Ctrl+X)":
                        await TelnetConnect.sendCommand(new byte[] { 24 });
                        break;
                    case "站內轉寄 (x)":
                        await TelnetConnect.sendCommand("x");
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
