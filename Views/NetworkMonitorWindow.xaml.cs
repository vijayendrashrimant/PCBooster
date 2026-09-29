using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class NetworkMonitorWindow : Window
    {
        private readonly NetworkMonitorService _service = new();
        private readonly DispatcherTimer       _timer   = new();

        private readonly Queue<double> _downHistory = new(31);
        private readonly Queue<double> _upHistory   = new(31);
        private double _peakDown, _peakUp;

        public NetworkMonitorWindow()
        {
            InitializeComponent();
            Loaded  += OnLoaded;
            Closed  += (_, __) => { _timer.Stop(); _service.Dispose(); };
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick    += OnTick;
            _timer.Start();
            OnTick(null, null); // immediate first read
        }

        private void OnTick(object sender, EventArgs e)
        {
            var snap = _service.GetSnapshot();

            // Speed displays
            DownloadSpeed.Text = FormatSpeed(snap.DownloadKbps);
            UploadSpeed.Text   = FormatSpeed(snap.UploadKbps);
            TotalReceived.Text = FormatMB(snap.TotalRecvMB);
            TotalSent.Text     = FormatMB(snap.TotalSentMB);

            // Peaks
            if (snap.DownloadKbps > _peakDown) _peakDown = snap.DownloadKbps;
            if (snap.UploadKbps   > _peakUp)   _peakUp   = snap.UploadKbps;
            DownloadPeak.Text = $"Peak: {FormatSpeed(_peakDown)}";
            UploadPeak.Text   = $"Peak: {FormatSpeed(_peakUp)}";

            // Adapter info
            AdapterLabel.Text   = snap.AdapterName;
            FullAdapterName.Text = snap.AdapterName;
            IpLabel.Text        = snap.IpAddress;
            MacLabel.Text       = snap.MacAddress;
            StatusLabel.Text    = snap.Status;

            // History
            if (_downHistory.Count >= 30) _downHistory.Dequeue();
            if (_upHistory.Count   >= 30) _upHistory.Dequeue();
            _downHistory.Enqueue(snap.DownloadKbps);
            _upHistory.Enqueue(snap.UploadKbps);

            // Progress bars (normalise against peak or 10 Mbps)
            double norm = Math.Max(_peakDown, 10240);
            DownloadBar.Value = Math.Min(100, snap.DownloadKbps / norm * 100);
            UploadBar.Value   = Math.Min(100, snap.UploadKbps   / Math.Max(_peakUp, 1024) * 100);

            DrawGraph();
            NetStatusText.Text = $"Updated {DateTime.Now:HH:mm:ss}  ·  Polling every 1 s";
        }

        private void DrawGraph()
        {
            SpeedGraph.Children.Clear();

            double w = SpeedGraph.ActualWidth;
            double h = SpeedGraph.ActualHeight;
            if (w < 10 || h < 10) return;

            var downArr = _downHistory.ToArray();
            var upArr   = _upHistory.ToArray();
            double maxVal = Math.Max(downArr.Concat(upArr).DefaultIfEmpty(1).Max(), 1);

            DrawLine(downArr, maxVal, w, h, Color.FromRgb(0x28, 0xC8, 0x40));
            DrawLine(upArr,   maxVal, w, h, Color.FromRgb(0x4F, 0xA3, 0xE0));

            // Gridlines
            for (int i = 1; i <= 3; i++)
            {
                double y = h - (i / 4.0) * h;
                var line = new Line
                {
                    X1 = 0, X2 = w, Y1 = y, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 0x4A, 0x55, 0x68)),
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };
                SpeedGraph.Children.Add(line);
            }
        }

        private void DrawLine(double[] values, double maxVal, double w, double h, Color color)
        {
            if (values.Length < 2) return;

            var poly = new Polyline
            {
                Stroke          = new SolidColorBrush(color),
                StrokeThickness = 2,
                StrokeLineJoin  = PenLineJoin.Round
            };

            int n = values.Length;
            for (int i = 0; i < n; i++)
            {
                double x = i / (double)(30 - 1) * w;
                double y = h - (values[i] / maxVal) * (h - 8);
                poly.Points.Add(new Point(x, y));
            }
            SpeedGraph.Children.Add(poly);

            // Filled area under the line
            var fill = new Polygon { Opacity = 0.12, Fill = new SolidColorBrush(color) };
            fill.Points.Add(new Point(0, h));
            foreach (var pt in poly.Points) fill.Points.Add(pt);
            fill.Points.Add(new Point(poly.Points.Last().X, h));
            SpeedGraph.Children.Insert(0, fill);
        }

        private void SpeedTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://www.fast.com") { UseShellExecute = true });
            }
            catch { }
        }

        private static string FormatSpeed(double kbps)
        {
            if (kbps >= 1024 * 1024) return $"{kbps / 1024 / 1024:F2} Gbps";
            if (kbps >= 1024)        return $"{kbps / 1024:F1} Mbps";
            return $"{kbps:F0} Kbps";
        }

        private static string FormatMB(double mb)
        {
            if (mb >= 1024) return $"{mb / 1024:F2} GB";
            return $"{mb:F1} MB";
        }
    }
}
