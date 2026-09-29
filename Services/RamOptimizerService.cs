using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PCBooster.Services
{
    public class RamResult
    {
        public long   BytesReclaimed   { get; set; }
        public int    ProcessesTrimmed { get; set; }
        public double MBReclaimed      => Math.Round(BytesReclaimed / 1024.0 / 1024.0, 1);
    }

    public static class RamOptimizerService
    {
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(
            IntPtr hProcess,
            IntPtr dwMinimumWorkingSetSize,
            IntPtr dwMaximumWorkingSetSize);

        [DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        public static RamResult Optimize(IProgress<string> progress = null)
        {
            var result = new RamResult();

            // Snapshot RAM before
            long before = GC.GetTotalMemory(false);

            progress?.Report("Trimming process working sets…");

            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    long ws = proc.WorkingSet64;
                    // Trim working set (request OS to page out idle memory)
                    SetProcessWorkingSetSize(proc.Handle, (IntPtr)(-1), (IntPtr)(-1));
                    result.BytesReclaimed   += ws - proc.WorkingSet64;
                    result.ProcessesTrimmed++;
                }
                catch { /* Access denied or process exited */ }
                finally
                {
                    proc.Dispose();
                }
            }

            // Force .NET GC
            progress?.Report("Collecting .NET garbage…");
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

            long after = GC.GetTotalMemory(true);
            result.BytesReclaimed = Math.Max(result.BytesReclaimed, before - after);

            progress?.Report($"Reclaimed ~{result.MBReclaimed} MB across {result.ProcessesTrimmed} processes.");
            return result;
        }
    }
}
