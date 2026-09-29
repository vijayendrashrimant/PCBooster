using System;
using System.Collections.Generic;
using System.IO;

namespace PCBooster.Services
{
    public class CleanResult
    {
        public long   BytesFreed   { get; set; }
        public int    FilesDeleted { get; set; }
        public int    FilesSkipped { get; set; }
        public double MBFreed      => Math.Round(BytesFreed / 1024.0 / 1024.0, 1);
    }

    public static class JunkCleanerService
    {
        // Directories to clean
        private static IEnumerable<string> GetTargetPaths()
        {
            yield return Path.GetTempPath();

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return System.IO.Path.Combine(appData, @"Microsoft\Windows\Recent");

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return System.IO.Path.Combine(localAppData, @"Microsoft\Windows\INetCache");
            yield return System.IO.Path.Combine(localAppData, @"Temp");
        }

        public static CleanResult Clean(IProgress<string> progress = null)
        {
            var result = new CleanResult();

            foreach (var dir in GetTargetPaths())
            {
                if (!Directory.Exists(dir)) continue;

                progress?.Report($"Scanning {dir}…");

                try
                {
                    foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            long size = info.Length;
                            info.Delete();
                            result.BytesFreed   += size;
                            result.FilesDeleted++;
                        }
                        catch
                        {
                            result.FilesSkipped++;
                        }
                    }

                    // Also try to remove empty subdirectories
                    foreach (var subDir in Directory.EnumerateDirectories(dir))
                    {
                        try { Directory.Delete(subDir, true); }
                        catch { /* In use */ }
                    }
                }
                catch { /* Access denied on directory */ }
            }

            progress?.Report($"Cleaned {result.MBFreed} MB ({result.FilesDeleted} files).");
            return result;
        }

        /// <summary>Returns estimated junk size in MB without deleting anything.</summary>
        public static double ScanSize()
        {
            long total = 0;
            foreach (var dir in GetTargetPaths())
            {
                if (!Directory.Exists(dir)) continue;
                try
                {
                    foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                    {
                        try { total += new FileInfo(file).Length; } catch { }
                    }
                }
                catch { }
            }
            return Math.Round(total / 1024.0 / 1024.0, 1);
        }
    }
}
