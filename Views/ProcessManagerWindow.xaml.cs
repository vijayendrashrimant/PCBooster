using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PCBooster.Services;

namespace PCBooster.Views
{
    public partial class ProcessManagerWindow : Window
    {
        private List<ProcessEntry>  _allProcesses  = new();
        private readonly DispatcherTimer _autoTimer = new();
        private string _currentSort   = "Ram";
        private string _searchText    = "";
        private bool   _autoRefresh   = true;

        public ProcessManagerWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += (_, __) => _autoTimer.Stop();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadProcesses();

            _autoTimer.Interval = TimeSpan.FromSeconds(4);
            _autoTimer.Tick    += (_, __) => { if (_autoRefresh) LoadProcesses(); };
            _autoTimer.Start();
        }

        private async void LoadProcesses()
        {
            ProcStatusText.Text = "Refreshing…";

            var procs = await Task.Run(() =>
                ProcessManagerService.GetProcesses(_currentSort));

            _allProcesses = procs;
            ApplyFilter();

            // Summary tiles
            int notResp = procs.Count(p => p.Status == "Not Responding");
            double totalRamGB = procs.Sum(p => p.RamMB) / 1024.0;

            TotalProcs.Text        = procs.Count.ToString();
            NotRespondingCount.Text = notResp.ToString();
            TotalRam.Text          = $"{totalRamGB:F1} GB";
            ProcCountLabel.Text    = $"{procs.Count} processes running";
            LastRefreshText.Text   = $"Last refresh: {DateTime.Now:HH:mm:ss}";
            ProcStatusText.Text    = "Ready  ·  Click Kill to end a process";
        }

        private void ApplyFilter()
        {
            var filtered = string.IsNullOrWhiteSpace(_searchText)
                ? _allProcesses
                : _allProcesses.Where(p =>
                    p.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    p.Pid.ToString().Contains(_searchText)).ToList();

            ProcessList.ItemsSource = filtered;
        }

        // ── Kill ─────────────────────────────────────────────────────────
        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int pid)
            {
                var entry = _allProcesses.FirstOrDefault(p => p.Pid == pid);
                string name = entry?.Name ?? pid.ToString();

                bool isSafe = ProcessManagerService.IsSafeToKill(name);
                string warning = isSafe
                    ? $"End process '{name}' (PID {pid})?"
                    : $"⚠  '{name}' may be a system process.\nForce-ending it could cause instability.\n\nProceed?";

                var result = MessageBox.Show(warning, "Confirm Kill",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var (success, message) = ProcessManagerService.KillProcess(pid);
                    ProcStatusText.Text = message;
                    if (success) LoadProcesses();
                }
            }
        }

        // ── Search ───────────────────────────────────────────────────────
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text;
            SearchHint.Visibility = string.IsNullOrEmpty(_searchText)
                ? Visibility.Visible : Visibility.Collapsed;
            ApplyFilter();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
            => SearchHint.Visibility = Visibility.Collapsed;

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
                SearchHint.Visibility = Visibility.Visible;
        }

        // ── Sort ─────────────────────────────────────────────────────────
        private void SortCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            _currentSort = SortCombo.SelectedIndex switch
            {
                1 => "Cpu",
                2 => "Name",
                _ => "Ram"
            };
            LoadProcesses();
        }

        // ── Refresh ──────────────────────────────────────────────────────
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadProcesses();

        private void AutoRefresh_Toggle(object sender, RoutedEventArgs e)
        {
            _autoRefresh = AutoRefreshToggle.IsChecked == true;
            AutoRefreshToggle.Content  = _autoRefresh ? "ON"  : "OFF";
            AutoRefreshToggle.Foreground = _autoRefresh
                ? System.Windows.Media.Brushes.LimeGreen
                : System.Windows.Media.Brushes.Gray;
        }
    }
}
