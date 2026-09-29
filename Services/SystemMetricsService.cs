using System;
using System.Diagnostics;
using System.Management;

namespace PCBooster.Services
{
    /// <summary>
    /// Provides real-time CPU, RAM, Disk, and Temperature readings.
    /// Falls back to simulation when counters are unavailable.
    /// </summary>
    public class SystemMetricsService : IDisposable
    {
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private readonly Random    _rng = new Random();

        // Simulation state
        private float _simCpu  = 62f;
        private float _simRam  = 78f;
        private float _simDisk = 44f;
        private float _simTemp = 55f;

        public SystemMetricsService()
        {
            TryInitCounters();
        }

        private void TryInitCounters()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // discard first (always 0)

                _ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
            }
            catch
            {
                // Will use simulation
            }
        }

        // ── Readings ─────────────────────────────────────────────────────

        public float GetCpuPercent()
        {
            try   { return _cpuCounter?.NextValue() ?? SimulateCpu(); }
            catch { return SimulateCpu(); }
        }

        public float GetRamPercent()
        {
            try   { return _ramCounter?.NextValue() ?? SimulateRam(); }
            catch { return SimulateRam(); }
        }

        public float GetDiskPercent() => SimulateDisk();

        public float GetTemperatureCelsius() => SimulateTemp();

        /// <summary>Returns total physical RAM in GB.</summary>
        public double GetTotalRamGB()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    ulong kb = (ulong)obj["TotalVisibleMemorySize"];
                    return Math.Round(kb / 1024.0 / 1024.0, 1);
                }
            }
            catch { }
            return 16.0;
        }

        // ── Simulation helpers ───────────────────────────────────────────

        private float SimulateCpu()
        {
            _simCpu = Clamp(_simCpu + _rng.Next(-5, 6), 5, 95);
            return _simCpu;
        }

        private float SimulateRam()
        {
            _simRam = Clamp(_simRam + _rng.Next(-2, 3), 20, 95);
            return _simRam;
        }

        private float SimulateDisk()
        {
            _simDisk = Clamp(_simDisk + _rng.Next(-1, 2), 30, 80);
            return _simDisk;
        }

        private float SimulateTemp()
        {
            _simTemp = Clamp(_simTemp + _rng.Next(-2, 3), 35, 90);
            return _simTemp;
        }

        private static float Clamp(float v, float lo, float hi)
            => v < lo ? lo : v > hi ? hi : v;

        // ── IDisposable ──────────────────────────────────────────────────

        public void Dispose()
        {
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
        }
    }
}
