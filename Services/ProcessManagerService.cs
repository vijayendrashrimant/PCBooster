using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PCBooster.Services
{
    public class ProcessEntry
    {
        public int    Pid         { get; set; }
        public string Name        { get; set; }
        public double CpuPercent  { get; set; }
        public double RamMB       { get; set; }
        public string Status      { get; set; }
        public string User        { get; set; }
        public DateTime StartTime { get; set; }
        public string StartTimeDisplay => StartTime == default ? "—" : StartTime.ToString("HH:mm");

        // Categorise impact for colour coding
        public string Impact => CpuPercent >= 20 || RamMB >= 500 ? "High"
                              : CpuPercent >= 5  || RamMB >= 150 ? "Medium"
                                                                  : "Low";
    }

    public static class ProcessManagerService
    {
        // Known safe-to-kill background processes (display names only)
        private static readonly HashSet<string> SafeToKill = new(StringComparer.OrdinalIgnoreCase)
        {
            "SearchIndexer", "OneDrive", "Teams", "Slack", "Discord",
            "Spotify", "Steam", "EpicGamesLauncher", "AdobeUpdateService",
            "CCleaner", "Skype", "Cortana"
        };

        public static List<ProcessEntry> GetProcesses(string sortBy = "Ram")
        {
            var list = new List<ProcessEntry>();

            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    var entry = new ProcessEntry
                    {
                        Pid    = proc.Id,
                        Name   = proc.ProcessName,
                        RamMB  = Math.Round(proc.WorkingSet64 / 1024.0 / 1024.0, 1),
                        Status = proc.Responding ? "Running" : "Not Responding",
                    };

                    try { entry.StartTime = proc.StartTime; } catch { }

                    list.Add(entry);
                }
                catch { }
                finally { proc.Dispose(); }
            }

            // Sort
            return sortBy switch
            {
                "Cpu" => list.OrderByDescending(p => p.CpuPercent).ToList(),
                "Name"=> list.OrderBy(p => p.Name).ToList(),
                "Pid" => list.OrderBy(p => p.Pid).ToList(),
                _     => list.OrderByDescending(p => p.RamMB).ToList()
            };
        }

        /// <summary>
        /// Fills CPU% for each entry using a two-sample approach (call once, wait 1s, call again).
        /// Returns a dict of PID → cpu%.
        /// </summary>
        public static Dictionary<int, double> SampleCpu(IEnumerable<int> pids, int intervalMs = 1000)
        {
            var result  = new Dictionary<int, double>();
            var samples = new Dictionary<int, (TimeSpan total, DateTime wall)>();

            foreach (int pid in pids)
            {
                try
                {
                    using var p = Process.GetProcessById(pid);
                    samples[pid] = (p.TotalProcessorTime, DateTime.UtcNow);
                }
                catch { }
            }

            System.Threading.Thread.Sleep(intervalMs);

            int cpuCount = Environment.ProcessorCount;
            foreach (var (pid, (t0, w0)) in samples)
            {
                try
                {
                    using var p = Process.GetProcessById(pid);
                    var t1  = p.TotalProcessorTime;
                    var w1  = DateTime.UtcNow;
                    double elapsed = (w1 - w0).TotalSeconds;
                    if (elapsed > 0)
                        result[pid] = Math.Round((t1 - t0).TotalSeconds / (elapsed * cpuCount) * 100.0, 1);
                }
                catch { result[pid] = 0; }
            }

            return result;
        }

        /// <summary>Kills a process by PID. Returns true on success.</summary>
        public static (bool Success, string Message) KillProcess(int pid)
        {
            try
            {
                using var proc = Process.GetProcessById(pid);
                string name = proc.ProcessName;
                proc.Kill();
                return (true, $"'{name}' (PID {pid}) terminated.");
            }
            catch (Exception ex)
            {
                return (false, $"Could not kill PID {pid}: {ex.Message}");
            }
        }

        public static bool IsSafeToKill(string name) =>
            SafeToKill.Contains(name);

        /// <summary>Returns processes likely responsible for high resource usage.</summary>
        public static List<ProcessEntry> GetHeavyHitters(List<ProcessEntry> all, int topN = 5)
            => all.Where(p => p.RamMB > 50)
                  .OrderByDescending(p => p.RamMB + p.CpuPercent * 10)
                  .Take(topN)
                  .ToList();
    }
}
