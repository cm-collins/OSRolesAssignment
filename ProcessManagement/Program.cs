using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ProcessManagement
{
    internal class Program
    {
        static void Main()
        {
            Console.Title = "OS Role: Process Management";
            PrintHeader("PROCESS MANAGEMENT (C#) — List / Start / Inspect / Priority / Kill");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Choose an option:");
                Console.WriteLine("  1) List running processes (Top 20 by RAM)");
                Console.WriteLine("  2) View process details (by PID)");
                Console.WriteLine("  3) Start a process (Notepad / Calculator / Custom)");
                Console.WriteLine("  4) Change process priority (by PID)");
                Console.WriteLine("  5) Terminate a process (by PID)");
                Console.WriteLine("  6) Monitor a process for 10 seconds (CPU% + RAM)");
                Console.WriteLine("  0) Exit");
                Console.Write("Enter choice: ");

                var choice = Console.ReadLine()?.Trim();

                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        ListProcessesTopByMemory(20);
                        break;
                    case "2":
                        ViewProcessDetails();
                        break;
                    case "3":
                        StartProcessMenu();
                        break;
                    case "4":
                        ChangePriority();
                        break;
                    case "5":
                        TerminateProcess();
                        break;
                    case "6":
                        MonitorProcess();
                        break;
                    case "0":
                        Console.WriteLine("Goodbye.");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Try again.");
                        break;
                }
            }
        }

        // -------------------- Option 1 --------------------
        static void ListProcessesTopByMemory(int top)
        {
            PrintSection($"Running Processes (Top {top} by RAM)");

            var processes = Process.GetProcesses()
                .OrderByDescending(p => SafeGetWorkingSet(p))
                .Take(top)
                .ToList();

            Console.WriteLine("{0,-8} {1,-28} {2,10} {3,12}",
                "PID", "Name", "RAM(MB)", "Threads");

            Console.WriteLine(new string('-', 65));

            foreach (var p in processes)
            {
                long ram = SafeGetWorkingSet(p);
                int threads = SafeGetThreadCount(p);

                Console.WriteLine("{0,-8} {1,-28} {2,10:0.0} {3,12}",
                    p.Id, Truncate(p.ProcessName, 28), ram / 1024.0 / 1024.0, threads);
            }

            Console.WriteLine();
            Console.WriteLine("Tip: Use option 2 to inspect any PID in detail.");
        }

        // -------------------- Option 2 --------------------
        static void ViewProcessDetails()
        {
            PrintSection("Process Details");

            int pid = ReadPid();
            if (pid == -1) return;

            try
            {
                using var p = Process.GetProcessById(pid);

                Console.WriteLine($"Name            : {p.ProcessName}");
                Console.WriteLine($"PID             : {p.Id}");
                Console.WriteLine($"Responding      : {SafeGetResponding(p)}");
                Console.WriteLine($"Threads         : {SafeGetThreadCount(p)}");
                Console.WriteLine($"Handles         : {SafeGetHandleCount(p)}");
                Console.WriteLine($"Priority Class  : {SafeGetPriorityClass(p)}");
                Console.WriteLine($"Working Set RAM : {FormatBytes((ulong)SafeGetWorkingSet(p))}");
                Console.WriteLine($"Paged Memory    : {FormatBytes((ulong)SafeGetPagedMemory(p))}");
                Console.WriteLine($"CPU Time        : {SafeGetCpuTime(p)}");
                Console.WriteLine($"Start Time      : {SafeGetStartTime(p)}");
                Console.WriteLine($"Path            : {SafeGetMainModulePath(p)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not read process details.");
                Console.WriteLine($"Reason: {ex.Message}");
            }
        }

        // -------------------- Option 3 --------------------
        static void StartProcessMenu()
        {
            PrintSection("Start a Process");

            Console.WriteLine("  1) Notepad");
            Console.WriteLine("  2) Calculator");
            Console.WriteLine("  3) Custom (type an executable name/path)");
            Console.Write("Choose: ");

            var ch = Console.ReadLine()?.Trim();

            try
            {
                Process? started = ch switch
                {
                    "1" => Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true }),
                    "2" => Process.Start(new ProcessStartInfo("calc.exe") { UseShellExecute = true }),
                    "3" => StartCustom(),
                    _ => null
                };

                if (started == null)
                {
                    Console.WriteLine("No process started (invalid choice or cancelled).");
                    return;
                }

                Console.WriteLine($"Started: {started.ProcessName} (PID: {started.Id})");
                Console.WriteLine("This demonstrates process creation (a core OS role).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start process.");
                Console.WriteLine($"Reason: {ex.Message}");
            }
        }

        static Process? StartCustom()
        {
            Console.Write("Enter exe name or full path (e.g., mspaint.exe): ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
                return null;

            // UseShellExecute=true lets Windows resolve app paths more easily
            return Process.Start(new ProcessStartInfo(input) { UseShellExecute = true });
        }

        // -------------------- Option 4 --------------------
        static void ChangePriority()
        {
            PrintSection("Change Process Priority");

            int pid = ReadPid();
            if (pid == -1) return;

            Console.WriteLine("Priority options:");
            Console.WriteLine("  1) Idle");
            Console.WriteLine("  2) BelowNormal");
            Console.WriteLine("  3) Normal");
            Console.WriteLine("  4) AboveNormal");
            Console.WriteLine("  5) High");
            Console.Write("Choose: ");

            var choice = Console.ReadLine()?.Trim();

            var newPriority = choice switch
            {
                "1" => ProcessPriorityClass.Idle,
                "2" => ProcessPriorityClass.BelowNormal,
                "3" => ProcessPriorityClass.Normal,
                "4" => ProcessPriorityClass.AboveNormal,
                "5" => ProcessPriorityClass.High,
                _ => (ProcessPriorityClass?)null
            };

            if (newPriority == null)
            {
                Console.WriteLine("Invalid priority selection.");
                return;
            }

            try
            {
                using var p = Process.GetProcessById(pid);
                var old = p.PriorityClass;
                p.PriorityClass = newPriority.Value;

                Console.WriteLine($"Changed priority for {p.ProcessName} (PID {pid})");
                Console.WriteLine($"Old: {old}  ->  New: {p.PriorityClass}");
                Console.WriteLine("This demonstrates scheduling-related control (priority).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to change priority. (Some processes require admin rights.)");
                Console.WriteLine($"Reason: {ex.Message}");
            }
        }

        // -------------------- Option 5 --------------------
        static void TerminateProcess()
        {
            PrintSection("Terminate a Process");

            Console.WriteLine("⚠️ Only terminate processes you started (e.g., Notepad) to avoid crashing the system.");
            int pid = ReadPid();
            if (pid == -1) return;

            try
            {
                using var p = Process.GetProcessById(pid);

                Console.WriteLine($"Target: {p.ProcessName} (PID {pid})");
                Console.Write("Type YES to confirm termination: ");
                var confirm = Console.ReadLine()?.Trim();

                if (!string.Equals(confirm, "YES", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }

                p.Kill(entireProcessTree: true);
                p.WaitForExit(3000);

                Console.WriteLine("Process terminated successfully.");
                Console.WriteLine("This demonstrates process termination (a core OS function).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to terminate process.");
                Console.WriteLine($"Reason: {ex.Message}");
            }
        }

        // -------------------- Option 6 --------------------
        static void MonitorProcess()
        {
            PrintSection("Monitor Process (10 seconds)");

            int pid = ReadPid();
            if (pid == -1) return;

            try
            {
                using var p = Process.GetProcessById(pid);
                Console.WriteLine($"Monitoring: {p.ProcessName} (PID {pid})");
                Console.WriteLine("Sampling every 1 second...\n");

                // Baseline CPU time
                TimeSpan prevCpu = p.TotalProcessorTime;
                DateTime prevTime = DateTime.UtcNow;

                Console.WriteLine("{0,-6} {1,8} {2,12}", "Sec", "CPU(%)", "RAM");
                Console.WriteLine(new string('-', 32));

                for (int sec = 1; sec <= 10; sec++)
                {
                    Thread.Sleep(1000);

                    p.Refresh();

                    TimeSpan curCpu = SafeGetTotalProcessorTime(p);
                    DateTime curTime = DateTime.UtcNow;

                    double elapsedMs = (curTime - prevTime).TotalMilliseconds;
                    double cpuMs = (curCpu - prevCpu).TotalMilliseconds;

                    // Normalize by logical processors
                    double cpuPct = elapsedMs > 0
                        ? (cpuMs / (elapsedMs * Environment.ProcessorCount)) * 100.0
                        : 0.0;

                    long ram = SafeGetWorkingSet(p);

                    Console.WriteLine("{0,-6} {1,8:0.0} {2,12}", sec, Clamp(cpuPct, 0, 999), FormatBytes((ulong)ram));

                    prevCpu = curCpu;
                    prevTime = curTime;
                }

                Console.WriteLine("\nDone monitoring.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Monitoring failed.");
                Console.WriteLine($"Reason: {ex.Message}");
            }
        }

        // -------------------- Helpers --------------------
        static int ReadPid()
        {
            Console.Write("Enter PID: ");
            var input = Console.ReadLine()?.Trim();

            if (!int.TryParse(input, out int pid) || pid <= 0)
            {
                Console.WriteLine("Invalid PID.");
                return -1;
            }
            return pid;
        }

        static void PrintHeader(string title)
        {
            Console.WriteLine(new string('=', 72));
            Console.WriteLine(title);
            Console.WriteLine(new string('=', 72));
        }

        static void PrintSection(string title)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {title} ---");
        }

        static long SafeGetWorkingSet(Process p)
        {
            try { return p.WorkingSet64; } catch { return 0; }
        }

        static long SafeGetPagedMemory(Process p)
        {
            try { return p.PagedMemorySize64; } catch { return 0; }
        }

        static int SafeGetThreadCount(Process p)
        {
            try { return p.Threads.Count; } catch { return -1; }
        }

        static int SafeGetHandleCount(Process p)
        {
            try { return p.HandleCount; } catch { return -1; }
        }

        static string SafeGetResponding(Process p)
        {
            try { return p.Responding ? "Yes" : "No"; } catch { return "N/A"; }
        }

        static string SafeGetPriorityClass(Process p)
        {
            try { return p.PriorityClass.ToString(); } catch { return "N/A"; }
        }

        static string SafeGetStartTime(Process p)
        {
            try { return p.StartTime.ToString("yyyy-MM-dd HH:mm:ss"); } catch { return "Access denied / N/A"; }
        }

        static string SafeGetCpuTime(Process p)
        {
            try { return p.TotalProcessorTime.ToString(); } catch { return "N/A"; }
        }

        static TimeSpan SafeGetTotalProcessorTime(Process p)
        {
            try { return p.TotalProcessorTime; } catch { return TimeSpan.Zero; }
        }

        static string SafeGetMainModulePath(Process p)
        {
            try { return p.MainModule?.FileName ?? "N/A"; }
            catch { return "Access denied / N/A"; }
        }

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

        static string Truncate(string s, int max)
            => s.Length <= max ? s : s.Substring(0, max - 1) + "…";

        static double Clamp(double v, double min, double max)
            => v < min ? min : (v > max ? max : v);
    }
}
