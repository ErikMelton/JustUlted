using System.Collections.Generic;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using JustUltedProj.Logic;
using JustUltedProj.Logic.JSON;
using JustUltedProj.Logic.SQLite;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Linq;
using System.Windows.Navigation;
using RAFlibPlus;
using ComponentAce.Compression.Libs.zlib;
using Microsoft.Win32;
using JustUltedProj.Logic.Patcher;

namespace JustUltedProj.Windows
{
    /// <summary>
    /// Interaction logic for PatcherPage.xaml
    /// </summary>
    public partial class PatcherPage : Page
    {
        internal static bool LoLDataIsUpToDate = false;
        internal static string LatestLolDataVersion = "";
        internal static string LolDataVersion = "";
     
        public PatcherPage()
        {
            InitializeComponent();
            GetDev();
            StartPatcher();
            Client.Log("JustUlted Started Up Successfully");
        }

        private void GetDev()
        {
            Welcome.Text = "Welcome Kovu";
            Welcome.Visibility = Visibility.Visible;
        }

        private void DevSkip_Click(object sender, RoutedEventArgs e)
        {
            Client.Log("Swiched to LoginPage with DevSkip");
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Client.SwitchPage(new LoginPage());
        }

        private void StartPatcher()
        {
            try
            {

                Thread bgThead = new Thread(() =>
                {
                    LogTextBox("Starting Patcher");

                    WebClient client = new WebClient();
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadDDragon);
                    client.DownloadProgressChanged += (o, e) =>
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                        {
                            double bytesIn = double.Parse(e.BytesReceived.ToString());
                            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                            double percentage = bytesIn / totalBytes * 100;
                            CurrentProgressLabel.Content = "Downloaded " + e.BytesReceived + " of " + e.TotalBytesToReceive;
                            CurrentProgressBar.Value = int.Parse(Math.Truncate(percentage).ToString());
                        }));
                    };

                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                    {
                        TotalProgressLabel.Content = "20%";
                        TotalProgessBar.Value = 20;
                    }));

                    #region DDragon

                    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                    if (!Directory.Exists(Path.Combine(Client.ExecutingDirectory, "Assets")))
                    {
                        Directory.CreateDirectory(Path.Combine(Client.ExecutingDirectory, "Assets"));
                    }
                    if (!File.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_DDRagon")))
                    {
                        var VersionLOL = File.Create(Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_DDRagon"));
                        VersionLOL.Write(encoding.GetBytes("0.0.0"), 0, encoding.GetBytes("0.0.0").Length);

                        VersionLOL.Close();
                    }


                    RiotPatcher patcher = new RiotPatcher();
                    string DDragonDownloadURL = patcher.GetDragon();
                    LogTextBox("DataDragon Version: " + patcher.DDragonVersion);
                    string DDragonVersion = File.ReadAllText(Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_DDragon"));
                    LogTextBox("Current DataDragon Version: " + DDragonVersion);

                    Client.Version = DDragonVersion;
                    Client.Log("DDragon Version (LOL Version) = " + DDragonVersion);

                    LogTextBox("Client Version: " + Client.Version);

                    if (patcher.DDragonVersion != DDragonVersion)
                    {
                        try
                        {
                            if (!Directory.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "temp")))
                            {
                                Directory.CreateDirectory(Path.Combine(Client.ExecutingDirectory, "Assets", "temp"));
                            }

                            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                            {
                                CurrentProgressLabel.Content = "Downloading DataDragon";
                            }));
                            client.DownloadFile(DDragonDownloadURL, Path.Combine(Client.ExecutingDirectory, "Assets", "dragontail-" + patcher.DDragonVersion + ".tgz"));


                            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                            {
                                CurrentProgressLabel.Content = "Extracting DataDragon";
                            }));

                            Stream inStream = File.OpenRead(Path.Combine(Client.ExecutingDirectory, "Assets", "dragontail-" + patcher.DDragonVersion + ".tgz"));

                            using (GZipInputStream gzipStream = new GZipInputStream(inStream))
                            {
                                TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                                tarArchive.ExtractContents(Path.Combine(Client.ExecutingDirectory, "Assets", "temp"));
                                //tarArchive.Close();
                                tarArchive = null;
                            }
                            inStream.Close();

                            Copy(Path.Combine(Client.ExecutingDirectory, "Assets", "temp", patcher.DDragonVersion, "data"), Path.Combine(Client.ExecutingDirectory, "Assets", "data"));
                            Copy(Path.Combine(Client.ExecutingDirectory, "Assets", "temp", patcher.DDragonVersion, "img"), Path.Combine(Client.ExecutingDirectory, "Assets"));
                            DeleteDirectoryRecursive(Path.Combine(Client.ExecutingDirectory, "Assets", "temp"));

                            var VersionDDragon = File.Create(Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_DDRagon"));
                            VersionDDragon.Write(encoding.GetBytes(patcher.DDragonVersion), 0, encoding.GetBytes(patcher.DDragonVersion).Length);

                            Client.Version = DDragonVersion;
                            VersionDDragon.Close();
                        }
                        catch
                        {
                            Client.Log("Probably updated version number without actually uploading the files. Thanks riot.");
                        }
                    }

                    #endregion DDragon

                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                    {
                        TotalProgressLabel.Content = "40%";
                        TotalProgessBar.Value = 40;
                    }));

                    // Try get LoL path from registry

                    //A string that looks like C:\Riot Games\League of Legends\
                    string lolRootPath = GetLolRootPath();

                    #region lol_air_client

                    if (!File.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR")))
                    {
                        var VersionAIR = File.Create(Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR"));
                        VersionAIR.Write(encoding.GetBytes("0.0.0.0"), 0, encoding.GetBytes("0.0.0.0").Length);
                        VersionAIR.Close();
                    }

                    string LatestAIR = patcher.GetLatestAir();
                    LogTextBox("Air Assets Version: " + LatestAIR);
                    string AirVersion = File.ReadAllText(Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR"));
                    LogTextBox("Current Air Assets Version: " + AirVersion);
                    WebClient UpdateClient = new WebClient();
                    string Release = UpdateClient.DownloadString("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/releaselisting_NA");
                    string[] LatestVersion = Release.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                    var vers = LatestVersion[0];
                    if (AirVersion != LatestVersion[0])
                    {
                        //Download Air Assists from riot
                        try
                        {
                            string Package = UpdateClient.DownloadString("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/" + LatestVersion[0] + "/packages/files/packagemanifest");
                            GetAllPngs(Package);
                            if (File.Exists(Path.Combine(Client.ExecutingDirectory, "gameStats_en_US.sqlite")))
                                File.Delete(Path.Combine(Client.ExecutingDirectory, "gameStats_en_US.sqlite"));
                            string[] x = Package.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                            string locationv = LatestVersion[0];
                            foreach(string m in x)
                            {
                                if (m.Contains("/files/assets/data/gameStats/gameStats_en_US.sqlite"))
                                {
                                    string l = m.Split(',')[0];
                                    locationv = l.Replace("/projects/lol_air_client/releases/", "").Replace("/files/assets/data/gameStats/gameStats_en_US.sqlite", "");
                                }
                            }
                            UpdateClient.DownloadFile(new Uri("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/" + locationv + "/files/assets/data/gameStats/gameStats_en_US.sqlite"), Path.Combine(Client.ExecutingDirectory, "gameStats_en_US.sqlite"));
                            
                            if (File.Exists(System.IO.Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR")))
                                File.Delete(System.IO.Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR"));
                            var file = File.Create(System.IO.Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR"));
                            file.Write(encoding.GetBytes(LatestVersion[0]), 0, encoding.GetBytes(LatestVersion[0]).Length);
                            file.Close();
                        }
                        catch
                        {
                            Client.Log("Probably riot updated air client version without actually releasing the latest version. Why riot, why? Trying to download last version.");
                            
                            LatestVersion = Release.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                            Console.WriteLine("XXXXXXX" + "http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/" + LatestVersion[0] + "/packages/files/packagemanifest");
                            string Package = UpdateClient.DownloadString("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/" + LatestVersion[0] + "/packages/files/packagemanifest");
                            GetAllPngs(Package);
                            if (File.Exists(Path.Combine(Client.ExecutingDirectory, "gameStats_en_US.sqlite")))
                            { 

                                File.Delete(Path.Combine(Client.ExecutingDirectory, "gameStats_en_US.sqlite"));
                            }
                            string locationv = LatestVersion[0];
                            string[] x = Package.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                            string locationm = LatestVersion[0];
                            foreach (string m in x)
                            {
                                if (m.Contains("/files/assets/data/gameStats/gameStats_en_US.sqlite"))
                                {
                                    string l = m.Split(',')[0];
                                    locationm = l.Replace("/projects/lol_air_client/releases/", "").Replace("/files/assets/data/gameStats/gameStats_en_US.sqlite", "");
                                }
                            }
                            UpdateClient.DownloadFile(new Uri("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/" + locationv + "/files/assets/data/gameStats/gameStats_en_US.sqlite"), Path.Combine(Client.ExecutingDirectory, "gameStats_en_US.sqlite"));

                            if (File.Exists(System.IO.Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR")))
                                File.Delete(System.IO.Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR"));
                            var file = File.Create(System.IO.Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR"));
                            file.Write(encoding.GetBytes(LatestVersion[1]), 0, encoding.GetBytes(LatestVersion[1]).Length);
                            file.Close();
                        }
                    }

                    if (AirVersion != LatestAIR)
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                        {
                            PlayButton.IsEnabled = true;
                            CurrentProgressLabel.Content = "Retrieving Air Assets";
                        }));
                    }

                    #endregion lol_air_client


                    //string GameVersion = File.ReadAllText(Path.Combine(Client.ExecutingDirectory, "RADS", "VERSION_LOL"));
                    #region lol_game_client
                    LogTextBox("Trying to detect League of Legends GameClient");
                    LogTextBox("League of Legends is located at: " + lolRootPath);
                    //RADS\solutions\lol_game_client_sln\releases
                    var GameLocation = Path.Combine(lolRootPath, "RADS", "solutions", "lol_game_client_sln", "releases");

                    string LolVersion2 = new WebClient().DownloadString("http://l3cdn.riotgames.com/releases/live/projects/lol_game_client/releases/releaselisting_NA");
                    string LolVersion = new WebClient().DownloadString("http://l3cdn.riotgames.com/releases/live/solutions/lol_game_client_sln/releases/releaselisting_NA");
                    string GameClientSln = LolVersion.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0];
                    string GameClient = LolVersion2.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0];
                    LogTextBox("Latest League of Legends GameClient: " + GameClientSln);
                    LogTextBox("Checking if League of Legends is Up-To-Date");

                    string LolLauncherVersion = new WebClient().DownloadString("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/releaselisting_NA");
                    string LauncherVersion = LolLauncherVersion.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0];
                    if (Directory.Exists(Path.Combine(GameLocation, GameClientSln)))
                    {
                        LogTextBox("League of Legends is Up-To-Date");
                        Client.LOLCLIENTVERSION = LolVersion2;
                        Client.Location = Path.Combine(lolRootPath, "RADS", "solutions", "lol_game_client_sln", "releases", GameClientSln, "deploy");
                        Client.LoLLauncherLocation = Path.Combine(lolRootPath, "RADS", "projects", "lol_air_client", "releases", LauncherVersion, "deploy");
                        Client.RootLocation = lolRootPath;
                    }
                    else
                    {
                        LogTextBox("League of Legends is not Up-To-Date. Please Update League Of Legends");
                        return;
                    }
                    #endregion lol_game_client

                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                    {
                        TotalProgressLabel.Content = "100%";
                        TotalProgessBar.Value = 100;
                        CurrentProgressLabel.Content = "Finished Patching";
                        CurrentStatusLabel.Content = "Ready To Play";
                        PlayButton.IsEnabled = true;
                    }));

                    LogTextBox("LegendaryClient Has Finished Patching");

                });

                bgThead.Start();

                Client.Log("LegendaryClient Has Finished Patching");
            }
            catch (Exception e)
            {
                Client.Log(e.Message + " - in PatcherPage updating progress.");
            }
        }

        private string GetLolRootPath()
        {
            var possiblePaths = new List<Tuple<string, string>>  
            {
                new Tuple<string, string>(@"HKEY_CURRENT_USER\Software\Classes\VirtualStore\MACHINE\SOFTWARE\RIOT GAMES", "Path"),
                new Tuple<string, string>(@"HKEY_CURRENT_USER\Software\Classes\VirtualStore\MACHINE\SOFTWARE\Wow6432Node\RIOT GAMES", "Path"),
                new Tuple<string, string>(@"HKEY_CURRENT_USER\Software\RIOT GAMES", "Path"),
                new Tuple<string, string>(@"HKEY_CURRENT_USER\Software\Wow6432Node\Riot Games", "Path"),
                new Tuple<string, string>(@"HKEY_LOCAL_MACHINE\Software\Riot Games\League Of Legends", "Path"),
                new Tuple<string, string>(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Riot Games", "Path"),
                new Tuple<string, string>(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Riot Games\League Of Legends", "Path"),
                // Yes, a f*ckin whitespace after "Riot Games"..
                new Tuple<string, string>(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Riot Games \League Of Legends", "Path"),
            };
            foreach (var tuple in possiblePaths)
            {
                var path = tuple.Item1;
                var valueName = tuple.Item2;
                try
                {
                    var value = Microsoft.Win32.Registry.GetValue(path, valueName, string.Empty);
                    if (value != null && value.ToString() != string.Empty)
                    {
                        return value.ToString();
                    }
                }
                catch { }
            }

            OpenFileDialog FindLeagueDialog = new OpenFileDialog();

            if (!Directory.Exists(Path.Combine("C:\\", "Riot Games", "League of Legends")))
            {
                FindLeagueDialog.InitialDirectory = Path.Combine("C:\\", "Program Files (x86)", "GarenaLoL", "GameData", "Apps", "LoL");
            }
            else
            {
                FindLeagueDialog.InitialDirectory = Path.Combine("C:\\", "Riot Games", "League of Legends");
            }
            FindLeagueDialog.DefaultExt = ".exe";
            FindLeagueDialog.Filter = "League of Legends Launcher|lol.launcher*.exe|Garena Launcher|lol.exe";

            Nullable<bool> result = FindLeagueDialog.ShowDialog();
            if (result == true)
            {
                return FindLeagueDialog.FileName.Replace("lol.launcher.exe", "").Replace("lol.launcher.admin.exe", "");
            }
            else
                return string.Empty;
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                CurrentProgressBar.Value = int.Parse(Math.Truncate(percentage).ToString());
                CurrentProgressLabel.Content = "Now downloading LegendaryClient";
            }));
        }

        void client_DownloadDDragon(object sender, AsyncCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                CurrentProgressLabel.Content = "Download Completed";
                LogTextBox("Finished Download");
                CurrentProgressBar.Value = 0;
            }));
        }

        void UpdateClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                TotalProgressLabel.Content = "100%";
                TotalProgessBar.Value = 100;
                CurrentProgressLabel.Content = "Finished Patching";
                CurrentStatusLabel.Content = "Ready To Play";
                PlayButton.IsEnabled = true;
            }));

            LogTextBox("JustUlted Has Finished Patching");
            Client.Log("JustUlted Has Finished Patching");
        }

        private void LogTextBox(string s)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                PatchTextBox.Text += "[" + DateTime.Now.ToShortTimeString() + "] " + s + Environment.NewLine;
                PatchTextBox.ScrollToEnd();
            }));
            Client.Log(s);
        }

        private void Copy(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);

            foreach (var directory in Directory.GetDirectories(sourceDir))
                Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }

        private string GetMd5()
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            FileInfo fi = null;
            FileStream stream = null;

            fi = new FileInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            stream = File.Open(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName, FileMode.Open, FileAccess.Read);

            md5.ComputeHash(stream);

            stream.Close();

            string rtrn = "";
            for (int i = 0; i < md5.Hash.Length; i++)
            {
                rtrn += (md5.Hash[i].ToString("x2"));
            }
            return rtrn.ToUpper();
        }

        private void DeleteDirectoryRecursive(string path)
        {
            foreach (var directory in Directory.GetDirectories(path))
            {
                DeleteDirectoryRecursive(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }
        public void WriteLatestVersion(string fileDirectory)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            string dDirectory = fileDirectory;
            DirectoryInfo dInfo = new DirectoryInfo(dDirectory);
            DirectoryInfo[] subdirs = null;
            try
            {
                subdirs = dInfo.GetDirectories();
            }
            catch { return; }
            string latestVersion = "0.0.1";
            foreach (DirectoryInfo info in subdirs)
            {
                latestVersion = info.Name;
            }
            var VersionLOL = File.Create(Path.Combine(Client.ExecutingDirectory, "RADS", "VERSION_LOL"));
            VersionLOL.Write(encoding.GetBytes(latestVersion), 0, encoding.GetBytes(latestVersion).Length);
            VersionLOL.Close();
            LolDataVersion = latestVersion;
        }

        public void CopyStream(System.IO.Stream input, System.IO.Stream output)
        {
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }

        public void uncompressFile(string inFile, string outFile)
        {
            try
            {
                int data = 0;
                int stopByte = -1;
                System.IO.FileStream outFileStream = new System.IO.FileStream(outFile, System.IO.FileMode.Create);
                ZInputStream inZStream = new ZInputStream(System.IO.File.Open(inFile, System.IO.FileMode.Open, System.IO.FileAccess.Read));
                while (stopByte != (data = inZStream.Read()))
                {
                    byte _dataByte = (byte)data;
                    outFileStream.WriteByte(_dataByte);
                }

                inZStream.Close();
                outFileStream.Close();
            }
            catch
            {
                Client.Log("Unable to find a file to uncompress");
            }

        }

        private void GetAllPngs(string PackageManifest)
        {
            string[] FileMetaData = PackageManifest.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Skip(1).ToArray();
            foreach (string s in FileMetaData)
            {
                if (String.IsNullOrEmpty(s))
                {
                    continue;
                }
                //Remove size and type metadata
                string Location = s.Split(',')[0];
                //Get save position
                string SavePlace = Location.Split(new string[] { "/files/" }, StringSplitOptions.None)[1];
                if (!Directory.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "champions")))
                    Directory.CreateDirectory(Path.Combine(Client.ExecutingDirectory, "Assets", "champions"));
                if (SavePlace.EndsWith(".jpg") || SavePlace.EndsWith(".png"))
                {
                    if (SavePlace.Contains("assets/images/champions/"))
                    {
                        using (WebClient newClient = new WebClient())
                        {
                            string SaveName = Location.Split(new string[] { "/champions/" }, StringSplitOptions.None)[1];
                            if (!File.Exists(Client.ExecutingDirectory + "\\Assets\\" + "champions\\" + SaveName))
                            {
                                LogTextBox("Downloading " + SaveName + " from http://l3cdn.riotgames.com");

                                newClient.DownloadFile("http://l3cdn.riotgames.com/releases/live" + Location, Path.Combine(Client.ExecutingDirectory, "Assets", "champions", SaveName));
                            }
                        }
                    }
                }
            }
        }
     }
}