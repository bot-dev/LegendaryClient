﻿#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LegendaryClient.Controls;
using LegendaryClient.Logic;
using LegendaryClient.Logic.SQLite;
using LegendaryClient.Properties;
using PVPNetConnect.RiotObjects.Platform.Harassment;
using PVPNetConnect.RiotObjects.Platform.Statistics;

#endregion

namespace LegendaryClient.Windows.Profile
{
    /// <summary>
    ///     Interaction logic for Overview.xaml
    /// </summary>
    public partial class Overview
    {
        private double AccId;
        private List<PlayerStatSummary> Summaries = new List<PlayerStatSummary>();

        public Overview()
        {
            InitializeComponent();
            Change();
        }

        public void Change()
        {
            var themeAccent = new ResourceDictionary
            {
                Source = new Uri(Settings.Default.Theme)
            };
            Resources.MergedDictionaries.Add(themeAccent);
        }

        public async void Update(double summonerId, double accountId)
        {
            AccId = accountId;
            LcdsResponseString totalKudos =
                await Client.PVPNet.CallKudos("{\"commandName\":\"TOTALS\",\"summonerId\": " + summonerId + "}");
            RenderKudos(totalKudos);
            ChampionStatInfo[] topChampions = await Client.PVPNet.RetrieveTopPlayedChampions(accountId, "CLASSIC");
            RenderTopPlayedChampions(topChampions);
            Client.PVPNet.RetrievePlayerStatsByAccountId(accountId, "3", GotPlayerStats);
        }

        public void RenderKudos(LcdsResponseString TotalKudos)
        {
            KudosListView.Items.Clear();
            TotalKudos.Value = TotalKudos.Value.Replace("{\"totals\":[0,", "").Replace("]}", "");
            string[] kudos = TotalKudos.Value.Split(',');
            var item = new KudosItem("Friendly", kudos[0]);
            KudosListView.Items.Add(item);
            item = new KudosItem("Helpful", kudos[1]);
            KudosListView.Items.Add(item);
            item = new KudosItem("Teamwork", kudos[2]);
            KudosListView.Items.Add(item);
            item = new KudosItem("Honorable Opponent", kudos[3]);
            KudosListView.Items.Add(item);
        }

        public void RenderTopPlayedChampions(ChampionStatInfo[] topChampions)
        {
            ViewAggregatedStatsButton.IsEnabled = false;
            TopChampionsListView.Items.Clear();
            if (!topChampions.Any())
                return;

            TopChampionsLabel.Content = "Top Champions (" + topChampions[0].TotalGamesPlayed + " Ranked Games)";
            foreach (ChampionStatInfo info in topChampions)
            {
                ViewAggregatedStatsButton.IsEnabled = true;
                if (!(Math.Abs(info.ChampionId) > 0))
                    continue;

                champions champion = champions.GetChampion(Convert.ToInt32(info.ChampionId));
                var player = new ChatPlayer
                {
                    LevelLabel = {Visibility = Visibility.Hidden},
                    PlayerName = {Content = champion.displayName},
                    PlayerStatus = {Content = info.TotalGamesPlayed + " games played"},
                    ProfileImage = {Source = champions.GetChampion(champion.id).icon},
                    Background = new SolidColorBrush(Color.FromArgb(102, 80, 80, 80)),
                    Height = 51.5,
                    Width = 278
                };
                TopChampionsListView.Items.Add(player);
            }
        }

        public void GotPlayerStats(PlayerLifetimeStats stats)
        {
            Summaries = new List<PlayerStatSummary>();
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                StatsComboBox.Items.Clear();
                StatsListView.Items.Clear();
                try
                {
                    foreach (
                        PlayerStatSummary x in
                            stats.PlayerStatSummaries.PlayerStatSummarySet.Where(x => x.AggregatedStats.Stats.Count > 0)
                        )
                    {
                        Summaries.Add(x);
                        string summaryString = x.PlayerStatSummaryTypeString;
                        summaryString =
                            string.Concat(
                                summaryString.Select(
                                    e => Char.IsUpper(e) ? " " + e : e.ToString(CultureInfo.InvariantCulture)))
                                .TrimStart(' ');
                        summaryString = summaryString.Replace("Odin", "Dominion");
                        summaryString = summaryString.Replace("x", "v");
                        StatsComboBox.Items.Add(summaryString);
                    }
                }
                catch
                {
                    Client.Log("Error when loading player stats.");
                }
                StatsComboBox.SelectedItem = "Ranked Solo5v5";
            }));
        }

        private void StatsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatsComboBox.SelectedIndex != -1)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                {
                    StatsListView.Items.Clear();
                    PlayerStatSummary gameMode = Summaries[StatsComboBox.SelectedIndex];
                    foreach (
                        KudosItem Item in gameMode.AggregatedStats.Stats.Select(stat => new ProfilePage.KeyValueItem
                        {
                            Key = Client.TitleCaseString(stat.StatType.Replace('_', ' ')),
                            Value = stat.Value.ToString(CultureInfo.InvariantCulture)
                        })
                            .Select(
                                item =>
                                    new KudosItem(item.Key.ToString(), item.Value.ToString())
                                    {
                                        MinWidth = gameMode.AggregatedStats.Stats.Count < 15 ? 972 : 962,
                                        MinHeight = 18
                                    }))
                    {
                        Item.TypeLabel.FontSize = 12;
                        Item.AmountLabel.FontSize = 13;
                        StatsListView.Items.Add(Item);
                    }
                }));
            }
        }

        private async void ViewAggregatedStatsButton_Click(object sender, RoutedEventArgs e)
        {
            AggregatedStats x = await Client.PVPNet.GetAggregatedStats(AccId, "CLASSIC", "3");
            Client.OverlayContainer.Content =
                new AggregatedStatsOverlay(x, AccId == Client.LoginPacket.AllSummonerData.Summoner.AcctId).Content;
            Client.OverlayContainer.Visibility = Visibility.Visible;
        }
    }
}