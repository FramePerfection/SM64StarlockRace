using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;

namespace SM64AppBase
{
    public struct ProcessEntry
    {
        public string name;
        public int pID;
        public ProcessEntry(string name, int ID)
        {
            this.name = name; this.pID = ID;
        }
        public override string ToString()
        {
            return pID + " - " + name;
        }
    }

    public class MemorySync
    {
        private int EmulationOffset = 0;

        public Process process = null;
        public List<ProcessEntry> availableProcesses = new List<ProcessEntry>();
        private IntPtr pID;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;

        private class moduleoffset
        {
            public int module;
            public int[] offset;
        }

        private class moduleinfo
        {
            public string name;
            public string version;
        }

        private static Dictionary<moduleinfo, moduleoffset> ModuleOffsets = new Dictionary<moduleinfo, moduleoffset>();
        private static string[] allowedProcesses = new string[] { "Project64"}; //, "mupen64-rerecording" };

        static MemorySync()
        {
            if (File.Exists("modules.cfg"))
            {
                StreamReader rd = new StreamReader("modules.cfg");
                while (!rd.EndOfStream)
                {
                    string line = rd.ReadLine();
                    if (line.Trim() == "") continue;
                    if (line.StartsWith("#Unsupported")) continue;
                    string[] split = line.Split(':');
                    if (split.Length >= 2)
                    {
                        moduleinfo info = new moduleinfo();
                        string[] iSplit = split[0].Split('|');
                        info.name = iSplit[0].Trim();
                        if (iSplit.Length > 1) info.version = iSplit[1].Trim();

                        moduleoffset d = new moduleoffset();
                        d.offset = new int[split.Length - 1];
                        for (int i = 1; i < split.Length; i++)
                            int.TryParse(split[i], NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out d.offset[i - 1]);
                        ModuleOffsets.Add(info, d);
                    }
                }
                rd.Close();
            }
        }

        public void SetProcess(Process p)
        {
            EmulationOffset = 0;
            process = p;
            foreach (KeyValuePair<moduleinfo, moduleoffset> supportedModule in ModuleOffsets)
                supportedModule.Value.module = 0;
        }

        public void Update()
        {
            availableProcesses.Clear();
            List<Process> allProcesses = new List<Process>(); ;
            foreach (string allowedProcess in allowedProcesses)
                foreach (Process p in Process.GetProcessesByName(allowedProcess))
                {
                    allProcesses.Add(p);
                    availableProcesses.Add(new ProcessEntry(p.ProcessName, p.Id));
                }

            if (process != null && process.HasExited)
            {
                EmulationOffset = 0;
                foreach (KeyValuePair<moduleinfo, moduleoffset> supportedModule in ModuleOffsets)
                    supportedModule.Value.module = 0;
                process = null;
                if (availableProcesses.Count > 0) process = Process.GetProcessById(availableProcesses[0].pID);
            }

            if (process != null)
            {
                try
                {
                    pID = OpenProcess(PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, process.Id);
                    foreach (ProcessModule m in process.Modules)
                    {
                        foreach (KeyValuePair<moduleinfo, moduleoffset> supportedModule in ModuleOffsets)
                            if (m.FileName.EndsWith(supportedModule.Key.name))
                                if (supportedModule.Key.version == null || m.FileVersionInfo.FileVersion == supportedModule.Key.version)
                                    supportedModule.Value.module = m.BaseAddress.ToInt32();
                    }
                    byte[] buffer = new byte[4];
                    int bytesRead = 0;
                    foreach (KeyValuePair<moduleinfo, moduleoffset> supportedModule in ModuleOffsets)
                        if (supportedModule.Value.module > 0)
                        {
                            int offset = supportedModule.Value.module;
                            for (int i = 0; i < supportedModule.Value.offset.Length; i++)
                            {
                                ReadProcessMemory((int)pID, offset + supportedModule.Value.offset[i], buffer, 4, ref bytesRead);
                                offset = BitConverter.ToInt32(buffer, 0);
                            }
                            if (offset > 0)
                            {
                                EmulationOffset = offset;
                                break;
                            }
                        }
                }
                catch
                {
                }
            }
            foreach (Process p in allProcesses)
                p.Dispose();
        }

        public byte[] ReadMemory(int lpBaseAddress, int count)
        {
            if (process == null)
                return new byte[count];
            byte[] buffer = new byte[count];
            int bytesRead = 0;
            ReadProcessMemory((int)pID, lpBaseAddress + EmulationOffset, buffer, count, ref bytesRead);
            return buffer;
        }

        public void WriteMemory(int lpBaseAddress, byte[] buffer)
        {
            if (process == null) return;
            int bytesWritten = 0;
            WriteProcessMemory((int)pID, lpBaseAddress + EmulationOffset, buffer, buffer.Length, ref bytesWritten);
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);
    }
}
