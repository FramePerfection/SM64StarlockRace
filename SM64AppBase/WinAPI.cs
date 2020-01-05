using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

#pragma warning disable 1591

namespace SM64AppBase
{
    using SizeT = UIntPtr;

    public enum MemPageState : uint
    {
        MEM_COMMIT = 0x1000,
        MEM_RESERVE = 0x2000,
        MEM_FREE = 0x10000,
    }

    public enum MemPageType : uint
    {
        MEM_PRIVATE = 0x20000,
        MEM_MAPPED = 0x40000,
        MEM_IMAGE = 0x1000000
    }

    [Flags]
    public enum MemPageProtect : uint
    {
        PAGE_NOACCESS = 0x01,
        PAGE_READONLY = 0x02,
        PAGE_READWRITE = 0x04,
        PAGE_WRITECOPY = 0x08,
        PAGE_EXECUTE = 0x10,
        PAGE_EXECUTE_READ = 0x20,
        PAGE_EXECUTE_READWRITE = 0x40,
        PAGE_EXECUTE_WRITECOPY = 0x80,
        PAGE_GUARD = 0x100,
        PAGE_NOCACHE = 0x200,
        PAGE_WRITECOMBINE = 0x400,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryBasicInformation // MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public MemPageProtect AllocationProtect;
        public SizeT RegionSize;
        public MemPageState State;
        public MemPageProtect Protect;
        public MemPageType Type;
    }

    public static class WinAPI
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer,
            SizeT nSize, out SizeT lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer,
            SizeT nSize, out SizeT lpNumberOfBytesWritten);

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumProcessModulesEx(IntPtr hProcess, [Out] IntPtr[] lphModule, uint cb,
            out uint lpcbNeeded, uint dwFilterFlag);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName,
            uint nSize);

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, [Out] out MODULEINFO lpmodinfo,
            uint cb);

        [DllImport("psapi.dll")]
        public static extern uint GetModuleBaseName(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName,
            uint nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process(IntPtr hProcess,
            [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SizeT VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress,
            [Out] out MemoryBasicInformation lpBuffer, SizeT dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, SizeT dwSize, uint flAllocationType,
            MemPageProtect flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, SizeT dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, SizeT dwSize,
            MemPageProtect flNewProtect, [Out] out MemPageProtect lpflOldProtect);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern IntPtr NtSuspendProcess(IntPtr hProcess);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern IntPtr NtResumeProcess(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, SizeT dwStackSize,
            IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEINFO
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }
        static bool is64BitProcess = (IntPtr.Size == 8);
        public static readonly bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
            {
                using (Process p = Process.GetCurrentProcess())
                {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal))
                    {
                        return false;
                    }
                    return retVal;
                }
            }
            else
            {
                return false;
            }
        }
    }

    public static class ProcessReader
    {
        public static bool ReadBytes(Process process, IntPtr addr, int count, out byte[] val)
        {
            var bytes = new byte[count];

            SizeT read;
            val = null;
            try
            {
                if (!WinAPI.ReadProcessMemory(process.Handle, addr, bytes, (SizeT)bytes.Length, out read)
                    || read != (SizeT)bytes.Length)
                    return false;
            }
            catch
            {
                return false;
            }

            val = bytes;

            return true;
        }

        public static bool ReadUInt32(Process process, IntPtr addr, out uint val)
        {
            byte[] arr;
            if (!ReadBytes(process, addr, 4, out arr))
            {
                val = 0;
                return false;
            }
            val = BitConverter.ToUInt32(arr, 0);
            return true;
        }

        public static bool ReadPointer(Process process, IntPtr addr, bool is64Bit, out IntPtr val)
        {
            var bytes = new byte[is64Bit ? 8 : 4];

            SizeT read;
            val = IntPtr.Zero;
            if (!WinAPI.ReadProcessMemory(process.Handle, addr, bytes, (SizeT)bytes.Length, out read)
                || read != (SizeT)bytes.Length)
                return false;

            val = is64Bit ? (IntPtr)BitConverter.ToInt64(bytes, 0) : (IntPtr)BitConverter.ToUInt32(bytes, 0);

            return true;
        }
    }
}
