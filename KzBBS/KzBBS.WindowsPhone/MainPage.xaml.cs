﻿using KzBBS.Common;
using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.AdMediator.Core.Events;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace KzBBS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        
        public static TelnetConnect connection = new TelnetConnect();
        static Windows.ApplicationModel.Resources.ResourceLoader loader =
            new Windows.ApplicationModel.Resources.ResourceLoader();
        public MainPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            
            connection.SocketDisconnect += PTTSocket_SocketDisconnect;
            disconnBtn.IsEnabled = false;
            PTTMode.IsChecked = false;
            isBig5.IsChecked = true;
            loadProfile();

            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;
            versionText.Text = String.Format("Version: {0}.{1}.{2}.{3}\n",
                version.Major, version.Minor, version.Build, version.Revision);

            AdMediator_1697A3.AdMediatorError += AdMediator_Bottom_AdMediatorError;
        }

        private void AdMediator_Bottom_AdMediatorError(object sender, AdMediatorFailedEventArgs e)
        {
            Debug.WriteLine("AdMediatorError:" + e.Error + " " + e.ErrorCode);
            // if (e.ErrorCode == AdMediatorErrorCode.NoAdAvailable)
            // AdMediator will not show an ad for this mediation cycle
        }

        private void loadProfile()
        {
            //check if need show ad
            LicenseInformation licenseInfo = CurrentApp.LicenseInformation;
            if (licenseInfo.IsActive)
            {
                Debug.WriteLine("ad disable");
                AdMediator_1697A3.Visibility = Visibility.Collapsed;
            }
            else
            {
                Debug.WriteLine("ad enable");
                AdMediator_1697A3.Visibility = Visibility.Visible;
            }

            //load user profile
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
        
            if (roamingSettings.Values.ContainsKey("telnetAccount"))
            {
                tAccount.Text = roamingSettings.Values["telnetAccount"].ToString();
                //clearSaved.IsEnabled = true;
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
            if (roamingSettings.Values.ContainsKey("PTTMode"))
            {
                if (roamingSettings.Values["PTTMode"].ToString() == "yes")
                { PTTMode.IsChecked = true; }
                else
                { PTTMode.IsChecked = false; }
            }
            if (roamingSettings.Values.ContainsKey("telnetEncode"))
            {
                if (roamingSettings.Values["telnetEncode"].ToString() == "big5")
                { isBig5.IsChecked = true; }
                else
                { isGBK.IsChecked = true; }
            }
        }

        void PTTSocket_SocketDisconnect(object sender, EventArgs e)
        {
            connBtn.IsEnabled = true;
            disconnBtn.IsEnabled = false;
            isBig5.IsEnabled = true;
            isGBK.IsEnabled = true;
            PTTDisplay.resetAllSetting();
            TelnetANSIParser.resetAllSetting();
            if (TelnetPage.Current != null)
            { TelnetPage.Current.resetAllSetting(); }
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
            loadProfile();
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
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["telnetAccount"] = tAccount.Text;
            roamingSettings.Values["telnetPassword"] = tPwd.Password;
            roamingSettings.Values["telnetAddress"] = tIP.Text;
            roamingSettings.Values["telnetPort"] = tPort.Text;
            if (PTTMode.IsChecked == true)
            { roamingSettings.Values["PTTMode"] = "yes"; }
            else
            { roamingSettings.Values["PTTMode"] = "no"; }
            if (isBig5.IsChecked == true)
            { roamingSettings.Values["telnetEncode"] = "big5"; }
            else
            { roamingSettings.Values["telnetEncode"] = "gbk"; }
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

        private async void connect_Click(object sender, RoutedEventArgs e)
        {
            disconnBtn.IsEnabled = true;
            connBtn.IsEnabled = false;
            isBig5.IsEnabled = false;
            isGBK.IsEnabled = false;
            if (string.IsNullOrEmpty(tIP.Text) || string.IsNullOrEmpty(tPort.Text)) return;
            if (!string.IsNullOrEmpty(tAccount.Text) && !string.IsNullOrEmpty(tPwd.Password))
            {
                connection.autoLogin = true;
                connection.account = tAccount.Text;
                connection.password = tPwd.Password;
            }
            try
            {
                await connection.OnConnect(tIP.Text, tPort.Text);
            }
            catch (Exception exp)
            {
                TelnetConnect.ShowMessage(exp.Message);
                return;
            }
            this.Frame.Navigate(typeof(TelnetPage));
        }

        private void disconnect_Click(object sender, RoutedEventArgs e)
        {
            connection.OnDisconnect();
        }

        //private async void remember_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (string.IsNullOrEmpty(tAccount.Text) || string.IsNullOrEmpty(tPwd.Password))
        //    {
        //        //ShowMessage("帳號密碼都要輸入的啦! 請重發, 謝謝.");
        //        TelnetSocket.ShowMessage(loader.GetString("plsreinput"));
        //        rememberAcPd.IsChecked = false;
        //        return;
        //    }
        //    bool isYes = true;
        //    Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
        //    if (roamingSettings.Values.ContainsKey("telnetAccount") || roamingSettings.Values.ContainsKey("telnetPassword"))
        //    {
        //        //confirm overwrite
        //        //isYes = await confirmDialog("已經存在帳號: " + roamingSettings.Values["telnetAccount"]
        //        //    + ", 要覆蓋嗎?");
        //        isYes = await TelnetSocket.confirmDialog(loader.GetString("existaccount") + roamingSettings.Values["telnetAccount"]
        //            + loader.GetString("needoverwrite"));
        //    }
        //    if (isYes)
        //    {
        //        roamingSettings.Values["telnetAccount"] = tAccount.Text;
        //        roamingSettings.Values["telnetPassword"] = tPwd.Password;
        //        roamingSettings.Values["telnetAddress"] = tIP.Text;
        //        roamingSettings.Values["telnetPort"] = tPort.Text;
        //        clearSaved.IsEnabled = true;
        //        //ShowMessage("位址, 帳號, 密碼已存");
        //        TelnetSocket.ShowMessage(loader.GetString("itsaved"));
        //    }
        //    else
        //    { //ShowMessage("位址, 帳號, 密碼未存入"); 
        //        TelnetSocket.ShowMessage(loader.GetString("itdoesntsaved"));
        //    }
        //}

        //private async void clearSaved_Click(object sender, RoutedEventArgs e)
        //{
        //    bool isYes = true;
        //    Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
        //    if (roamingSettings.Values.ContainsKey("telnetAccount") && roamingSettings.Values.ContainsKey("telnetPassword"))
        //    {
        //        //isYes = await confirmDialog("要刪除帳號: " + roamingSettings.Values["telnetAccount"]
        //        //    + " 嗎?");
        //        isYes = await TelnetSocket.confirmDialog(loader.GetString("todeleteacct") + roamingSettings.Values["telnetAccount"]
        //            + loader.GetString("yesornot"));
        //        if (isYes)
        //        {
        //            roamingSettings.Values.Remove("telnetAccount");
        //            roamingSettings.Values.Remove("telnetPassword");
        //            roamingSettings.Values.Remove("telnetAddress");
        //            roamingSettings.Values.Remove("telnetPort");
        //            clearSaved.IsEnabled = false;
        //        }
        //        else
        //        { //ShowMessage("帳號密碼未刪除");
        //            TelnetSocket.ShowMessage(loader.GetString("notdeleted"));
        //        }
        //    }
        //    else
        //    {
        //        //ShowMessage("找不到已儲存的帳號密碼.");
        //        TelnetSocket.ShowMessage(loader.GetString("cantfind"));
        //    }
        //}

        private void goTelnetPage(object sender, RoutedEventArgs e)
        {
            if (connection.IsConnected)
            {
                this.Frame.Navigate(typeof(TelnetPage));
            }
            else
            {
                //TelnetSocket.ShowMessage("please connect first");
                TelnetConnect.ShowMessage(loader.GetString("plsConn"));
            }
        }

        private async void buy_Click(object sender, RoutedEventArgs e)
        {
            //StorageFolder proxyDataFolder = await Package.Current.InstalledLocation.GetFolderAsync("ApiData");
            //StorageFile proxyFile = await proxyDataFolder.GetFileAsync("WindowsStoreProxy.xml");
            //await CurrentAppSimulator.ReloadSimulatorAsync(proxyFile);
            //ListingInformation listing = await CurrentAppSimulator.LoadListingInformationAsync();
            //var product1 = listing.ProductListings["support1USD"];
            //LicenseInformation licenseInfo = CurrentAppSimulator.LicenseInformation;
            //var productionLicense = licenseInfo.ProductLicenses["support1USD"];
            LicenseInformation licenseInfo = CurrentApp.LicenseInformation;
            if (licenseInfo.IsTrial)
            {
                //the customer doesn't buy this app
                try
                {
                    //await CurrentAppSimulator.RequestAppPurchaseAsync(true);
                    await CurrentApp.RequestAppPurchaseAsync(true);
                    if (!licenseInfo.IsTrial)
                    { //ShowMessage("感謝你的購買");
                        TelnetConnect.ShowMessage(loader.GetString("thanksforbuy"));
                    }
                }
                catch (Exception)
                { //ShowMessage("購買失敗");
                    TelnetConnect.ShowMessage(loader.GetString("buyfailed"));
                }
            }
            else
            {
                //ShowMessage("感謝, 你已經買過了.");
                TelnetConnect.ShowMessage(loader.GetString("alreadybuy"));
            }
        }

        private void PTT_Checked(object sender, RoutedEventArgs e)
        {
            if (tIP != null)
            {
                if (!tIP.Text.Contains("ptt"))
                {
                    TelnetConnect.ShowMessage("勾選PTT可能會造成非PTT站台顯示不正常, 請斟酌使用.");
                }
            }
            PTTDisplay.PTTMode = true;
        }

        private void PTT_Unchecked(object sender, RoutedEventArgs e)
        {
            PTTDisplay.PTTMode = false;
        }

        private void encode_Checked(object sender, RoutedEventArgs e)
        {
            if (isBig5.IsChecked == true)
            {
                Big5Util.isBig5Encode = true;
            }
            else
            {
                Big5Util.isBig5Encode = false;
            }
        }

        //private void ad_ErrorOccurred(object sender, Microsoft.Advertising.Mobile.Common.AdErrorEventArgs e)
        //{
        //    Debug.WriteLine(e.Error.Message);
        //}
    }
}
