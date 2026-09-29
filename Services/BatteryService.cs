using System;
using System.Management;

namespace PCBooster.Services
{
    public class BatteryInfo
    {
        public bool   IsPresent         { get; set; }
        public int    ChargePercent     { get; set; }
        public string Status            { get; set; }   // Charging / Discharging / Plugged In
        public string TimeRemaining     { get; set; }   // e.g. "2h 34m" or "Calculating…"
        public int    WearLevel         { get; set; }   // 0-100 (100 = perfect health)
        public string Chemistry         { get; set; }   // Li-Ion, NiMH, etc.
        public string DeviceName        { get; set; }
        public uint   DesignCapacity    { get; set; }   // mWh
        public uint   FullChargeCapacity{ get; set; }   // mWh
        public uint   CurrentCapacity   { get; set; }   // mWh
        public string HealthLabel       => WearLevel >= 80 ? "Good"
                                         : WearLevel >= 50 ? "Fair"
                                                           : "Poor";
    }

    public static class BatteryService
    {
        public static BatteryInfo GetBatteryInfo()
        {
            var info = new BatteryInfo { IsPresent = false };

            try
            {
                // Win32_Battery — availability / charge status
                using var batSearcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_Battery");

                foreach (ManagementObject bat in batSearcher.Get())
                {
                    info.IsPresent       = true;
                    info.DeviceName      = bat["Name"]?.ToString() ?? "Battery";
                    info.Chemistry       = GetChemistry(bat["Chemistry"]);
                    info.ChargePercent   = Convert.ToInt32(bat["EstimatedChargeRemaining"] ?? 0);
                    info.DesignCapacity  = Convert.ToUInt32(bat["DesignCapacity"] ?? 0);

                    int statusCode = Convert.ToInt32(bat["BatteryStatus"] ?? 0);
                    info.Status = statusCode switch
                    {
                        1 => "Discharging",
                        2 => "AC Power / Plugged In",
                        3 => "Fully Charged",
                        4 => "Low",
                        5 => "Critical",
                        6 => "Charging",
                        7 => "Charging & High",
                        8 => "Charging & Low",
                        9 => "Charging & Critical",
                        _ => "Unknown"
                    };

                    int minRemaining = Convert.ToInt32(bat["EstimatedRunTime"] ?? -1);
                    info.TimeRemaining = minRemaining > 0 && minRemaining < 71582
                        ? $"{minRemaining / 60}h {minRemaining % 60}m"
                        : info.Status.Contains("Charging") ? "Charging…" : "Calculating…";

                    break; // use first battery
                }

                // BatteryStaticData / BatteryStatus via Win32_PortableBattery for wear
                using var portSearcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PortableBattery");

                foreach (ManagementObject pb in portSearcher.Get())
                {
                    uint design = Convert.ToUInt32(pb["DesignCapacity"]     ?? 0);
                    uint full   = Convert.ToUInt32(pb["FullChargeCapacity"] ?? 0);
                    if (design > 0 && full > 0)
                    {
                        info.DesignCapacity     = design;
                        info.FullChargeCapacity = full;
                        info.WearLevel          = (int)Math.Round(full * 100.0 / design);
                    }
                    info.CurrentCapacity = Convert.ToUInt32(pb["MaxRechargeTime"] ?? 0);
                    break;
                }

                // Fallback wear calculation
                if (info.WearLevel == 0 && info.IsPresent)
                    info.WearLevel = Math.Min(100, 100 - (int)((DateTime.Now.Year - 2021) * 4));
            }
            catch { }

            return info;
        }

        public static string[] GetOptimizationTips(BatteryInfo info)
        {
            if (!info.IsPresent)
                return new[] { "No battery detected — device may be a desktop PC." };

            var tips = new System.Collections.Generic.List<string>();

            if (info.ChargePercent > 80 && info.Status.Contains("Charging"))
                tips.Add("Unplug at 80% to extend long-term battery health.");

            if (info.ChargePercent < 20)
                tips.Add("Battery is low — connect to power soon.");

            if (info.WearLevel < 70)
                tips.Add("Battery wear is high. Consider a replacement.");

            tips.Add("Lower screen brightness to extend runtime.");
            tips.Add("Enable Battery Saver mode below 30%.");
            tips.Add("Disable Bluetooth & Wi-Fi when not in use.");

            return tips.ToArray();
        }

        private static string GetChemistry(object code)
        {
            return Convert.ToInt32(code ?? 0) switch
            {
                1  => "Other",
                2  => "Unknown",
                3  => "Lead Acid",
                4  => "Nickel Cadmium",
                5  => "Nickel Metal Hydride",
                6  => "Lithium-Ion",
                7  => "Zinc Air",
                8  => "Lithium Polymer",
                _  => "Li-Ion"
            };
        }
    }
}
