using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class BatteryOptimizerWindow : Window
    {
        public BatteryOptimizerWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var info = BatteryService.GetBatteryInfo();
            Populate(info);
        }

        private void Populate(BatteryInfo info)
        {
            if (!info.IsPresent)
            {
                BatterySubtitle.Text  = "No battery detected";
                ChargePercent.Text    = "N/A";
                ChargeStatus.Text     = "Desktop PC";
                HealthLabel.Text      = "N/A";
                TimeRemaining.Text    = "—";
                TipsPanel.Children.Clear();
                AddTip("🖥  No battery found. This tool is for laptops and tablets.");
                return;
            }

            // Header
            BatterySubtitle.Text = info.DeviceName;

            // Charge
            ChargePercent.Text = $"{info.ChargePercent}%";
            ChargeStatus.Text  = info.Status;

            // Health
            HealthLabel.Text = info.HealthLabel;
            HealthBar.Value  = info.WearLevel;
            HealthBar.Foreground = new SolidColorBrush(info.WearLevel >= 80
                ? Color.FromRgb(0x28, 0xC8, 0x40)
                : info.WearLevel >= 50
                    ? Color.FromRgb(0xFE, 0xBC, 0x2E)
                    : Color.FromRgb(0xFF, 0x5F, 0x57));

            WearText.Text  = $"Wear level: {info.WearLevel}%  ({100 - info.WearLevel}% degraded)";
            ChemText.Text  = $"Chemistry: {info.Chemistry}";

            // Time / Capacity
            TimeRemaining.Text = info.TimeRemaining;
            CapacityText.Text  = info.FullChargeCapacity > 0
                ? $"{info.FullChargeCapacity:N0} / {info.DesignCapacity:N0} mWh"
                : "Unavailable";

            // Draw battery icon
            DrawBatteryIcon(info.ChargePercent, info.Status.Contains("Charging"));

            // Tips
            TipsPanel.Children.Clear();
            foreach (var tip in BatteryService.GetOptimizationTips(info))
                AddTip(tip);
        }

        private void AddTip(string text)
        {
            var border = new Border
            {
                Background     = new SolidColorBrush(Color.FromRgb(0x0D, 0x18, 0x30)),
                BorderBrush    = new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61)),
                BorderThickness= new Thickness(1),
                CornerRadius   = new CornerRadius(6),
                Padding        = new Thickness(12, 9, 12, 9),
                Margin         = new Thickness(0, 0, 0, 6)
            };
            border.Child = new TextBlock
            {
                Text         = text,
                Foreground   = new SolidColorBrush(Color.FromRgb(0xE8, 0xEA, 0xF6)),
                FontSize     = 12,
                TextWrapping = TextWrapping.Wrap
            };
            TipsPanel.Children.Add(border);
        }

        private void DrawBatteryIcon(int percent, bool charging)
        {
            BatteryCanvas.Children.Clear();

            double bw = 56, bh = 28;
            double bx = (80 - bw) / 2, by = (80 - bh) / 2;

            // Outer shell
            var shell = new Rectangle
            {
                Width  = bw, Height = bh,
                Stroke = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0)),
                StrokeThickness = 2.5,
                Fill   = Brushes.Transparent,
                RadiusX = 4, RadiusY = 4
            };
            Canvas.SetLeft(shell, bx);
            Canvas.SetTop(shell, by);
            BatteryCanvas.Children.Add(shell);

            // Positive terminal nub
            var nub = new Rectangle
            {
                Width = 5, Height = 12,
                Fill = new SolidColorBrush(Color.FromRgb(0x9F, 0xA8, 0xC0)),
                RadiusX = 2, RadiusY = 2
            };
            Canvas.SetLeft(nub, bx + bw + 1);
            Canvas.SetTop(nub, by + (bh - 12) / 2);
            BatteryCanvas.Children.Add(nub);

            // Fill level
            Color fillColor = percent >= 60
                ? Color.FromRgb(0x28, 0xC8, 0x40)
                : percent >= 25
                    ? Color.FromRgb(0xFE, 0xBC, 0x2E)
                    : Color.FromRgb(0xFF, 0x5F, 0x57);

            double fillW = Math.Max(2, (bw - 6) * percent / 100.0);
            var fill = new Rectangle
            {
                Width = fillW, Height = bh - 8,
                Fill = new SolidColorBrush(fillColor),
                RadiusX = 2, RadiusY = 2
            };
            Canvas.SetLeft(fill, bx + 4);
            Canvas.SetTop(fill, by + 4);
            BatteryCanvas.Children.Add(fill);

            // Charging bolt
            if (charging)
            {
                var bolt = new TextBlock
                {
                    Text       = "⚡",
                    FontSize   = 18,
                    Foreground = Brushes.White
                };
                Canvas.SetLeft(bolt, bx + bw / 2 - 9);
                Canvas.SetTop(bolt, by + bh / 2 - 14);
                BatteryCanvas.Children.Add(bolt);
            }
        }

        // ── Actions ──────────────────────────────────────────────────────

        private void EnableBatterySaver_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Toggle power saver via powercfg
                Process.Start(new ProcessStartInfo("powercfg", "/setactive SCHEME_MAX")
                {
                    CreateNoWindow = true, UseShellExecute = false
                });
                ActionStatus.Text = "✓ Power Saver plan activated.";
            }
            catch
            {
                ActionStatus.Text = "Could not change power plan automatically. Open Power Options manually.";
            }
        }

        private void LowerBrightness_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open display settings
                Process.Start(new ProcessStartInfo("ms-settings:display") { UseShellExecute = true });
                ActionStatus.Text = "Opening display settings…";
            }
            catch
            {
                ActionStatus.Text = "Open Settings → Display → Brightness manually.";
            }
        }

        private void OpenPowerOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("powercfg.cpl") { UseShellExecute = true });
            }
            catch { }
        }

        private void BatteryReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string reportPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "battery-report.html");

                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo("powercfg", $"/batteryreport /output \"{reportPath}\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                };
                proc.Start();
                proc.WaitForExit(5000);

                if (File.Exists(reportPath))
                {
                    Process.Start(new ProcessStartInfo(reportPath) { UseShellExecute = true });
                    ActionStatus.Text = "✓ Battery report opened in browser.";
                }
                else
                {
                    ActionStatus.Text = "Report saved. Check your user profile folder.";
                }
            }
            catch (Exception ex)
            {
                ActionStatus.Text = $"Error: {ex.Message}";
            }
        }
    }
}
