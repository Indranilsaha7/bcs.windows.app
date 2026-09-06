using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace BarodaPOS.Services
{
    public class NetworkStateMonitor
    {
        private static NetworkStateMonitor _instance;
        public static NetworkStateMonitor Instance => _instance ??= new NetworkStateMonitor();

        private CancellationTokenSource _cts;
        private bool _isOnline = true; // Assume online initially
        
        public bool IsOnline
        {
            get => _isOnline;
            private set
            {
                if (_isOnline != value)
                {
                    _isOnline = value;
                    OnNetworkStateChanged?.Invoke(_isOnline);
                }
            }
        }

        public event Action<bool> OnNetworkStateChanged;

        private NetworkStateMonitor()
        {
        }

        public void StartMonitoring()
        {
            if (_cts != null) return;
            
            _cts = new CancellationTokenSource();
            Task.Run(() => MonitorLoop(_cts.Token));
        }

        public void StopMonitoring()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                bool currentStatus = await CheckConnectionAsync();
                IsOnline = currentStatus;
                
                // Check every 5 seconds
                await Task.Delay(5000, token);
            }
        }

        private async Task<bool> CheckConnectionAsync()
        {
            try
            {
                using (var ping = new Ping())
                {
                    // Ping Google DNS as a reliable internet check
                    var reply = await ping.SendPingAsync("8.8.8.8", 2000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
