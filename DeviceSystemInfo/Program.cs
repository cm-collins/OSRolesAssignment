using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Management;

namespace DeviceSystemInfo
{
    internal class Program
    {
        static void Main()
        {
            Console.Title = "OS Role: Device Management / System Information";
            PrintHeader("DEVICE MANAGEMENT / SYSTEM INFORMATION (C#)");

            PrintBasicSystemInfo();
            PrintCpuInfo();
            PrintMemoryInfo();
            PrintDiskInfo();
            PrintGpuInfo();
            PrintNetworkInfo();

            Console.WriteLine();
            Console.WriteLine("Done. Press any key to exit...");
            Console.ReadKey();
        }

        // -------------------- Sections --------------------

        static void PrintBasicSystemInfo()
        {
            PrintSection("Basic System Info");

            Console.WriteLine($"Machine Name  : {Environment.MachineName}");
            Console.WriteLine($"User Name     : {Environment.UserName}");
            Console.WriteLine($"OS Version    : {Environment.OSVersion}");
            Console.WriteLine($"OS Desc       : {RuntimeInformation.OSDescription}");
            Console.WriteLine($"Architecture  : {RuntimeInformation.OSArchitecture}");
            Console.WriteLine($"Process Arch  : {RuntimeInformation.ProcessArchitecture}");
            Console.WriteLine($".NET Runtime  : {RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"64-bit OS?    : {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"64-bit Proc?  : {Environment.Is64BitProcess}");
            Console.WriteLine($"System Dir    : {Environment.SystemDirectory}");
        }

        static void PrintCpuInfo()
        {
            PrintSection("CPU Info (WMI: Win32_Processor)");

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor");

                var results = searcher.Get().Cast<ManagementObject>().ToList();

                if (results.Count == 0)
                {
                    Console.WriteLine("No CPU info found (WMI returned 0 results).");
                    return;
                }

                int index = 1;
                foreach (var cpu in results)
                {
                    Console.WriteLine($"CPU #{index++}");
                    Console.WriteLine($"  Name                 : {cpu["Name"] ?? "N/A"}");
                    Console.WriteLine($"  Cores                : {cpu["NumberOfCores"] ?? "N/A"}");
                    Console.WriteLine($"  Logical Processors   : {cpu["NumberOfLogicalProcessors"] ?? "N/A"}");
                    Console.WriteLine($"  Max Clock Speed (MHz): {cpu["MaxClockSpeed"] ?? "N/A"}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CPU info failed (WMI).");
                Console.WriteLine($"Reason: {ex.Message}");
            }
        }

        static void PrintMemoryInfo()
        {
            PrintSection("Memory Info (WMI: Win32_OperatingSystem)");

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");

                foreach (ManagementObject os in searcher.Get())
                {
                    // Values are in KB
                    ulong totalKb = ToUInt64(os["TotalVisibleMemorySize"]);
                    ulong freeKb = ToUInt64(os["FreePhysicalMemory"]);

                    ulong totalBytes = totalKb * 1024;
                    ulong freeBytes = freeKb * 1024;
                    ulong usedBytes = totalBytes > freeBytes ? (totalBytes - freeBytes) : 0;

                    Console.WriteLine($"Total RAM : {FormatBytes(totalBytes)}");
                    Console.WriteLine($"Free RAM  : {FormatBytes(freeBytes)}");
                    Console.WriteLine($"Used RAM  : {FormatBytes(usedBytes)}");
                    return;
                }

                Console.WriteLine("No memory info found (WMI returned 0 results).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Memory info failed (WMI).");
                Console.WriteLine($"Reason: {ex.Message}");
            }
        }

        static void PrintDiskInfo()
        {
            PrintSection("Disk / Drive Info (System.IO.DriveInfo)");

            try
            {
                var drives = DriveInfo.GetDrives();

                foreach (var d in drives)
                {
                    Console.WriteLine($"Drive: {d.Name}  Type: {d.DriveType}");

                    if (!d.IsReady)
                    {
                        Console.WriteLine("  Status : Not ready");
                        continue;
                    }

                    Console.WriteLine($"  Label  : {d.VolumeLabel}");
                    Console.WriteLine($"  Format : {d.DriveFormat}");
                    Console.WriteLine($"  Total  : {FormatBytes((ulong)d.TotalSize)}");
                    Console.WriteLine($"  Free   : {FormatBytes((ulong)d.AvailableFreeSpace)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Disk info failed.");
                Console.WriteLine($"Reason: {ex.Message}");
            }
        }

        static void PrintGpuInfo()
        {
            PrintSection("GPU Info (WMI: Win32_VideoController)");

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController");

                var gpus = searcher.Get().Cast<ManagementObject>().ToList();

                if (gpus.Count == 0)
                {
                    Console.WriteLine("No GPU info found (WMI returned 0 results).");
                    return;
                }

                int index = 1;
                foreach (var gpu in gpus)
                {
                    Console.WriteLine($"GPU #{index++}");
                    Console.WriteLine($"  Name         : {gpu["Name"] ?? "N/A"}");
                    Console.WriteLine($"  Driver       : {gpu["DriverVersion"] ?? "N/A"}");

                    // AdapterRAM may be null or not reliable on some drivers
                    ulong vram = ToUInt64(gpu["AdapterRAM"]);
                    Console.WriteLine($"  VRAM (approx): {(vram == 0 ? "N/A" : FormatBytes(vram))}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GPU info failed (WMI).");
                Console.WriteLine($"Reason: {ex.Message}");
            }
        }

        static void PrintNetworkInfo()
        {
            PrintSection("Network Adapters (System.Net.NetworkInformation)");

            try
            {
                var adapters = NetworkInterface.GetAllNetworkInterfaces()
                    .OrderBy(n => n.OperationalStatus)
                    .ThenBy(n => n.NetworkInterfaceType)
                    .ThenBy(n => n.Name)
                    .ToList();

                if (adapters.Count == 0)
                {
                    Console.WriteLine("No network adapters found.");
                    return;
                }

                foreach (var nic in adapters)
                {
                    var props = nic.GetIPProperties();
                    var ips = props.UnicastAddresses
                        .Select(a => a.Address)
                        .Where(a => a != null)
                        .Select(a => a.ToString())
                        .ToList();

                    Console.WriteLine($"{nic.Name}");
                    Console.WriteLine($"  Type   : {nic.NetworkInterfaceType}");
                    Console.WriteLine($"  Status : {nic.OperationalStatus}");
                    Console.WriteLine($"  MAC    : {FormatMac(nic.GetPhysicalAddress())}");

                    if (ips.Count == 0)
                        Console.WriteLine("  IPs    : (none)");
                    else
                        Console.WriteLine($"  IPs    : {string.Join(", ", ips)}");

                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Network info failed.");
                Console.WriteLine($"Reason: {ex.Message}");
            }
        }

        // -------------------- Helpers --------------------

        static void PrintHeader(string title)
        {
            Console.WriteLine(new string('=', 70));
            Console.WriteLine(title);
            Console.WriteLine(new string('=', 70));
        }

        static void PrintSection(string title)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {title} ---");
        }

        static ulong ToUInt64(object? value)
        {
            if (value == null) return 0;
            try
            {
                if (value is ulong u) return u;
                if (value is long l && l >= 0) return (ulong)l;
                if (ulong.TryParse(value.ToString(), out var parsed)) return parsed;
            }
            catch { }
            return 0;
        }

        static string FormatBytes(ulong bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
            double size = bytes;
            int unit = 0;

            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return $"{size:0.##} {units[unit]}";
        }

        static string FormatMac(PhysicalAddress mac)
        {
            var bytes = mac.GetAddressBytes();
            if (bytes.Length == 0) return "N/A";
            return string.Join("-", bytes.Select(b => b.ToString("X2")));
        }
    }
}
