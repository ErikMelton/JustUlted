using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using MahApps.Metro.Controls;
using System.Diagnostics;
using System.ComponentModel;
using System.Net;
using System.Web.Script.Serialization;
using System.Collections;
using JustUltedProj.GUIElements;
using System.Configuration;
using JsonConfig;
using System.Windows.Threading;
using JustUltedProj.Logic;
using RtmpSharp.IO;
using System.Threading;
using jabber.client;
using jabber.connection;
using JustUltedProj.Windows;
using JustUlted.Region;
using PVPNetConnect.RiotObjects.Platform.Clientfacade.Domain;
using PVPNetConnect.RiotObjects.Platform.Login;
using PVPNetConnect.RiotObjects.Platform.Game;
using System.IO;
using JustUltedProj.Logic.SQLite;
using JustUltedProj.Logic.JSON;

namespace JustUltedProj
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            var ExecutingDirectory = System.IO.Directory.GetParent(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

            Client.ExecutingDirectory = ExecutingDirectory.ToString().Replace("file:\\", "");

            InitializeComponent();
            Client.PVPNet = new PVPNetConnect.PVPNetConnection();
            Client.PVPNet.KeepDelegatesOnLogout = false;
            Client.ChatClient = new JabberClient();
            Client.chatPage = new ChatPage();
            ChatContainer.Content = Client.chatPage.Content;
            Client.notificationPage = new NotificationPage();
            NotificationContainer.Content = Client.notificationPage.Content;
            Client.statusPage = new StatusPage();
            StatusContainer.Content = Client.statusPage.Content;
            NotificationOverlayContainer.Content = new FakePage().Content;
            
            Grid NotificationTempGrid = null;
            foreach (var x in NotificationOverlayContainer.GetChildObjects())
            {
                if (x is Grid)
                {
                    NotificationTempGrid = x as Grid;
                }
            }
            
            Client.Pages = new List<Page>();
            Client.Container = Container;
            Client.BackgroundImage = BackImage;
            Client.SwitchPage(new PatcherPage());
        }
    }
}