using System;
using System.IO;
using System.Linq;

namespace FileManagement
{
    internal class Program
    {
        // Demo root folder (safe area)
        private static readonly string DemoRoot =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                         "OSRolesAssignmentDemo",
                         "FileManagement");

        static void Main()
        {
            Console.Title = "OS Role: File Management";
            EnsureDemoRoot();

            PrintHeader("FILE MANAGEMENT (C#) — Create / Read / Write / List / Copy / Move / Delete / Info");
            Console.WriteLine($"Demo Root Folder: {DemoRoot}");
            Console.WriteLine("All operations are restricted to this folder for safety.");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Choose an option:");
                Console.WriteLine("  1) Create a folder");
                Console.WriteLine("  2) Create a file");
                Console.WriteLine("  3) Write/Append text to a file");
                Console.WriteLine("  4) Read a file");
                Console.WriteLine("  5) List folder contents");
                Console.WriteLine("  6) Copy a file");
                Console.WriteLine("  7) Move/Rename a file");
                Console.WriteLine("  8) Delete a file");
                Console.WriteLine("  9) Show file info (size, dates, attributes)");
                Console.WriteLine(" 10) Set/Clear attributes (ReadOnly/Hidden)");
                Console.WriteLine(" 11) Search files by name pattern (e.g. *.txt)");
                Console.WriteLine("  0) Exit");
                Console.Write("Enter choice: ");

                var choice = Console.ReadLine()?.Trim();

                try
                {
                    Console.WriteLine();
                    switch (choice)
                    {
                        case "1": CreateFolder(); break;
                        case "2": CreateFile(); break;
                        case "3": WriteAppendFile(); break;
                        case "4": ReadFile(); break;
                        case "5": ListContents(); break;
                        case "6": CopyFile(); break;
                        case "7": MoveFile(); break;
                        case "8": DeleteFile(); break;
                        case "9": ShowFileInfo(); break;
                        case "10": SetClearAttributes(); break;
                        case "11": SearchFiles(); break;
                        case "0": Console.WriteLine("Goodbye."); return;
                        default: Console.WriteLine("Invalid choice. Try again."); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Operation failed.");
                    Console.WriteLine($"Reason: {ex.Message}");
                }
            }
        }

        // -------------------- Core Operations --------------------

        static void CreateFolder()
        {
            PrintSection("Create Folder");
            string relative = ReadRelativePath("Enter folder name (e.g., MyFolder): ", allowEmpty: false);
            string fullPath = SafePath(relative);

            Directory.CreateDirectory(fullPath);
            Console.WriteLine($"Created/Exists: {fullPath}");
        }

        static void CreateFile()
        {
            PrintSection("Create File");
            string relative = ReadRelativePath("Enter file name (e.g., notes.txt): ", allowEmpty: false);
            string fullPath = SafePath(relative);

            EnsureParentDirectory(fullPath);

            if (File.Exists(fullPath))
            {
                Console.WriteLine("File already exists.");
                return;
            }

            File.WriteAllText(fullPath, "");
            Console.WriteLine($"Created: {fullPath}");
        }

        static void WriteAppendFile()
        {
            PrintSection("Write/Append Text");
            string relative = ReadRelativePath("Enter file name (e.g., notes.txt): ", allowEmpty: false);
            string fullPath = SafePath(relative);

            EnsureParentDirectory(fullPath);

            Console.Write("Enter text (single line): ");
            string? text = Console.ReadLine();

            Console.Write("Append (A) or Overwrite (O)? [A/O]: ");
            string mode = (Console.ReadLine() ?? "A").Trim().ToUpperInvariant();

            if (mode == "O")
            {
                File.WriteAllText(fullPath, text ?? "");
                Console.WriteLine("Overwritten successfully.");
            }
            else
            {
                File.AppendAllText(fullPath, (text ?? "") + Environment.NewLine);
                Console.WriteLine("Appended successfully.");
            }

            Console.WriteLine($"Saved to: {fullPath}");
        }

        static void ReadFile()
        {
            PrintSection("Read File");
            string relative = ReadRelativePath("Enter file name (e.g., notes.txt): ", allowEmpty: false);
            string fullPath = SafePath(relative);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine("File not found.");
                return;
            }

            Console.WriteLine("---- FILE CONTENT START ----");
            Console.WriteLine(File.ReadAllText(fullPath));
            Console.WriteLine("---- FILE CONTENT END ----");
        }

        static void ListContents()
        {
            PrintSection("List Folder Contents");
            string relative = ReadRelativePath("Enter folder (blank for root): ", allowEmpty: true);
            string folder = string.IsNullOrWhiteSpace(relative) ? DemoRoot : SafePath(relative);

            if (!Directory.Exists(folder))
            {
                Console.WriteLine("Folder not found.");
                return;
            }

            var dirs = Directory.GetDirectories(folder).Select(Path.GetFileName).OrderBy(x => x).ToList();
            var files = Directory.GetFiles(folder).Select(Path.GetFileName).OrderBy(x => x).ToList();

            Console.WriteLine($"Folder: {folder}");
            Console.WriteLine();

            Console.WriteLine("Directories:");
            if (dirs.Count == 0) Console.WriteLine("  (none)");
            else foreach (var d in dirs) Console.WriteLine($"  [D] {d}");

            Console.WriteLine();
            Console.WriteLine("Files:");
            if (files.Count == 0) Console.WriteLine("  (none)");
            else foreach (var f in files) Console.WriteLine($"  [F] {f}");
        }

        static void CopyFile()
        {
            PrintSection("Copy File");
            string srcRel = ReadRelativePath("Source file (e.g., notes.txt): ", allowEmpty: false);
            string dstRel = ReadRelativePath("Destination file (e.g., copy.txt): ", allowEmpty: false);

            string src = SafePath(srcRel);
            string dst = SafePath(dstRel);

            if (!File.Exists(src))
            {
                Console.WriteLine("Source file not found.");
                return;
            }

            EnsureParentDirectory(dst);

            File.Copy(src, dst, overwrite: true);
            Console.WriteLine($"Copied:\n  From: {src}\n  To  : {dst}");
        }

        static void MoveFile()
        {
            PrintSection("Move/Rename File");
            string srcRel = ReadRelativePath("Source file (e.g., notes.txt): ", allowEmpty: false);
            string dstRel = ReadRelativePath("New name/path (e.g., archive\\notes2.txt): ", allowEmpty: false);

            string src = SafePath(srcRel);
            string dst = SafePath(dstRel);

            if (!File.Exists(src))
            {
                Console.WriteLine("Source file not found.");
                return;
            }

            EnsureParentDirectory(dst);

            if (File.Exists(dst))
                File.Delete(dst);

            File.Move(src, dst);
            Console.WriteLine($"Moved/Renamed:\n  From: {src}\n  To  : {dst}");
        }

        static void DeleteFile()
        {
            PrintSection("Delete File");
            string rel = ReadRelativePath("Enter file to delete (e.g., notes.txt): ", allowEmpty: false);
            string fullPath = SafePath(rel);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine("File not found.");
                return;
            }

            Console.Write("Type YES to confirm delete: ");
            var confirm = Console.ReadLine()?.Trim();

            if (!string.Equals(confirm, "YES", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Cancelled.");
                return;
            }

            File.SetAttributes(fullPath, FileAttributes.Normal); // in case it's ReadOnly/Hidden
            File.Delete(fullPath);
            Console.WriteLine("Deleted successfully.");
        }

        static void ShowFileInfo()
        {
            PrintSection("File Info");
            string rel = ReadRelativePath("Enter file name (e.g., notes.txt): ", allowEmpty: false);
            string fullPath = SafePath(rel);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine("File not found.");
                return;
            }

            var info = new FileInfo(fullPath);
            Console.WriteLine($"Path       : {info.FullName}");
            Console.WriteLine($"Size       : {FormatBytes((ulong)info.Length)}");
            Console.WriteLine($"Created    : {info.CreationTime}");
            Console.WriteLine($"Modified   : {info.LastWriteTime}");
            Console.WriteLine($"Accessed   : {info.LastAccessTime}");
            Console.WriteLine($"Attributes : {File.GetAttributes(fullPath)}");
        }

        static void SetClearAttributes()
        {
            PrintSection("Set/Clear Attributes");
            string rel = ReadRelativePath("Enter file name (e.g., notes.txt): ", allowEmpty: false);
            string fullPath = SafePath(rel);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine("File not found.");
                return;
            }

            var current = File.GetAttributes(fullPath);
            Console.WriteLine($"Current attributes: {current}");
            Console.WriteLine("  1) Toggle ReadOnly");
            Console.WriteLine("  2) Toggle Hidden");
            Console.Write("Choose: ");
            var choice = Console.ReadLine()?.Trim();

            FileAttributes updated = current;

            if (choice == "1")
                updated = Toggle(updated, FileAttributes.ReadOnly);
            else if (choice == "2")
                updated = Toggle(updated, FileAttributes.Hidden);
            else
            {
                Console.WriteLine("Invalid choice.");
                return;
            }

            File.SetAttributes(fullPath, updated);
            Console.WriteLine($"Updated attributes: {File.GetAttributes(fullPath)}");
        }

        static void SearchFiles()
        {
            PrintSection("Search Files");
            Console.Write("Enter pattern (e.g., *.txt or notes*.*): ");
            var pattern = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(pattern)) pattern = "*.*";

            var files = Directory.GetFiles(DemoRoot, pattern, SearchOption.AllDirectories)
                                 .OrderBy(f => f)
                                 .ToList();

            Console.WriteLine($"Found {files.Count} file(s):");
            foreach (var f in files)
            {
                // show relative path for neat output
                Console.WriteLine("  " + Path.GetRelativePath(DemoRoot, f));
            }
        }

        // -------------------- Safety + Helpers --------------------

        static void EnsureDemoRoot()
        {
            Directory.CreateDirectory(DemoRoot);
        }

        static string ReadRelativePath(string prompt, bool allowEmpty)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (allowEmpty && string.IsNullOrWhiteSpace(input))
                return "";

            if (!allowEmpty && string.IsNullOrWhiteSpace(input))
                throw new InvalidOperationException("Path cannot be empty.");

            // Normalize slashes
            return (input ?? "").Replace('/', Path.DirectorySeparatorChar).Trim();
        }

        static string SafePath(string relative)
        {
            // Combine and then full-resolve
            string combined = Path.Combine(DemoRoot, relative);
            string full = Path.GetFullPath(combined);

            // Prevent escaping the demo root via ..\..\ etc.
            string rootFull = Path.GetFullPath(DemoRoot) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid path: must stay inside the demo root folder.");

            return full;
        }

        static void EnsureParentDirectory(string filePath)
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
        }

        static FileAttributes Toggle(FileAttributes current, FileAttributes flag)
            => current.HasFlag(flag) ? (current & ~flag) : (current | flag);

        static void PrintHeader(string title)
        {
            Console.WriteLine(new string('=', 76));
            Console.WriteLine(title);
            Console.WriteLine(new string('=', 76));
        }

        static void PrintSection(string title)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {title} ---");
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
    }
}
