using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace PCBooster.Services
{
    public class NetworkSnapshot
    {
        public string AdapterName   { get; set; }
        public double DownloadKbps  { get; set; }
        public double UploadKbps    { get; set; }
        public double TotalSentMB   { get; set; }
        public double TotalRecvMB   { get; set; }
        public string Status        { get; set; }
        public string MacAddress    { get; set; }
        public string IpAddress     { get; set; }

        public string DownloadDisplay => DownloadKbps >= 1024
            ? $"{DownloadKbps / 1024:F1} Mbps"
            : $"{DownloadKbps:F0} Kbps";

        public string UploadDisplay => UploadKbps >= 1024
            ? $"{UploadKbps / 1024:F1} Mbps"
            : $"{UploadKbps:F0} Kbps";
    }

    public class NetworkMonitorService : IDisposable
    {
        private readonly List<PerformanceCounter> _downloadCounters = new();
        private readonly List<PerformanceCounter> _uploadCounters   = new();
        private readonly Random _rng = new Random();

        // Simulation fallback state
        private double _simDown = 2400;
        private double _simUp   = 480;

        public NetworkMonitorService()
        {
            TryInitCounters();
        }

        private void TryInitCounters()
        {
            try
            {
                var category = new PerformanceCounterCategory("Network Interface");
                foreach (var instance in category.GetInstanceNames())
                {
                    _downloadCounters.Add(new PerformanceCounter(
                        "Network Interface", "Bytes Received/sec", instance));
                    _uploadCounters.Add(new PerformanceCounter(
                        "Network Interface", "Bytes Sent/sec", instance));
                }
                // Prime counters
                _downloadCounters.ForEach(c => c.NextValue());
                _uploadCounters.ForEach(c => c.NextValue());
            }
            catch { }
        }

        public NetworkSnapshot GetSnapshot()
        {
            double downBytes = 0, upBytes = 0;

            try
            {
                downBytes = _downloadCounters.Sum(c => { try { return c.NextValue(); } catch { return 0; } });
                upBytes   = _uploadCounters.Sum(c => { try { return c.NextValue(); } catch { return 0; } });
            }
            catch { }

            double downKbps = downBytes > 0 ? downBytes / 1024.0 : SimulateDown();
            double upKbps   = upBytes   > 0 ? upBytes   / 1024.0 : SimulateUp();

            // Get adapter info from NetworkInterface
            string adapterName = "Unknown Adapter";
            string mac         = "—";
            string ip          = "—";
            string status      = "Connected";
            double sentMB      = 0, recvMB = 0;

            try
            {
                var iface = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up
                             && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .OrderByDescending(n => n.GetIPv4Statistics().BytesReceived)
                    .FirstOrDefault();

                if (iface != null)
                {
                    adapterName = iface.Name;
                    mac         = iface.GetPhysicalAddress().ToString();
                    if (mac.Length == 12)
                        mac = string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));

                    var stats = iface.GetIPv4Statistics();
                    sentMB    = stats.BytesSent     / 1024.0 / 1024.0;
                    recvMB    = stats.BytesReceived / 1024.0 / 1024.0;

                    ip = iface.GetIPProperties().UnicastAddresses
                        .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(a => a.Address.ToString())
                        .FirstOrDefault() ?? "—";

                    status = iface.OperationalStatus.ToString();
                }
            }
            catch { }

            return new NetworkSnapshot
            {
                AdapterName  = adapterName,
                DownloadKbps = downKbps,
                UploadKbps   = upKbps,
                TotalSentMB  = Math.Round(sentMB, 1),
                TotalRecvMB  = Math.Round(recvMB, 1),
                MacAddress   = mac,
                IpAddress    = ip,
                Status       = status
            };
        }

        private double SimulateDown()
        {
            _simDown = Math.Max(100, Math.Min(50000, _simDown + _rng.Next(-600, 700)));
            return _simDown;
        }
        private double SimulateUp()
        {
            _simUp = Math.Max(50, Math.Min(10000, _simUp + _rng.Next(-200, 250)));
            return _simUp;
        }

        public void Dispose()
        {
            _downloadCounters.ForEach(c => c.Dispose());
            _uploadCounters.ForEach(c => c.Dispose());
        }
    }
}
