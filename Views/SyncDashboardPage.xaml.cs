using System;
using System.Windows;
using System.Windows.Controls;
using BarodaPOS.Services;

namespace BarodaPOS.Views
{
    public partial class SyncDashboardPage : Page
    {
        private NetworkSyncManager _syncManager;

        public SyncDashboardPage()
        {
            InitializeComponent();
            this.Loaded += SyncDashboardPage_Loaded;
            this.Unloaded += SyncDashboardPage_Unloaded;
        }

        private void SyncDashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            _syncManager = NetworkSyncManager.Instance;
            _syncManager.OnStateChanged += SyncManager_OnStateChanged;
            _syncManager.OnLogEvent += SyncManager_OnLogEvent;
            
            NetworkStateMonitor.Instance.OnNetworkStateChanged += NetworkStateMonitor_OnNetworkStateChanged;
            
            UpdateUI();
        }

        private void SyncDashboardPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_syncManager != null)
            {
                _syncManager.OnStateChanged -= SyncManager_OnStateChanged;
                _syncManager.OnLogEvent -= SyncManager_OnLogEvent;
            }
            NetworkStateMonitor.Instance.OnNetworkStateChanged -= NetworkStateMonitor_OnNetworkStateChanged;
        }

        private void NetworkStateMonitor_OnNetworkStateChanged(bool isOnline)
        {
            Dispatcher.Invoke(UpdateUI);
        }

        private void SyncManager_OnStateChanged()
        {
            Dispatcher.Invoke(UpdateUI);
        }

        private void UpdateUI()
        {
            bool isOnline = NetworkStateMonitor.Instance.IsOnline;
            
            if (isOnline)
            {
                CurrentModeText.Text = "Cloud (Firebase)";
                CurrentModeText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.MediumSeaGreen);
            }
            else
            {
                CurrentModeText.Text = "Local Fallback (P2P)";
                CurrentModeText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkOrange);
            }

            int queueSize = _syncManager.GetOfflineQueueSize();
            QueueBadge.Content = queueSize.ToString();
            
            FlushQueueButton.IsEnabled = isOnline && queueSize > 0;

            StatusText.Text = _syncManager.CurrentStatus;
            
            if (_syncManager.PeerIP != "None")
                PeerText.Text = $"{_syncManager.PeerHostname} ({_syncManager.PeerIP})";
            else
                PeerText.Text = "Searching...";

            if (_syncManager.PeerIP != "None")
                MasterStatusText.Text = _syncManager.IsMasterNode ? "This PC (Master DB)" : "Peer PC (Master DB)";
            else
                MasterStatusText.Text = "Waiting for peer...";
        }

        private async void FlushQueue_Click(object sender, RoutedEventArgs e)
        {
            FlushQueueButton.IsEnabled = false;
            FlushQueueButton.Content = "Syncing...";
            
            await _syncManager.ProcessCloudResyncQueueAsync(new FirestoreService());
            
            FlushQueueButton.Content = "Push to Cloud";
            UpdateUI();
        }

        private void SyncManager_OnLogEvent(string logMessage)
        {
            Dispatcher.Invoke(() =>
            {
                LogText.Text += logMessage + Environment.NewLine;
                LogScrollViewer.ScrollToEnd();
            });
        }

        private async void ForceSync_Click(object sender, RoutedEventArgs e)
        {
            if (_syncManager != null)
            {
                await _syncManager.ForceSyncAsync();
            }
        }
    }
}
