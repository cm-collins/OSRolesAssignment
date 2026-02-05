using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace MemoryMonitoring
{
    internal class Program
    {
        // Keep allocated blocks here so they stay in memory until user frees them.
        private static readonly List<byte[]> Allocations = new();

        static void Main()
        {
            Console.Title = "OS Role: Memory Management / Monitoring";
            PrintHeader("MEMORY MANAGEMENT / MONITORING (C#) — RAM Status / Process Memory / Allocation / GC");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Choose an option:");
                Console.WriteLine("  1) Show system RAM status (Total/Available/Used)");
                Console.WriteLine("  2) Show current process memory (Working Set / Private / GC heap)");
                Console.WriteLine("  3) List top 15 processes by RAM");
                Console.WriteLine("  4) Allocate memory (MB) and hold it");
                Console.WriteLine("  5) Free all allocated memory");
                Console.WriteLine("  6) Force GC (Garbage Collection) and show before/after");
                Console.WriteLine("  7) Monitor memory for 10 seconds (system + this process)");
                Console.WriteLine("  0) Exit");
                Console.Write("Enter choice: ");

                var choice = Console.ReadLine()?.Trim();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            ShowSystemMemory();
                            break;
                        case "2":
                            ShowCurrentProcessMemory();
                            break;
                        case "3":
                            ListTopProcessesByRam(15);
                            break;
                        case "4":
                            AllocateMemory();
                            break;
                        case "5":
                            FreeAllAllocations();
                            break;
                        case "6":
                            ForceGcDemo();
                            break;
                        case "7":
                            MonitorMemory(10);
                            break;
                        case "0":
                            Console.WriteLine("Goodbye.");
                            return;
                        default:
                            Console.WriteLine("Invalid choice. Try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Operation failed.");
                    Console.WriteLine($"Reason: {ex.Message}");
                }
            }
        }

        // -------------------- 1) System Memory (Windows API) --------------------
        static void ShowSystemMemory()
        {
            PrintSection("System RAM Status (GlobalMemoryStatusEx)");

            var mem = GetMemoryStatus();

            ulong total = mem.ullTotalPhys;
            ulong avail = mem.ullAvailPhys;
            ulong used = total > avail ? (total - avail) : 0;

            Console.WriteLine($"Total Physical RAM : {FormatBytes(total)}");
            Console.WriteLine($"Available RAM      : {FormatBytes(avail)}");
            Console.WriteLine($"Used RAM (approx)  : {FormatBytes(used)}");
            Console.WriteLine($"Memory Load        : {mem.dwMemoryLoad}%");
            Console.WriteLine();
            Console.WriteLine("This demonstrates OS-provided memory statistics (like Task Manager shows).");
        }

        // -------------------- 2) Current Process Memory --------------------
        static void ShowCurrentProcessMemory()
        {
            PrintSection("Current Process Memory");

            using var p = Process.GetCurrentProcess();
            p.Refresh();

            Console.WriteLine($"Process Name       : {p.ProcessName}");
            Console.WriteLine($"PID                : {p.Id}");
            Console.WriteLine($"Working Set (RAM)  : {FormatBytes((ulong)p.WorkingSet64)}");
            Console.WriteLine($"Private Memory     : {FormatBytes((ulong)p.PrivateMemorySize64)}");
            Console.WriteLine($"Paged Memory       : {FormatBytes((ulong)p.PagedMemorySize64)}");

            long gcHeap = GC.GetTotalMemory(forceFullCollection: false);
            Console.WriteLine($"GC Heap (managed)  : {FormatBytes((ulong)gcHeap)}");

            var info = GC.GetGCMemoryInfo();
            Console.WriteLine($"Heap Size (approx) : {FormatBytes((ulong)info.HeapSizeBytes)}");
            Console.WriteLine($"Committed (approx) : {FormatBytes((ulong)info.TotalCommittedBytes)}");

            Console.WriteLine();
            Console.WriteLine($"Held Allocations   : {Allocations.Count} block(s)");
            Console.WriteLine($"Held Alloc Size    : {FormatBytes((ulong)Allocations.Sum(a => (long)a.Length))}");
        }

        // -------------------- 3) Top Processes by RAM --------------------
        static void ListTopProcessesByRam(int top)
        {
            PrintSection($"Top {top} Processes by RAM (Working Set)");

            var list = Process.GetProcesses()
                .Select(p => new
                {
                    Proc = p,
                    Ram = SafeGetWorkingSet(p),
                    Name = SafeGetName(p)
                })
                .OrderByDescending(x => x.Ram)
                .Take(top)
                .ToList();

            Console.WriteLine("{0,-8} {1,-28} {2,12}", "PID", "Name", "RAM");
            Console.WriteLine(new string('-', 55));

            foreach (var x in list)
            {
                Console.WriteLine("{0,-8} {1,-28} {2,12}",
                    x.Proc.Id, Truncate(x.Name, 28), FormatBytes((ulong)x.Ram));
            }

            Console.WriteLine();
            Console.WriteLine("This demonstrates memory usage tracking across processes.");
        }

        // -------------------- 4) Allocate Memory --------------------
        static void AllocateMemory()
        {
            PrintSection("Allocate Memory");

            Console.Write("Enter MB to allocate (e.g., 50): ");
            var input = Console.ReadLine()?.Trim();

            if (!int.TryParse(input, out int mb) || mb <= 0)
            {
                Console.WriteLine("Invalid number.");
                return;
            }

            // Limit to prevent accidental huge allocations
            if (mb > 1024)
            {
                Console.WriteLine("Too large. Enter 1024 MB or less.");
                return;
            }

            int bytes = mb * 1024 * 1024;
            var block = new byte[bytes];

            // Touch some bytes so the OS commits pages (helps show change in RAM)
            for (int i = 0; i < block.Length; i += 4096)
                block[i] = 1;

            Allocations.Add(block);

            Console.WriteLine($"Allocated and held: {mb} MB");
            Console.WriteLine($"Total held blocks  : {Allocations.Count}");
            Console.WriteLine($"Total held size    : {FormatBytes((ulong)Allocations.Sum(a => (long)a.Length))}");
            Console.WriteLine();
            Console.WriteLine("This simulates memory allocation that an OS must manage and track.");
        }

        // -------------------- 5) Free Allocations --------------------
        static void FreeAllAllocations()
        {
            PrintSection("Free All Allocations");

            ulong before = (ulong)Allocations.Sum(a => (long)a.Length);
            Allocations.Clear();

            Console.WriteLine($"Freed held memory references: {FormatBytes(before)}");
            Console.WriteLine("Note: RAM may not drop immediately until GC runs.");
        }

        // -------------------- 6) Force GC Demo --------------------
        static void ForceGcDemo()
        {
            PrintSection("Force Garbage Collection (GC)");

            Console.WriteLine("Before GC:");
            ShowCurrentProcessMemory();

            Console.WriteLine();
            Console.WriteLine("Running GC.Collect()...");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Console.WriteLine();
            Console.WriteLine("After GC:");
            ShowCurrentProcessMemory();

            Console.WriteLine();
            Console.WriteLine("This demonstrates memory reclamation (similar to freeing memory in OS concepts).");
        }

        // -------------------- 7) Monitor Memory --------------------
        static void MonitorMemory(int seconds)
        {
            PrintSection($"Live Monitor ({seconds} seconds)");

            Console.WriteLine("{0,-4} {1,12} {2,12} {3,12} {4,12}",
                "Sec", "SysUsed", "SysAvail", "ProcWS", "GCHeap");
            Console.WriteLine(new string('-', 62));

            for (int sec = 1; sec <= seconds; sec++)
            {
                var mem = GetMemoryStatus();
                ulong total = mem.ullTotalPhys;
                ulong avail = mem.ullAvailPhys;
                ulong used = total > avail ? total - avail : 0;

                using var p = Process.GetCurrentProcess();
                p.Refresh();

                ulong procWs = (ulong)p.WorkingSet64;
                ulong gcHeap = (ulong)GC.GetTotalMemory(false);

                Console.WriteLine("{0,-4} {1,12} {2,12} {3,12} {4,12}",
                    sec,
                    ShortBytes(used),
                    ShortBytes(avail),
                    ShortBytes(procWs),
                    ShortBytes(gcHeap));

                Thread.Sleep(1000);
            }

            Console.WriteLine();
            Console.WriteLine("Tip: Run option 4 (allocate) then option 7 to show the change clearly.");
        }

        // -------------------- Windows API: GlobalMemoryStatusEx --------------------
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        private static MEMORYSTATUSEX GetMemoryStatus()
        {
            var mem = new MEMORYSTATUSEX();
            mem.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

            if (!GlobalMemoryStatusEx(ref mem))
                throw new InvalidOperationException("GlobalMemoryStatusEx failed.");

            return mem;
        }

        // -------------------- Helpers --------------------
        static void PrintHeader(string title)
        {
            Console.WriteLine(new string('=', 78));
            Console.WriteLine(title);
            Console.WriteLine(new string('=', 78));
        }

        static void PrintSection(string title)
        {
            Console.WriteLine($"--- {title} ---");
        }

        static long SafeGetWorkingSet(Process p)
        {
            try { return p.WorkingSet64; } catch { return 0; }
        }

        static string SafeGetName(Process p)
        {
            try { return p.ProcessName; } catch { return "N/A"; }
        }

        static string Truncate(string s, int max)
            => s.Length <= max ? s : s.Substring(0, max - 1) + "…";

        static string FormatBytes(ulong bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unit = 0;

            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return $"{size:0.##} {units[unit]}";
        }

        // Shorter formatting for monitoring table (e.g., 812MB, 1.2GB)
        static string ShortBytes(ulong bytes)
        {
            double mb = bytes / 1024.0 / 1024.0;
            if (mb < 1024) return $"{mb:0}MB";
            double gb = mb / 1024.0;
            return $"{gb:0.0}GB";
        }
    }
}
