using KzBBS.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class MainPage : Page
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

        static Windows.ApplicationModel.Resources.ResourceLoader loader =
            new Windows.ApplicationModel.Resources.ResourceLoader();
        //public static MainPage Current;
        public MainPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

            TelnetSocket.PTTSocket.SocketDisconnect += PTTSocket_SocketDisconnect;
            disconnBtn.IsEnabled = false;
            PTTMode.IsChecked = true;
            loadProfile();
            //Current = this;
        }

        Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
        private void loadProfile()
        {
            if (roamingSettings.Values.ContainsKey("telnetAccount"))
            {
                tAccount.Text = roamingSettings.Values["telnetAccount"].ToString();
                clearSaved.IsEnabled = true;
            }
            if (roamingSettings.Values.ContainsKey("telnetPassword"))
            {
                tPwd.Password = roamingSettings.Values["telnetPassword"].ToString();
            }
            if (roamingSettings.Values.ContainsKey("telnetAddress"))
            { tIP.Text = roamingSettings.Values["telnetAddress"].ToString(); }
            else
            { tIP.Text = "ptt.cc"; }
            if (roamingSettings.Values.ContainsKey("telnetPort"))
            { tPort.Text = roamingSettings.Values["telnetPort"].ToString(); }
            else
            { tPort.Text = "23"; }
        }

        void PTTSocket_SocketDisconnect(object sender, EventArgs e)
        {
            connBtn.IsEnabled = true;
            disconnBtn.IsEnabled = false;
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

        private async void connect_Click(object sender, RoutedEventArgs e)
        {
            disconnBtn.IsEnabled = true;
            connBtn.IsEnabled = false;
            if (string.IsNullOrEmpty(tIP.Text) || string.IsNullOrEmpty(tPort.Text)) return;
            if (!string.IsNullOrEmpty(tAccount.Text) && !string.IsNullOrEmpty(tPwd.Password))
            {
                TelnetConnect.connection.autoLogin = true;
                TelnetConnect.connection.account = tAccount.Text;
                TelnetConnect.connection.password = tPwd.Password;
            }
            try
            {
                await TelnetConnect.connection.OnConnect(tIP.Text, tPort.Text);
            }
            catch(Exception exp)
            {
                TelnetSocket.ShowMessage(exp.Message);
                return;
            }
            if (PTTMode.IsChecked == true)
            {
                PTTDisplay.PTTMode = true;
            }
            else
            {
                PTTDisplay.PTTMode = false;
            }
            this.Frame.Navigate(typeof(TelnetPage));
        }

        private void disconnect_Click(object sender, RoutedEventArgs e)
        {
            TelnetConnect.connection.OnDisconnect();
        }

        private async void remember_Checked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tAccount.Text) || string.IsNullOrEmpty(tPwd.Password))
            {
                //ShowMessage("帳號密碼都要輸入的啦! 請重發, 謝謝.");
                TelnetSocket.ShowMessage(loader.GetString("plsreinput"));
                rememberAcPd.IsChecked = false;
                return;
            }
            bool isYes = true;
            //Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey("telnetAccount") || roamingSettings.Values.ContainsKey("telnetPassword"))
            {
                //confirm overwrite
                //isYes = await confirmDialog("已經存在帳號: " + roamingSettings.Values["telnetAccount"]
                //    + ", 要覆蓋嗎?");
                isYes = await TelnetSocket.confirmDialog(loader.GetString("existaccount") + roamingSettings.Values["telnetAccount"]
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
                TelnetSocket.ShowMessage(loader.GetString("itsaved"));
            }
            else
            { //ShowMessage("位址, 帳號, 密碼未存入"); 
                TelnetSocket.ShowMessage(loader.GetString("itdoesntsaved"));
            }
        }

        private void clearSaved_Click(object sender, RoutedEventArgs e)
        {

        }

        private void goTelnetPage(object sender, RoutedEventArgs e)
        {
            if (TelnetSocket.PTTSocket.IsConnected)
            {
                this.Frame.Navigate(typeof(TelnetPage));
            }
            else
            {
                TelnetSocket.ShowMessage("please connect first");
            }
        }
    }
}
