﻿#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using LegendaryClient.Logic;
using LegendaryClient.Properties;
using SharpCompress.Common;
using SharpCompress.Reader;
using CheckBox = System.Windows.Controls.CheckBox;

#endregion

namespace LegendaryClient.Windows
{
    /// <summary>
    ///     Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage
    {
        #region DLL Stuff

        //private const int ENUM_CURRENT_SETTINGS = -1;

        //private const int ENUM_REGISTRY_SETTINGS = -2;

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(
            string deviceName, int modeNum, ref DEVMODE devMode);

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            //private const int CCHDEVICENAME = 0x20;
            //private const int CCHFORMNAME = 0x20;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)] public string dmDeviceName;

            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)] public string dmFormName;

            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        #endregion DLL Stuff

        private readonly List<string> Resolutions = new List<string>();

        private readonly Dictionary<string, WinThemes> list = new Dictionary<string, WinThemes>();
        private readonly Dictionary<WinThemes, string> list2 = new Dictionary<WinThemes, string>();
        private readonly Dictionary<string, string> list3 = new Dictionary<string, string>();
        private readonly MainWindow mainWindow;

        public SettingsPage(MainWindow window)
        {
            InitializeComponent();
            InsertDefaultValues();
            mainWindow = window;
            HudLink.Text = "";
            WarnExitCheckbox.IsChecked = Settings.Default.warnClose;
            StatsCheckbox.IsChecked = Settings.Default.GatherStatistics;
            ErrorCheckbox.IsChecked = Settings.Default.SendErrors;
            UseAsBackground.IsChecked = Settings.Default.UseAsBackgroundImage;
            AutoRecordCheckbox.IsChecked = Settings.Default.AutoRecordGames;

            #region AboutTextbox

            string user;
            try
            {
                user = Client.LoginPacket.AllSummonerData.Summoner.Name;
            }
            catch
            {
                user = "you the user";
            }
            AboutTextBox.Text =
                @"Copyright (c) 2013-2014, Eddy5641 (Eddy V)
All rights reserved.



Thanks to " + user + @". Using this client means the most to me. Thank you very much!

Big thanks to Snowl. Created the foundation of this custom client.
Thanks to all the people at #riotcontrol, especially raler (for providing PVPNetConnect).
Thanks to all the people who done the RTMPS work. Your help has been invaluable.

Thanks Riot for providing a pretty awesome game. It might not be perfect, but I have wasted many hours on it.

Thanks to all the people who have supported me.

Uses Data Dragon QA Interface & l3cdn.

This product is not endorsed, certified or otherwise approved in any way by Riot Games, Inc. or any of its affiliates.

External libraries:
Awesomium
jabber-net
MahApps.Metro
Rtmp-sharp
PVPNetConnect
SharpZipLib
sqlite
zlib

Donations are accepted at:
Press the donate button

Donations will be used in ways that support LegendaryClient. Examples are:
Domain name (LegendaryClient.ca|LegendaryClient.gg)
A code signing license (So you know that you are using LegendaryClient)

";

            #endregion AboutTextbox

            Addtheme("Dark Steel", "pack://application:,,,/LegendaryClient;component/Controls/Steel.xaml");
            Addtheme("Light Steel", "pack://application:,,,/LegendaryClient;component/Controls/Steel.xaml");
            Addtheme("Dark Blue", "pack://application:,,,/MahApps.Metro;component/Styles/Accents/Blue.xaml");
            Addtheme("Light Blue", "pack://application:,,,/MahApps.Metro;component/Styles/Accents/Blue.xaml");
            Addtheme("Dark Red", "pack://application:,,,/MahApps.Metro;component/Styles/Accents/Red.xaml");
            Addtheme("Light Red", "pack://application:,,,/MahApps.Metro;component/Styles/Accents/Red.xaml");
            Addtheme("Dark Green", "pack://application:,,,/MahApps.Metro;component/Styles/Accents/Green.xaml");
            Addtheme("Light Green", "pack://application:,,,/MahApps.Metro;component/Styles/Accents/Green.xaml");
            Addtheme("Dark Purple", "pack://application:,,,/MahApps.Metro;component/Styles/Accents/Purple.xaml");
            Addtheme("Light Purple", "pack://application:,,,/MahApps.Metro;component/Styles/Accents/Purple.xaml");

            UpdateStats();
        }

        /// <summary>
        ///     Gets the current league stats
        /// </summary>
        private void UpdateStats()
        {
            Dictionary<String, String> val =
                Path.Combine(Client.RootLocation, "Config", "game.cfg").LeagueSettingsReader();
            try
            {
                MusicVolumeSlider.Value = (Convert.ToInt32(val["MusicVolume"])*100);
                VoiceVolumeSlider.Value = (Convert.ToInt32(val["VoiceVolume"])*100);
                AnnouncerVolumeSlider.Value = (Convert.ToInt32(val["AnnouncerVolume"])*100);
            }
            catch (Exception e)
            {
                Client.Log("Unable to read game config files. Error message: " + e.Message, "ConfigError");
            }
        }

        public void Addtheme(string text, string value)
        {
            var theme = new WinThemes
            {
                Text = text,
                Value = value
            };
            ThemeBox.Items.Add(theme);
            list.Add(text, theme);
            list2.Add(theme, text);
            list3.Add(text, value);
        }

        public void InsertDefaultValues()
        {
            //Insert resolutions
            var vDevMode = new DEVMODE();
            int i = 0;

            while (EnumDisplaySettings(null, i, ref vDevMode))
            {
                if (!Resolutions.Contains(String.Format("{0}x{1}", vDevMode.dmPelsWidth, vDevMode.dmPelsHeight)) &&
                    vDevMode.dmPelsWidth >= 1000)
                {
                    Resolutions.Add(String.Format("{0}x{1}", vDevMode.dmPelsWidth, vDevMode.dmPelsHeight));
                    ResolutionComboBox.Items.Add(String.Format("{0}x{1}", vDevMode.dmPelsWidth, vDevMode.dmPelsHeight));
                }
                i++;
            }

            LoginImageBox.Text = Settings.Default.LoginPageImage;

            ResolutionComboBox.SelectedIndex = ResolutionComboBox.Items.Count - 1;
            WindowModeComboBox.SelectedIndex = 0;
        }

        private void warnExitCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox) sender;
            if (cb.IsChecked != null)
                Settings.Default.warnClose = (bool) cb.IsChecked;

            Settings.Default.Save();
        }

        private void StatsCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (StatsCheckbox.IsChecked != null)
                Settings.Default.GatherStatistics = (bool) StatsCheckbox.IsChecked;

            Settings.Default.Save();
        }

        private void ErrorCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (ErrorCheckbox.IsChecked != null)
                Settings.Default.SendErrors = (bool) ErrorCheckbox.IsChecked;

            Settings.Default.Save();
        }

        private void LoginImageBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Default.LoginPageImage = LoginImageBox.Text;

            if (UseAsBackground.IsChecked != null && (UseAsBackground.HasContent && (bool) UseAsBackground.IsChecked &&
                                                      File.Exists(Path.Combine(Client.ExecutingDirectory, "Assets",
                                                          "champions",
                                                          Settings.Default.LoginPageImage.Replace("\r\n", "")))))
                Client.BackgroundImage.Source =
                    new BitmapImage(
                        new Uri(
                            Path.Combine(Client.ExecutingDirectory, "Assets", "champions",
                                Settings.Default.LoginPageImage), UriKind.Absolute));
        }

        private void LoginImageBox_DropDownClosed(object sender, EventArgs e)
        {
            string temp = Settings.Default.LoginPageImage;
            LoginImageBox.Items.Clear();
            LoginImageBox.Text = temp;
            if (UseAsBackground.IsChecked != null && (UseAsBackground.HasContent && (bool) UseAsBackground.IsChecked &&
                                                      File.Exists(Path.Combine(Client.ExecutingDirectory, "Assets",
                                                          "champions",
                                                          Settings.Default.LoginPageImage.Replace("\r\n", "")))))
                Client.BackgroundImage.Source =
                    new BitmapImage(
                        new Uri(
                            Path.Combine(Client.ExecutingDirectory, "Assets", "champions",
                                Settings.Default.LoginPageImage), UriKind.Absolute));
        }

        private void LoginImageBox_DropDownOpened(object sender, EventArgs e)
        {
            foreach (
                string s in
                    Directory.EnumerateFiles(Path.Combine(Client.ExecutingDirectory, "Assets", "champions"), "*",
                        SearchOption.AllDirectories)
                        .Select(Path.GetFileName).Where(s => s.Contains("Splash")))
            {
                LoginImageBox.Items.Add(s);
            }
        }

        private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var winThemes = ThemeBox.SelectedItem as WinThemes;
            if (winThemes != null)
            {
                Settings.Default.DarkTheme = winThemes.Text.Contains("Dark");
                Settings.Default.Theme = (string) winThemes.Value;
            }

            mainWindow.ChangeTheme();
            Client.statusPage.Change();
            Client.FriendList.Change();
            Client.notificationPage.Change();
        }

        private void UseAsBackground_Changed(object sender, RoutedEventArgs e)
        {
            if (!UseAsBackground.HasContent)
                return;

            if (UseAsBackground.IsChecked == null)
                return;

            Settings.Default.UseAsBackgroundImage = (bool) UseAsBackground.IsChecked;
            if (Settings.Default.UseAsBackgroundImage)
            {
                if (!UseAsBackground.HasContent || !(bool) UseAsBackground.IsChecked ||
                    !File.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "champions",
                        Settings.Default.LoginPageImage.Replace("\r\n", ""))))
                    return;

                Client.BackgroundImage.Visibility = Visibility.Visible;
                Client.BackgroundImage.Source =
                    new BitmapImage(
                        new Uri(
                            Path.Combine(Client.ExecutingDirectory, "Assets", "champions",
                                Settings.Default.LoginPageImage), UriKind.Absolute));
            }
            else
                Client.BackgroundImage.Visibility = Visibility.Hidden;
        }

        /// <summary>
        ///     Easy hud beta. Add a hud to you league of legends via easyhud powered by leaguecraft.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //If you have any other hud websites, send them to me, I will try to support that website
            string x = HudLink.Text;
            if (!x.Contains("http://leaguecraft.com/uimods/"))
            {
                ResultTextbox.Content =
                    "Invalid Uri. Please try a different Uri. Be sure that the Uri is from LeagueCraft.com (Easy hud only works with Leaguecraft currently)";
                ResultTextbox.Visibility = Visibility.Visible;
            }
            x = x.Replace("http://leaguecraft.com/uimods/", "");
            string y = x.Split('-')[0];
            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFileAsync(new Uri("http://leaguecraft.com/uimods/download/?id=" + y),
                        Path.Combine(Client.ExecutingDirectory, "LCHudFile.zip"));
                    client.DownloadFileCompleted += (o, xm) =>
                    {
                        ResultTextbox.Content = "Hud downloaded. Extracting your hud";
                        ResultTextbox.Visibility = Visibility.Visible;
                        string final = Path.Combine(Client.Location, "DATA", "menu", "hud");
                        string[] files = Directory.GetFiles(final);
                        foreach (string file in files.Where(file => !file.EndsWith(".ini")))
                            File.Delete(Path.Combine(final, file));

                        ZipFile.ExtractToDirectory(Path.Combine(Client.ExecutingDirectory, "LCHudFile.zip"), final);

                        //Not to sure about this but is will make it so that no rar/zip files are in the dir
                        if (DoAsyncExtractLoop(final))
                        {
                            ResultTextbox.Content =
                                "Hud installed. Please try a custom game before you play an actual game.";
                            ResultTextbox.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            ResultTextbox.Content =
                                "Unable to install hud. Please check the link to see if it is a .dds file";
                            ResultTextbox.Visibility = Visibility.Visible;
                        }
                    };
                }
                catch
                {
                    ResultTextbox.Content =
                        "Unable to install hud. Please check the link or try running LegendaryClient as admin.";
                    ResultTextbox.Visibility = Visibility.Visible;
                }
            }
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            using (new WebClient())
            {
                try
                {
                    string final = Path.Combine(Client.Location, "DATA", "menu", "hud");
                    string[] files = Directory.GetFiles(final);
                    foreach (string file in files)
                    {
                        //All settings in this folder end with .ini so delete all other files that do not end it .ini
                        if (!file.EndsWith(".ini"))
                            File.Delete(Path.Combine(final, file));

                        ResultTextbox.Content = "Hud removed.";
                        ResultTextbox.Visibility = Visibility.Visible;
                    }
                }
                catch
                {
                    ResultTextbox.Content = "Unable to remove hud. Please delete manually";
                    ResultTextbox.Visibility = Visibility.Visible;
                }
            }
        }

        private bool DoAsyncExtractLoop(string dir)
        {
            try
            {
                foreach (string filesindir in Directory.GetFiles(dir).Where(filesindir => !filesindir.EndsWith(".ini")))
                {
                    //Try to extract the file
                    if (filesindir.EndsWith(".zip"))
                    {
                        ZipFile.ExtractToDirectory(Path.Combine(dir, filesindir), dir);
                        File.Delete(Path.Combine(dir, filesindir));
                    }
                        //Use SharpCompres to read .rar files
                    else if (filesindir.EndsWith(".rar"))
                    {
                        using (Stream stream = File.OpenRead(Path.Combine(dir, filesindir)))
                        {
                            IReader reader = ReaderFactory.Open(stream);
                            while (reader.MoveToNextEntry())
                                if (!reader.Entry.IsDirectory)
                                    reader.WriteEntryToDirectory(dir,
                                        ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                        }
                        File.Delete(Path.Combine(dir, filesindir));
                    }
                        //File has been extracted. No point of trying to extract it anymore
                    else if (filesindir.EndsWith(".dds"))
                        return true;
                    else if (filesindir.EndsWith(".tga"))
                        return false;
                    else if (filesindir.EndsWith(".exe"))
                        return false;
                }

                return DoAsyncExtractLoop(dir);
            }
            catch
            {
                //I expect that this will cause errors because the way it is made
                return false;
            }
        }

        private void AutoRecordCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox) sender;
            if (cb.IsChecked != null)
                Settings.Default.AutoRecordGames = (bool) cb.IsChecked;

            Settings.Default.Save();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Process.Start("https://pledgie.com/campaigns/27549");
        }

        private void ChampSelectVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Client.AmbientSoundPlayer.Volume = ChampSelectVolumeSlider.Value/100;
        }
    }

    public class WinThemes
    {
        public string Text { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}