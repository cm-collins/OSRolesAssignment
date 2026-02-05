using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Networking
{
    internal class Program
    {
        static async Task Main()
        {
            Console.Title = "OS Role: Internetworking / Networking";
            PrintHeader("INTERNETWORKING (C#) — Ping / DNS / HTTP / Active Connections");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Choose an option:");
                Console.WriteLine("  1) Show basic network status (interfaces)");
                Console.WriteLine("  2) Ping a host (ICMP)");
                Console.WriteLine("  3) DNS lookup (resolve a domain)");
                Console.WriteLine("  4) HTTP test (GET a URL and show status/time)");
                Console.WriteLine("  5) Show active TCP connections");
                Console.WriteLine("  6) Show TCP/UDP listeners (ports)");
                Console.WriteLine("  0) Exit");
                Console.Write("Enter choice: ");

                var choice = Console.ReadLine()?.Trim();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            ShowInterfaces();
                            break;
                        case "2":
                            await PingHost();
                            break;
                        case "3":
                            await DnsLookup();
                            break;
                        case "4":
                            await HttpTest();
                            break;
                        case "5":
                            ShowActiveTcpConnections();
                            break;
                        case "6":
                            ShowListeners();
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

        // -------------------- Option 1 --------------------
        static void ShowInterfaces()
        {
            PrintSection("Network Interfaces");

            var nics = NetworkInterface.GetAllNetworkInterfaces()
                .OrderByDescending(n => n.OperationalStatus == OperationalStatus.Up)
                .ThenBy(n => n.NetworkInterfaceType)
                .ThenBy(n => n.Name)
                .ToList();

            if (nics.Count == 0)
            {
                Console.WriteLine("No network interfaces found.");
                return;
            }

            foreach (var nic in nics)
            {
                var props = nic.GetIPProperties();
                var ipv4 = props.UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address.ToString())
                    .ToList();

                var ipv6 = props.UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    .Select(a => a.Address.ToString())
                    .ToList();

                Console.WriteLine($"{nic.Name}");
                Console.WriteLine($"  Type     : {nic.NetworkInterfaceType}");
                Console.WriteLine($"  Status   : {nic.OperationalStatus}");
                Console.WriteLine($"  Speed    : {FormatSpeed(nic.Speed)}");
                Console.WriteLine($"  MAC      : {FormatMac(nic.GetPhysicalAddress())}");
                Console.WriteLine($"  IPv4     : {(ipv4.Count == 0 ? "(none)" : string.Join(", ", ipv4))}");
                Console.WriteLine($"  IPv6     : {(ipv6.Count == 0 ? "(none)" : string.Join(", ", ipv6))}");
                Console.WriteLine();
            }

            Console.WriteLine("This demonstrates how the OS manages network devices/adapters.");
        }

        // -------------------- Option 2 --------------------
        static async Task PingHost()
        {
            PrintSection("Ping (ICMP)");

            Console.Write("Enter host (e.g., google.com or 8.8.8.8): ");
            var host = (Console.ReadLine() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(host))
            {
                Console.WriteLine("Host cannot be empty.");
                return;
            }

            using var ping = new Ping();

            // 4 attempts
            for (int i = 1; i <= 4; i++)
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    var reply = await ping.SendPingAsync(host, 2000);
                    sw.Stop();

                    if (reply.Status == IPStatus.Success)
                    {
                        Console.WriteLine($"Reply {i}: {reply.Address}  time={sw.ElapsedMilliseconds}ms  ttl={reply.Options?.Ttl}");
                    }
                    else
                    {
                        Console.WriteLine($"Reply {i}: {reply.Status}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reply {i}: Failed ({ex.Message})");
                }
            }

            Console.WriteLine();
            Console.WriteLine("This demonstrates basic internetwork diagnostics using the OS network stack.");
        }

        // -------------------- Option 3 --------------------
        static async Task DnsLookup()
        {
            PrintSection("DNS Lookup");

            Console.Write("Enter domain (e.g., microsoft.com): ");
            var domain = (Console.ReadLine() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(domain))
            {
                Console.WriteLine("Domain cannot be empty.");
                return;
            }

            try
            {
                var entry = await Dns.GetHostEntryAsync(domain);

                Console.WriteLine($"Host Name: {entry.HostName}");
                var addrs = entry.AddressList
                    .Select(a => a.ToString())
                    .ToList();

                Console.WriteLine("IP Addresses:");
                foreach (var a in addrs)
                    Console.WriteLine($"  {a}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("DNS lookup failed.");
                Console.WriteLine($"Reason: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("DNS resolution shows internetworking support provided by the OS.");
        }

        // -------------------- Option 4 --------------------
        static async Task HttpTest()
        {
            PrintSection("HTTP GET Test");

            Console.Write("Enter URL (default: https://example.com): ");
            var input = (Console.ReadLine() ?? "").Trim();

            var url = string.IsNullOrWhiteSpace(input) ? "https://example.com" : input;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                Console.WriteLine("Invalid URL.");
                return;
            }

            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            var sw = Stopwatch.StartNew();
            try
            {
                using var resp = await client.GetAsync(uri);
                sw.Stop();

                Console.WriteLine($"URL         : {uri}");
                Console.WriteLine($"Status      : {(int)resp.StatusCode} {resp.ReasonPhrase}");
                Console.WriteLine($"Time Taken  : {sw.ElapsedMilliseconds} ms");
                Console.WriteLine($"ContentType : {resp.Content.Headers.ContentType}");
                Console.WriteLine($"Length      : {(resp.Content.Headers.ContentLength?.ToString() ?? "unknown")} bytes");
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine("HTTP request failed.");
                Console.WriteLine($"Time Taken: {sw.ElapsedMilliseconds} ms");
                Console.WriteLine($"Reason: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("This demonstrates application-layer networking using the OS TCP/IP stack.");
        }

        // -------------------- Option 5 --------------------
        static void ShowActiveTcpConnections()
        {
            PrintSection("Active TCP Connections");

            var ipProps = IPGlobalProperties.GetIPGlobalProperties();
            var conns = ipProps.GetActiveTcpConnections()
                               .OrderBy(c => c.State)
                               .ThenBy(c => c.LocalEndPoint.Port)
                               .ToList();

            if (conns.Count == 0)
            {
                Console.WriteLine("No active TCP connections found.");
                return;
            }

            // Print top 30 to keep output screenshot-friendly
            Console.WriteLine("{0,-22} {1,-22} {2}",
                "Local", "Remote", "State");
            Console.WriteLine(new string('-', 70));

            foreach (var c in conns.Take(30))
            {
                Console.WriteLine("{0,-22} {1,-22} {2}",
                    EndPointToString(c.LocalEndPoint),
                    EndPointToString(c.RemoteEndPoint),
                    c.State);
            }

            Console.WriteLine();
            Console.WriteLine($"Total active TCP connections: {conns.Count}");
            Console.WriteLine("This shows how the OS tracks live network sessions.");
        }

        // -------------------- Option 6 --------------------
        static void ShowListeners()
        {
            PrintSection("TCP/UDP Listeners (Open Ports)");

            var ipProps = IPGlobalProperties.GetIPGlobalProperties();

            var tcp = ipProps.GetActiveTcpListeners()
                             .OrderBy(e => e.Port)
                             .ToList();

            var udp = ipProps.GetActiveUdpListeners()
                             .OrderBy(e => e.Port)
                             .ToList();

            Console.WriteLine("TCP Listening Ports (top 25):");
            if (tcp.Count == 0) Console.WriteLine("  (none)");
            else foreach (var ep in tcp.Take(25)) Console.WriteLine($"  {EndPointToString(ep)}");

            Console.WriteLine();
            Console.WriteLine("UDP Listening Ports (top 25):");
            if (udp.Count == 0) Console.WriteLine("  (none)");
            else foreach (var ep in udp.Take(25)) Console.WriteLine($"  {EndPointToString(ep)}");

            Console.WriteLine();
            Console.WriteLine($"Total TCP listeners: {tcp.Count}, Total UDP listeners: {udp.Count}");
            Console.WriteLine("This demonstrates port management by the OS networking subsystem.");
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

        static string FormatMac(PhysicalAddress mac)
        {
            var bytes = mac.GetAddressBytes();
            return bytes.Length == 0 ? "N/A" : string.Join("-", bytes.Select(b => b.ToString("X2")));
        }

        static string FormatSpeed(long bitsPerSecond)
        {
            if (bitsPerSecond <= 0) return "Unknown";
            double bps = bitsPerSecond;

            string[] units = { "bps", "Kbps", "Mbps", "Gbps" };
            int unit = 0;

            while (bps >= 1000 && unit < units.Length - 1)
            {
                bps /= 1000;
                unit++;
            }

            return $"{bps:0.##} {units[unit]}";
        }

        static string EndPointToString(IPEndPoint ep)
            => $"{ep.Address}:{ep.Port}";
    }
}
