# OS Roles Simulator (C# / .NET 10)

A set of C# console programs that demonstrate **five key roles of an Operating System** on Windows:

1. **Device Management / System Information**
2. **Process Management**
3. **File Management**
4. **Internetworking / Networking**
5. **Memory Management / Monitoring**

These are user-space implementations (not kernel code) designed for an Operating Systems assignment.

---

## Project Structure

This solution contains multiple console projects (one per OS role):

- **DeviceSystemInfo** — Reads system/device info (CPU, RAM, disks, GPU, network adapters)
- **ProcessManagement** — Lists processes, starts apps, views details, changes priority, terminates, monitors CPU/RAM
- **FileManagement** — Create/read/write/list/copy/move/delete files + view metadata + file attributes
- **Networking** — Interfaces, ping, DNS lookup, HTTP test, active TCP connections, open listeners
- **MemoryMonitoring** — System RAM stats, process memory, allocation/free demo, GC demo, live monitor

---

## Requirements

- **Windows 10/11**
- **Visual Studio 2022+**
- **.NET 10 SDK**
- Target Framework: `net10.0-windows` for all projects

> Notes:
> - Some features may require **Run as Administrator** (especially process priority/termination on protected processes).
> - Ping (ICMP) can be blocked by firewall/network rules; DNS/HTTP options should still work.

---

## Setup & Run

1. Clone the repository:
   ```bash
   git clone <your-repo-url>
