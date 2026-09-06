using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BarodaPOS.Services
{
    public class NetworkSyncManager
    {
        private const int UDP_PORT = 8888;
        private const int TCP_PORT = 8889;
        private const string MAGIC_HEADER = "POS_DISCOVER";

        private UdpClient _udpListener;
        private TcpListener _tcpListener;
        private CancellationTokenSource _cts;

        public string CurrentStatus { get; private set; } = "Searching";
        public string PeerIP { get; private set; } = "None";
        public string PeerHostname { get; private set; } = "None";
        public bool IsMasterNode { get; private set; } = false;
        
        public long MyFreeSpace { get; private set; }
        public long PeerFreeSpace { get; private set; } = -1;

        public event Action<string> OnLogEvent;
        public event Action OnStateChanged;

        public static NetworkSyncManager Instance { get; } = new NetworkSyncManager();

        private NetworkSyncManager()
        {
            MyFreeSpace = GetMaxFreeSpace();
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            CurrentStatus = "Searching";
            OnStateChanged?.Invoke();

            Task.Run(() => UdpListenLoop(_cts.Token));
            Task.Run(() => UdpBroadcastLoop(_cts.Token));
            Task.Run(() => TcpListenLoop(_cts.Token));

            NetworkStateMonitor.Instance.OnNetworkStateChanged += async (isOnline) =>
            {
                if (isOnline)
                {
                    try
                    {
                        var firestore = new FirestoreService();
                        await ProcessCloudResyncQueueAsync(firestore);
                    }
                    catch (Exception ex)
                    {
                        Log("Auto-sync failed: " + ex.Message);
                    }
                }
            };
        }

        public void Stop()
        {
            _cts?.Cancel();
            _udpListener?.Close();
            _tcpListener?.Stop();
        }

        private long GetMaxFreeSpace()
        {
            long maxSpace = 0;
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    if (drive.AvailableFreeSpace > maxSpace)
                        maxSpace = drive.AvailableFreeSpace;
                }
            }
            return maxSpace;
        }

        private async Task UdpBroadcastLoop(CancellationToken token)
        {
            using var udpBroadcaster = new UdpClient();
            udpBroadcaster.EnableBroadcast = true;
            
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var payload = new { Header = MAGIC_HEADER, Hostname = Dns.GetHostName(), FreeSpace = MyFreeSpace };
                    string json = JsonSerializer.Serialize(payload);
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    
                    var endpoint = new IPEndPoint(IPAddress.Broadcast, UDP_PORT);
                    await udpBroadcaster.SendAsync(bytes, bytes.Length, endpoint);
                }
                catch { }

                await Task.Delay(3000, token); // Broadcast every 3 seconds
            }
        }

        private async Task UdpListenLoop(CancellationToken token)
        {
            _udpListener = new UdpClient();
            _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, UDP_PORT));

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync();
                    string json = Encoding.UTF8.GetString(result.Buffer);
                    
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("Header", out var header) && header.GetString() == MAGIC_HEADER)
                    {
                        string hostname = root.GetProperty("Hostname").GetString();
                        if (hostname == Dns.GetHostName()) continue; // Ignore self

                        long peerSpace = root.GetProperty("FreeSpace").GetInt64();
                        
                        bool stateChanged = false;
                        if (PeerIP != result.RemoteEndPoint.Address.ToString())
                        {
                            PeerIP = result.RemoteEndPoint.Address.ToString();
                            PeerHostname = hostname;
                            PeerFreeSpace = peerSpace;
                            CurrentStatus = "Connected";
                            stateChanged = true;
                            
                            Log($"Discovered Peer: {hostname} ({PeerIP})");
                        }

                        if (PeerFreeSpace != peerSpace)
                        {
                            PeerFreeSpace = peerSpace;
                            stateChanged = true;
                        }

                        bool newMasterState = MyFreeSpace >= PeerFreeSpace;
                        if (IsMasterNode != newMasterState)
                        {
                            IsMasterNode = newMasterState;
                            stateChanged = true;
                            Log(IsMasterNode ? "Elected as Master Node based on storage." : "Assigned as Client Node based on storage.");
                        }

                        if (stateChanged) OnStateChanged?.Invoke();
                    }
                }
                catch (Exception ex) when (!(ex is ObjectDisposedException || ex is SocketException))
                {
                    Log("UDP Error: " + ex.Message);
                }
            }
        }

        private async Task TcpListenLoop(CancellationToken token)
        {
            try
            {
                _tcpListener = new TcpListener(IPAddress.Any, TCP_PORT);
                _tcpListener.Start();
                Log("TCP Sync Server listening on port " + TCP_PORT);

                while (!token.IsCancellationRequested)
                {
                    var client = await _tcpListener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleTcpClient(client, token));
                }
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException || ex is SocketException))
            {
                Log("TCP Listener Error: " + ex.Message);
            }
        }

        private async Task HandleTcpClient(TcpClient client, CancellationToken token)
        {
            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                string json = await reader.ReadToEndAsync();
                
                Log("Received Sync Payload: " + Truncate(json, 50));
                
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("Action", out var actionProp))
                {
                    string action = actionProp.GetString();
                    if (action == "SyncCollection")
                    {
                        string collection = root.GetProperty("Collection").GetString();
                        var data = root.GetProperty("Data");
                        
                        string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarodaPOS", "LocalDb");
                        Directory.CreateDirectory(baseDir);
                        string filePath = Path.Combine(baseDir, $"{collection}.json");
                        
                        await File.WriteAllTextAsync(filePath, data.GetRawText());
                        Log($"Successfully synced {collection} to local storage.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log("TCP Receive Error: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
        }

        public async Task ForceSyncAsync()
        {
            if (CurrentStatus != "Connected" || PeerIP == "None")
            {
                Log("Cannot force sync: No peer connected.");
                return;
            }

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(PeerIP, TCP_PORT);
                using var stream = client.GetStream();
                
                string payload = JsonSerializer.Serialize(new { Action = "ForceSync", Timestamp = DateTime.UtcNow });
                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                await stream.WriteAsync(bytes, 0, bytes.Length);
                
                Log("Force sync signal sent to " + PeerHostname);
            }
            catch (Exception ex)
            {
                Log("Force sync failed: " + ex.Message);
            }
        }

        public async Task SendSyncPayloadAsync(object data)
        {
            if (PeerIP == "None") return;

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(PeerIP, TCP_PORT);
                using var stream = client.GetStream();
                
                string json = JsonSerializer.Serialize(data);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                
                await stream.WriteAsync(bytes, 0, bytes.Length);
                Log("Sent Sync Payload: " + Truncate(json, 50));
            }
            catch (Exception ex)
            {
                Log("TCP Send Error: " + ex.Message);
                CurrentStatus = "Searching";
                PeerIP = "None";
                OnStateChanged?.Invoke();
            }
        }

        public void Log(string message)
        {
            OnLogEvent?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
            System.Diagnostics.Debug.WriteLine($"[NetworkSync] {message}");
        }

        private string Truncate(string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }
        
        public int GetOfflineQueueSize()
        {
            string queuePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarodaPOS", "LocalDb", "offline_queue.json");
            if (!File.Exists(queuePath)) return 0;
            
            try
            {
                string json = File.ReadAllText(queuePath);
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetArrayLength();
            }
            catch { return 0; }
        }

        public async Task ProcessCloudResyncQueueAsync(dynamic firestore)
        {
            string queuePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarodaPOS", "LocalDb", "offline_queue.json");
            if (!File.Exists(queuePath)) return;

            try
            {
                string json = await File.ReadAllTextAsync(queuePath);
                var queue = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
                
                if (queue == null || queue.Count == 0) return;

                Log($"Processing Cloud Re-Sync: {queue.Count} items pending...");
                
                var db = firestore.GetDb();
                var remainingQueue = new List<Dictionary<string, object>>();
                
                foreach (var item in queue)
                {
                    try
                    {
                        if (item.ContainsKey("collection") && item.ContainsKey("action") && item.ContainsKey("docId"))
                        {
                            string collection = item["collection"]?.ToString();
                            string action = item["action"]?.ToString();
                            string docId = item["docId"]?.ToString();
                            string dataString = item.ContainsKey("data") && item["data"] != null ? item["data"].ToString() : null;
                            
                            var docRef = db.Collection(collection).Document(docId);
                            
                            if (action == "Set" && dataString != null)
                            {
                                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataString);
                                await docRef.SetAsync(data, Google.Cloud.Firestore.SetOptions.MergeAll);
                            }
                            else if (action == "Delete")
                            {
                                await docRef.DeleteAsync();
                            }
                        }
                    }
                    catch (Exception itemEx)
                    {
                        Log($"Failed to sync item in collection {item["collection"]}: {itemEx.Message}");
                        remainingQueue.Add(item);
                    }
                }
                
                if (remainingQueue.Count == 0)
                {
                    File.Delete(queuePath);
                    Log("Cloud Re-Sync Complete.");
                }
                else
                {
                    await File.WriteAllTextAsync(queuePath, JsonSerializer.Serialize(remainingQueue, new JsonSerializerOptions { WriteIndented = true }));
                    Log($"Cloud Re-Sync Partially Complete. {remainingQueue.Count} items remaining.");
                }
                OnStateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Log("Cloud Re-Sync Failed: " + ex.Message);
            }
        }
    }
}
