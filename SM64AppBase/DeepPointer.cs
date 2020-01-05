using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
#pragma warning disable 1591

// Note: Please be careful when modifying this because it could break existing components!

namespace SM64AppBase
{
    using OffsetT = Int32;

    public class ProcessModuleWow64Safe
    {

        public IntPtr BaseAddress { get; set; }
        public IntPtr EntryPointAddress { get; set; }
        public string FileName { get; set; }
        public int ModuleMemorySize { get; set; }
        public string ModuleName { get; set; }
        public FileVersionInfo FileVersionInfo
        {
            get { return FileVersionInfo.GetVersionInfo(FileName); }
        }
        public override string ToString()
        {
            return ModuleName ?? base.ToString();
        }

        public static bool Is64Bit(Process process)
        {
            bool procWow64;
            WinAPI.IsWow64Process(process.Handle, out procWow64);
            if (WinAPI.is64BitOperatingSystem && !procWow64)
                return true;
            return false;
        }

        private static Dictionary<int, ProcessModuleWow64Safe[]> ModuleCache = new Dictionary<int, ProcessModuleWow64Safe[]>();

        public static ProcessModuleWow64Safe MainModuleWow64Safe(Process p)
        {
            return ModulesWow64Safe(p)[0];
        }

        public static ProcessModuleWow64Safe[] ModulesWow64Safe(Process p)
        {
            if (ModuleCache.Count > 100)
                ModuleCache.Clear();

            const int LIST_MODULES_ALL = 3;
            const int MAX_PATH = 260;

            var hModules = new IntPtr[1024];

            uint cb = (uint)IntPtr.Size * (uint)hModules.Length;
            uint cbNeeded;

            if (!WinAPI.EnumProcessModulesEx(p.Handle, hModules, cb, out cbNeeded, LIST_MODULES_ALL))
                throw new Exception("Win32");
            uint numMods = cbNeeded / (uint)IntPtr.Size;

            int hash = p.StartTime.GetHashCode() + p.Id + (int)numMods;
            if (ModuleCache.ContainsKey(hash))
                return ModuleCache[hash];

            var ret = new List<ProcessModuleWow64Safe>();

            // everything below is fairly expensive, which is why we cache!
            for (int i = 0; i < numMods; i++)
            {
                var sb = new StringBuilder(MAX_PATH);
                if (WinAPI.GetModuleFileNameEx(p.Handle, hModules[i], sb, (uint)sb.Capacity) == 0)
                    throw new Exception("Win32");
                string fileName = sb.ToString();

                sb = new StringBuilder(MAX_PATH);
                if (WinAPI.GetModuleBaseName(p.Handle, hModules[i], sb, (uint)sb.Capacity) == 0)
                    throw new Exception("Win32");
                string baseName = sb.ToString();

                var moduleInfo = new WinAPI.MODULEINFO();
                if (!WinAPI.GetModuleInformation(p.Handle, hModules[i], out moduleInfo, (uint)Marshal.SizeOf(moduleInfo)))
                    throw new Exception("Win32");

                ret.Add(new ProcessModuleWow64Safe()
                {
                    FileName = fileName,
                    BaseAddress = moduleInfo.lpBaseOfDll,
                    ModuleMemorySize = (int)moduleInfo.SizeOfImage,
                    EntryPointAddress = moduleInfo.EntryPoint,
                    ModuleName = baseName
                });
            }

            ModuleCache.Add(hash, ret.ToArray());

            return ret.ToArray();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process(IntPtr hProcess,
            [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process);
    }

    public class DeepPointer
    {
        public readonly List<OffsetT> _offsets;
        public readonly OffsetT _base;
        private string _module;

        public DeepPointer(string module, OffsetT base_, params OffsetT[] offsets)
            : this(base_, offsets)
        {
            _module = module.ToLower();
        }

        public DeepPointer(OffsetT base_, params OffsetT[] offsets)
        {
            _base = base_;
            _offsets = new List<OffsetT>();
            _offsets.Add(0); // deref base first
            _offsets.AddRange(offsets);
        }

        public bool DerefInt(Process process, out int value)
        {
            uint tmp;
            IntPtr ptr;
            if (!DerefOffsets(process, out ptr)
                || !ProcessReader.ReadUInt32(process, ptr, out tmp))
            {
                value = 0;
                return false;
            }
            value = (int)tmp;
            return true;
        }

        public byte[] DerefBytes(Process process, int count)
        {
            byte[] bytes;
            if (!DerefBytes(process, count, out bytes))
                bytes = null;
            return bytes;
        }

        public bool DerefBytes(Process process, int count, out byte[] value)
        {
            IntPtr ptr;
            if (!DerefOffsets(process, out ptr)
                || !ProcessReader.ReadBytes(process, ptr, count, out value))
            {
                value = null;
                return false;
            }

            return true;
        }

        public bool DerefOffsets(Process process, out IntPtr ptr)
        {
            bool is64Bit = ProcessModuleWow64Safe.Is64Bit(process);

            if (!string.IsNullOrEmpty(_module))
            {
                ProcessModuleWow64Safe[] modules = ProcessModuleWow64Safe.ModulesWow64Safe(process);
                ProcessModuleWow64Safe module = null;
                foreach (var mod in modules)
                    if (mod.ModuleName.ToLower() == _module)
                        module = mod;
                if (module == null)
                {
                    ptr = IntPtr.Zero;
                    return false;
                }

                ptr = (IntPtr)((uint)module.BaseAddress + _base);
            }
            else
            {
                ptr = (IntPtr)((uint)ProcessModuleWow64Safe.MainModuleWow64Safe(process).BaseAddress + _base);
            }


            for (int i = 0; i < _offsets.Count - 1; i++)
            {
                if (!ProcessReader.ReadPointer(process, (IntPtr)((uint)ptr + _offsets[i]), is64Bit, out ptr)
                    || ptr == IntPtr.Zero)
                {
                    return false;
                }
            }

            ptr = (IntPtr)((uint)ptr + _offsets[_offsets.Count - 1]);
            return true;
        }
    }
}
