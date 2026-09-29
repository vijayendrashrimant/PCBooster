using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;

namespace PCBooster.Services
{
    public class StartupEntry
    {
        public string Name    { get; set; }
        public string Command { get; set; }
        public bool   IsUser  { get; set; } // HKCU vs HKLM
    }

    public static class StartupManagerService
    {
        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>Returns all startup entries from HKCU and HKLM.</summary>
        public static List<StartupEntry> GetStartupEntries()
        {
            var list = new List<StartupEntry>();

            // Current user
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, false))
            {
                if (key != null)
                    foreach (var name in key.GetValueNames())
                        list.Add(new StartupEntry
                        {
                            Name    = name,
                            Command = key.GetValue(name)?.ToString(),
                            IsUser  = true
                        });
            }

            // Local machine (may need elevation)
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(RunKey, false);
                if (key != null)
                    foreach (var name in key.GetValueNames())
                        list.Add(new StartupEntry
                        {
                            Name    = name,
                            Command = key.GetValue(name)?.ToString(),
                            IsUser  = false
                        });
            }
            catch { /* Not elevated */ }

            return list;
        }

        /// <summary>
        /// Removes a HKCU startup entry by name.
        /// Requires the app to run as the same user — no elevation needed.
        /// </summary>
        public static bool DisableUserEntry(string name)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
                if (key == null) return false;
                key.DeleteValue(name, false);
                return true;
            }
            catch { return false; }
        }

        /// <summary>Opens Task Manager on the Startup tab for manual management.</summary>
        public static void OpenTaskManagerStartup()
        {
            try
            {
                Process.Start(new ProcessStartInfo("taskmgr", "/0 /startup")
                {
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
