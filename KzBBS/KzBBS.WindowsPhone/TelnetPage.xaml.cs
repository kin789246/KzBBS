using KzBBS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
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
        public static TelnetPage Current;
        public TelnetPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            //TelnetConnect.connection.LinesPropChanged += telnetConnect_LinesPropChanged;
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
            normalScrollViewer.Height = 360 * factor;
            pttScrollViewer.Height = 360 * factor;
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
                boundControlBtns.Width = 600 * factor;
                boundControlBtns.Height = 360 * factor;
                PTTCanvas.Width = 600 * factor;
                PTTCanvas.Height = 360 * factor;

                sendCmd.Height = 22 * factor;
                sendCmd.MinWidth = 12 * 2 * factor;
                sendCmd.MinHeight = 15 * factor;
                sendCmd.FontSize = 12 * factor;

                BBSListView.Width = 600 * factor;
                BBSListView.Height = 360 * factor;
                normalScrollViewer.Width = 600 * factor;
                normalScrollViewer.Height = 360 * factor;
                pttScrollViewer.Width = 600 * factor;
                pttScrollViewer.Height = 360 * factor;
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
                    //byte[] cmd = Big5Util.ToBig5Bytes(sendCmd.Text);
                    //await TelnetConnect.sendCommand(cmd);
                    //statusBar.Text = sendCmd.Text + " sent";
                    //send big5 words one by one
                    foreach (var item in sendCmd.Text)
                    {
                        byte[] cmd = Big5Util.ToBig5Bytes(item.ToString());
                        await TelnetConnect.sendCommand(cmd);
                        statusBar.Text = item + " sent";
                    }
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
            buildMenuButton();
            if (PTTDisplay.PTTMode)
            {
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
            menuBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //choose menu items
            if (PTTDisplay.currentMode == BBSMode.Editor)
            {
                Visibility[] mfiVisibilty = { Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Collapsed, Visibility.Visible };
                setMenu(mfiVisibilty);
            }
            else if (PTTDisplay.currentMode == BBSMode.ArticleBrowse)
            {
                Visibility[] mfiVisibilty = { Visibility.Collapsed, Visibility.Visible, Visibility.Visible, Visibility.Visible,
                    Visibility.Visible, Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Visible, Visibility.Collapsed };
                setMenu(mfiVisibilty);
                
            }
            else if (PTTDisplay.currentMode == BBSMode.MainList)
            {
                Visibility[] mfiVisibilty = { Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Collapsed, Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed};
                setMenu(mfiVisibilty);
            }
            else if (PTTDisplay.currentMode == BBSMode.ArticleList)
            {
                Visibility[] mfiVisibilty = { Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Collapsed, Visibility.Visible, Visibility.Visible, Visibility.Collapsed,
                    Visibility.Visible, Visibility.Collapsed, Visibility.Visible, Visibility.Visible, Visibility.Visible,
                    Visibility.Collapsed, Visibility.Visible, Visibility.Collapsed};
                setMenu(mfiVisibilty);
            }
            else if (PTTDisplay.currentMode == BBSMode.BoardList || PTTDisplay.currentMode == BBSMode.ClassBoard)
            {
                Visibility[] mfiVisibilty = { Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Collapsed, Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Visible, Visibility.Collapsed};
                setMenu(mfiVisibilty);
            }
            else if (PTTDisplay.currentMode == BBSMode.MailList)
            {
                Visibility[] mfiVisibilty = { Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Visible,
                    Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed,
                    Visibility.Visible, Visibility.Visible, Visibility.Collapsed};
                setMenu(mfiVisibilty);
            }
            else
            {
                menuBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void setMenu(Windows.UI.Xaml.Visibility[] mfiVisibilty)
        {
            mfi_fileProcess.Visibility = mfiVisibilty[0];
            mfi_response.Visibility = mfiVisibilty[1];
            mfi_push.Visibility = mfiVisibilty[2];
            mfi_sameFirst.Visibility = mfiVisibilty[3];
            mfi_samePre.Visibility = mfiVisibilty[4];
            mfi_sameNext.Visibility = mfiVisibilty[5];
            mfi_searchBoard.Visibility = mfiVisibilty[6];
            mfi_post.Visibility = mfiVisibilty[7];
            mfi_newMail.Visibility = mfiVisibilty[8];
            mfi_searchArticle.Visibility = mfiVisibilty[9];
            mfi_search.Visibility = mfiVisibilty[10];
            mfi_searchAuthor.Visibility = mfiVisibilty[11];
            mfi_searchCode.Visibility = mfiVisibilty[12];
            mfi_essense.Visibility = mfiVisibilty[13];
            mfi_recycle.Visibility = mfiVisibilty[14];
            mfi_help.Visibility = mfiVisibilty[15];
            mfi_editorHelp.Visibility = mfiVisibilty[16];
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
                        articleListMenu.ShowAt(cs);
                        //int jumpCount = (int)(TelnetANSIParser.curPos.X - currentLine.No);
                        //var menu = new PopupMenu();
                        //menu.Commands.Add(new UICommand("推文 (X)", async (command) =>
                        //    {
                        //        await PTTDisplay.upOrDown(jumpCount);
                        //        await TelnetConnect.sendCommand("X");
                        //    }));

                        //menu.Commands.Add(new UICommand("回應 (y)", async (command) =>
                        //{
                        //    await PTTDisplay.upOrDown(jumpCount);
                        //    await TelnetConnect.sendCommand("y");
                        //}));

                        //if (currentLine.Author == PTTDisplay.User)
                        //{
                        //    menu.Commands.Add(new UICommand("編輯 (E)", async (command) =>
                        //    {
                        //        await PTTDisplay.upOrDown(jumpCount);
                        //        await TelnetConnect.sendCommand("E");
                        //    }));

                        //    menu.Commands.Add(new UICommand("刪除 (d)", async (command) =>
                        //    {
                        //        await PTTDisplay.upOrDown(jumpCount);
                        //        await TelnetConnect.sendCommand("d");
                        //    }));
                        //}

                        //menu.Commands.Add(new UICommand("同主題串接 (S)", async (command) =>
                        //{
                        //    await PTTDisplay.upOrDown(jumpCount);
                        //    await TelnetConnect.sendCommand("S");
                        //}));

                        //menu.Commands.Add(new UICommand("轉錄 (Ctrl+X)", async (command) =>
                        //{
                        //    await PTTDisplay.upOrDown(jumpCount);
                        //    await TelnetConnect.sendCommand(new byte[] { 24 });
                        //}));
                        //try
                        //{ 
                        //    await menu.ShowAsync(new Point(PTTCanvas.Width / 2, 0));
                        //}
                        //catch(Exception exp)
                        //{ Debug.WriteLine(exp.Message); }
                    }

                    if (PTTDisplay.currentMode == BBSMode.MailList)
                    {
                        mailListMenu.ShowAt(cs);
                        //int jumpCount = (int)(TelnetANSIParser.curPos.X - currentLine.No);
                        //var menu = new PopupMenu();
                        //menu.Commands.Add(new UICommand("回信 (y)", async (command) =>
                        //{
                        //    await PTTDisplay.upOrDown(jumpCount);
                        //    await TelnetConnect.sendCommand("y");
                        //}));

                        //menu.Commands.Add(new UICommand("刪除 (d)", async (command) =>
                        //{
                        //    await PTTDisplay.upOrDown(jumpCount);
                        //    await TelnetConnect.sendCommand("d");
                        //}));

                        //menu.Commands.Add(new UICommand("站內轉寄 (x)", async (command) =>
                        //{
                        //    await PTTDisplay.upOrDown(jumpCount);
                        //    await TelnetConnect.sendCommand("x");
                        //}));

                        //await menu.ShowAsync(new Point(PTTCanvas.Width / 2, 0));
                    }
                }
            }
        }

        private void getInput_Click(object sender, RoutedEventArgs e)
        {
            sendCmd.Focus(FocusState.Pointer);
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

        //private async void menuBtn_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    if (PTTDisplay.currentMode == BBSMode.Editor)
        //    {
        //        PopupMenu menu = new PopupMenu();
        //        menu.Commands.Add(new UICommand("檔案處理 (Ctrl+X)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand(new byte[] { 24 });
        //        }));
        //        menu.Commands.Add(new UICommand("說明 (Ctrl+Z)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand(new byte[] { 26 });
        //        }));
        //        await menu.ShowAsync(e.GetPosition(this));
        //    }
        //    else if (PTTDisplay.currentMode == BBSMode.ArticleBrowse)
        //    {
        //        PopupMenu menu = new PopupMenu();
                
        //        menu.Commands.Add(new UICommand("回應 (y)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("y");
        //        }));
        //        menu.Commands.Add(new UICommand("推文 (X)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("X");
        //        }));
        //        menu.Commands.Add(new UICommand("同主題第一篇 (=)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("=");
        //        }));
        //        menu.Commands.Add(new UICommand("同主題前篇 ([)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("[");
        //        }));
        //        menu.Commands.Add(new UICommand("同主題後篇 (])", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("]");
        //        }));
        //        //menu.Commands.Add(new UICommand("說明 (h)", async (cmd) =>
        //        //{
        //        //    await TelnetConnect.sendCommand("h");
        //        //}));
        //        await menu.ShowAsync(e.GetPosition(this));
        //    }
        //    else if (PTTDisplay.currentMode == BBSMode.MainList)
        //    {
        //        PopupMenu menu = new PopupMenu();
        //        menu.Commands.Add(new UICommand("搜尋看板 (s)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("s");
        //        }));
        //        await menu.ShowAsync(e.GetPosition(this));
        //    }
        //    else if (PTTDisplay.currentMode == BBSMode.ArticleList)
        //    {
        //        PopupMenu menu = new PopupMenu();
        //        menu.Commands.Add(new UICommand("搜尋看板 (s)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("s");
        //        }));
        //        menu.Commands.Add(new UICommand("發表文章 (Ctrl+P)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand(new byte[] { 16 });
        //        }));
        //        menu.Commands.Add(new UICommand("搜尋文章 (/)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("/");
        //        }));
        //        menu.Commands.Add(new UICommand("搜尋作者 (a)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("a");
        //        }));
        //        //menu.Commands.Add(new UICommand("搜尋文章代碼 (#)", async (cmd) =>
        //        //{
        //        //    await TelnetConnect.sendCommand("#");
        //        //}));
        //        //menu.Commands.Add(new UICommand("精華區 (z)", async (cmd) =>
        //        //{
        //        //    await TelnetConnect.sendCommand("z");
        //        //}));
        //        menu.Commands.Add(new UICommand("說明 (h)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("h");
        //        }));
        //        await menu.ShowAsync(e.GetPosition(this));
                
        //    }
        //    else if (PTTDisplay.currentMode == BBSMode.BoardList || PTTDisplay.currentMode == BBSMode.ClassBoard)
        //    {
        //        PopupMenu menu = new PopupMenu();
        //        menu.Commands.Add(new UICommand("搜尋看板 (s)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("s");
        //        }));
        //        menu.Commands.Add(new UICommand("搜尋 (/)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("/");
        //        }));
        //        menu.Commands.Add(new UICommand("說明 (h)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("h");
        //        }));
        //        await menu.ShowAsync(e.GetPosition(this));
        //    }
        //    else if (PTTDisplay.currentMode == BBSMode.MailList)
        //    {
        //        PopupMenu menu = new PopupMenu();
        //        menu.Commands.Add(new UICommand("發新郵件 (Ctrl+P)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand(new byte[] { 16 });
        //        }));
        //        menu.Commands.Add(new UICommand("資源回收桶 (~)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("~");
        //        })); 
        //        menu.Commands.Add(new UICommand("說明 (h)", async (cmd) =>
        //        {
        //            await TelnetConnect.sendCommand("h");
        //        }));
        //        await menu.ShowAsync(e.GetPosition(this));
        //    }
        //    else
        //    {
                
        //    }
        //}
    }
}
