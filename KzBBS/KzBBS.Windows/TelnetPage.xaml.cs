using KzBBS.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
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


        public TelnetPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            PTTDisplay.pttDisplay.LinesPropChanged += pttDisplay_LinesPropChanged;
            Window.Current.SizeChanged += Current_SizeChanged;
            DetermineSize();
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            DetermineSize();
        }

        double factor = 1;
        double canvasOffset = 10;
        private void DetermineSize()
        {
            Rect winSize = Window.Current.Bounds;

            if (winSize.Width > winSize.Height)
            {
                PTTDisplay.isPortrait = false;
                factor = (winSize.Width / 1280 > winSize.Height / 800) ? winSize.Height / 800 : winSize.Width / 1280;
                PTTDisplay._fontSize = 30 * factor;
                PTTDisplay.chtOffset = 5 * factor;
                PTTCanvas.Width = 1200 * factor;
                PTTCanvas.Height = 720 * factor;
                //canvasOffset = (winSize.Width - PTTCanvas.Width) / 2;

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
                PTTDisplay._fontSize = 25 * factor;
            }
        }

        void pttDisplay_LinesPropChanged(object sender, EventArgs e)
        {
            PTTDisplay.showBBS(PTTCanvas);
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
                PTTDisplay.showBBS(PTTCanvas);
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
    }
}
