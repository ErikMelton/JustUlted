using jabber.client;
using jabber.connection;
using JustUlted.Region;
using JustUltedProj.GUIElements;
using JustUltedProj.Logic;
using JustUltedProj.Logic.JSON;
using JustUltedProj.Logic.SQLite;
using PVPNetConnect.RiotObjects.Platform.Clientfacade.Domain;
using PVPNetConnect.RiotObjects.Platform.Game;
using PVPNetConnect.RiotObjects.Platform.Login;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace JustUltedProj.Windows
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public static BaseRegion region;
        internal ArrayList newsList;
        private bool loaded = false;

        public LoginPage()
        {
            InitializeComponent();
            SetupProperties();
           // GetNews(region);
            Client.Region = region;
            Client.SQLiteDatabase = new SQLite.SQLiteConnection(Path.Combine(Client.ExecutingDirectory, Client.sqlite));
            Client.Champions = (from s in Client.SQLiteDatabase.Table<champions>() orderby s.name select s).ToList();
            foreach (champions c in Client.Champions)
            {
                Console.WriteLine(Client.ExecutingDirectory);
                Console.Write(c.iconPath);
                var Source = new Uri(Path.Combine(Client.ExecutingDirectory, "Assets", "champions", c.iconPath), UriKind.Absolute);
                c.icon = new BitmapImage(Source);
                Console.WriteLine("Requesting : " + c.name + " champ");
                Champions.InsertExtraChampData(c);
            }
            Client.ChampionSkins = (from s in Client.SQLiteDatabase.Table<championSkins>() orderby s.name select s).ToList();
            Client.ChampionAbilities = (from s in Client.SQLiteDatabase.Table<championAbilities>() orderby s.name select s).ToList();
            Client.SearchTags = (from s in Client.SQLiteDatabase.Table<championSearchTags>() orderby s.id select s).ToList();
            Client.Keybinds = (from s in Client.SQLiteDatabase.Table<keybindingEvents>() orderby s.id select s).ToList();
            Client.Items = Items.PopulateItems();
            Client.Masteries = Masteries.PopulateMasteries();
            Client.Runes = Runes.PopulateRunes();

        }

        private void SetupProperties()
        {
            region = BaseRegion.GetRegion(Properties.Settings.Default.Region);
            if (region == null)
            {
                region = new NA();
            }
            regionComboBox.SelectedValue = Properties.Settings.Default.Region;
            usernameBox.Text = Properties.Settings.Default.Username;
            passwordBox.Password = Properties.Settings.Default.Password;
            usernameCheck.IsChecked = Properties.Settings.Default.RememberingUsername;
            passwordCheck.IsChecked = Properties.Settings.Default.RememberingPassword;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                var comboBox = sender as ComboBox;
                string tempRegion = (String)comboBox.SelectedValue;
                Console.WriteLine("Region is " + tempRegion);
                region = BaseRegion.GetRegion(tempRegion);

                Properties.Settings.Default.Region = tempRegion;
                Properties.Settings.Default.Save();
                ConfigurationManager.RefreshSection("userSettings");
            }
            loaded = true;
        }

        private void GetNews(BaseRegion region)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                string newsJSON = "";
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        newsJSON = client.DownloadString(region.NewsAddress);
                    }
                    catch (NullReferenceException e)
                    {
                        Debug.Print(e.ToString());
                    }
                }
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Dictionary<string, object> deserializedJSON = serializer.Deserialize<Dictionary<string, object>>(newsJSON);
                try
                {
                    newsList = deserializedJSON["news"] as ArrayList;
                    ArrayList promoList = deserializedJSON["promos"] as ArrayList;
                    foreach (Dictionary<string, object> objectPromo in promoList)
                    {
                        newsList.Add(objectPromo);
                    }
                }
                catch (NullReferenceException e)
                {
                    Debug.Print(e.ToString());
                }
            };

            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                ParseNews();
            };

            worker.RunWorkerAsync();
        }

        private void ParseNews()
        {
            if (newsList == null)
                return;
            if (newsList.Count <= 0)
                return;
            foreach (Dictionary<string, object> pair in newsList)
            {
                NewsItem item = new NewsItem();
                item.Margin = new Thickness(0, 5, 0, 5);
                foreach (KeyValuePair<string, object> kvPair in pair)
                {
                    if (kvPair.Key == "title")
                    {
                        item.NewsTitle.Content = kvPair.Value;
                    }
                    if (kvPair.Key == "description" || kvPair.Key == "promoText")
                    {
                        item.DescriptionLabel.Text = (string)kvPair.Value;
                    }
                    if (kvPair.Key == "thumbUrl")
                    {
                        BitmapImage promoImage = new BitmapImage();
                        promoImage.BeginInit();
                        promoImage.UriSource = new Uri((String)kvPair.Value, UriKind.RelativeOrAbsolute);
                        promoImage.CacheOption = BitmapCacheOption.OnLoad;
                        promoImage.EndInit();
                        item.PromoImage.Source = promoImage;
                    }
                    if (kvPair.Key == "linkUrl")
                    {
                        item.Tag = (string)kvPair.Value;
                    }
                }
                NewsListView.Items.Add(item);
            }
        }

        private void usernameCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberingUsername = true;
            Properties.Settings.Default.Save();
            ConfigurationManager.RefreshSection("userSettings");
        }

        private void usernameCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberingUsername = false;
            Properties.Settings.Default.Save();
            ConfigurationManager.RefreshSection("userSettings");
        }

        private void passwordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberingPassword = true;
            Properties.Settings.Default.Save();
            ConfigurationManager.RefreshSection("userSettings");
        }

        private void passwordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberingPassword = false;
            Properties.Settings.Default.Save();
            ConfigurationManager.RefreshSection("userSettings");
        }

        private async void loginButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Username = "";
            Properties.Settings.Default.Password = "";
            Properties.Settings.Default.Save();
            ConfigurationManager.RefreshSection("userSettings");

            if (Properties.Settings.Default.RememberingUsername)
            {
                Properties.Settings.Default.Username = usernameBox.Text.ToString();
                Properties.Settings.Default.Save();
                ConfigurationManager.RefreshSection("userSettings");
            }
            if (Properties.Settings.Default.RememberingPassword)
            {
                Properties.Settings.Default.Password = passwordBox.Password.ToString();
                Properties.Settings.Default.Save();
                ConfigurationManager.RefreshSection("userSettings");
            }

            Client.PVPNet.OnError += PVPNet_OnError;
            Client.PVPNet.OnLogin += PVPNet_OnLogin;
            Client.PVPNet.OnMessageReceived += Client.OnMessageReceived;
            Client.Region = region;
            
            Client.Version = "4.21.14";
           
            Dictionary<String, String> settings = region.Location.LeagueSettingsReader();
            //            Client.PVPNet.Connect(usernameBox.Text, passwordBox.Password, region.PVPRegion, Client.Version, true, settings["host"], settings["lq_uri"], region.Locale);
            Client.PVPNet.Connect(usernameBox.Text, passwordBox.Password, region.PVPRegion, Client.Version, true, settings["host"], settings["lq_uri"], region.Locale);
        }

        private void PVPNet_OnLogin(object sender, string username, string ipAddress)
        {
            Client.PVPNet.GetLoginDataPacketForUser(GotLoginPacket);
        }

        private void PVPNet_OnError(object sender, PVPNetConnect.Error error)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                Console.WriteLine(error.Message);
            }));
            Client.PVPNet.OnMessageReceived -= Client.OnMessageReceived;
            Client.PVPNet.OnError -= PVPNet_OnError;
            Client.PVPNet.OnLogin -= PVPNet_OnLogin;
        }

#pragma warning disable 4014 //Code does not need to be awaited
        private async void GotLoginPacket(LoginDataPacket packet)
        {
            if (packet.AllSummonerData == null)
            {
                //Just Created Account, need to put logic here.
                Client.done = false;
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                {
                    CreateSummonerNameOverlay createSummoner = new CreateSummonerNameOverlay();
                    Client.OverlayContainer.Content = createSummoner.Content;
                    Client.OverlayContainer.Visibility = Visibility.Visible;
                }));
                while (!Client.done) ;
                Client.PVPNet.Connect(usernameBox.Text, passwordBox.Password, Client.Region.PVPRegion, Client.Version);
                return;
            }
            Client.LoginPacket = packet;
            if (packet.AllSummonerData.Summoner.ProfileIconId == -1)
            {
                Client.done = false;
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                {
                    Client.OverlayContainer.Content = new ChooseProfilePicturePage().Content;
                    Client.OverlayContainer.Visibility = Visibility.Visible;
                }));
                while (!Client.done) ;
                Client.PVPNet.Connect(usernameBox.Text, passwordBox.Password, Client.Region.PVPRegion, Client.Version);
                return;
            }
            Client.PlayerChampions = await Client.PVPNet.GetAvailableChampions();
            Client.PVPNet.OnError -= PVPNet_OnError;
            Client.GameConfigs = packet.GameTypeConfigs;
            Client.PVPNet.Subscribe("bc", packet.AllSummonerData.Summoner.AcctId);
            Client.PVPNet.Subscribe("cn", packet.AllSummonerData.Summoner.AcctId);
            Client.PVPNet.Subscribe("gn", packet.AllSummonerData.Summoner.AcctId);
            Client.IsLoggedIn = true;

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(async () =>
            {
                AuthenticationCredentials newCredentials = new AuthenticationCredentials();
                newCredentials.Username = usernameBox.Text;
                newCredentials.Password = passwordBox.Password;
                newCredentials.ClientVersion = Client.Version;
                newCredentials.IpAddress = GetNewIpAddress();
                newCredentials.Locale = Client.Region.Locale;
                newCredentials.Domain = "lolclient.lol.riotgames.com";

                Session login = await Client.PVPNet.Login(newCredentials);
                Client.PlayerSession = login;

                //Setup chat
                Client.ChatClient.AutoReconnect = 30;
                Client.ChatClient.KeepAlive = 10;
                Client.ChatClient.NetworkHost = "chat." + Client.Region.ChatName + ".lol.riotgames.com";
                Client.ChatClient.Port = 5223;
                Client.ChatClient.Server = "pvp.net";
                Client.ChatClient.SSL = true;
                Client.ChatClient.User = usernameBox.Text;
                Client.ChatClient.Password = "AIR_" + passwordBox.Password;
                Client.ChatClient.OnInvalidCertificate += Client.ChatClient_OnInvalidCertificate;
                Client.ChatClient.OnMessage += Client.ChatClient_OnMessage;
                Client.ChatClient.OnPresence += Client.ChatClient_OnPresence;
                Client.ChatClient.Connect();

                Client.RostManager = new RosterManager();
                Client.RostManager.Stream = Client.ChatClient;
                Client.RostManager.AutoSubscribe = true;
                Client.RostManager.AutoAllow = jabber.client.AutoSubscriptionHanding.AllowAll;
                Client.RostManager.OnRosterItem += Client.RostManager_OnRosterItem;
                Client.RostManager.OnRosterEnd += new bedrock.ObjectHandler(Client.ChatClientConnect);

                Client.PresManager = new PresenceManager();
                Client.PresManager.Stream = Client.ChatClient;
                //Client.PresManager.OnPrimarySessionChange += Client.PresManager_OnPrimarySessionChange;

                Client.ConfManager = new ConferenceManager();
                Client.ConfManager.Stream = Client.ChatClient;
                Client.Log("Connected and logged in as " + Client.ChatClient.User);

                //Gather data and convert it that way that it does not cause errors
                PlatformGameLifecycleDTO data = Client.LoginPacket.ReconnectInfo;

                if (data != null && data.Game != null)
                {
                    Client.CurrentGame = data.PlayerCredentials;
                    Client.SwitchPage(new InGame());
                }
                else
                    Client.SwitchPage(new MainPage());
            }));
        }

        public static string GetNewIpAddress()
        {
            StringBuilder sb = new StringBuilder();

            WebRequest con = WebRequest.Create("http://ll.leagueoflegends.com/services/connection_info");
            WebResponse response = con.GetResponse();

            int c;
            while ((c = response.GetResponseStream().ReadByte()) != -1)
                sb.Append((char)c);

            con.Abort();

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, string> deserializedJSON = serializer.Deserialize<Dictionary<string, string>>(sb.ToString());

            return deserializedJSON["ip_address"];
        }

        public void SetPage(Page page)
        {
            FakePage p = new FakePage();
            p.getNavigationService().Navigate(page);
        }
    }
}
