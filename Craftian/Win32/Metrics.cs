using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Craftian.Win32
{
    public class Metrics
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SystemInfo
        {
            internal ProcessorInfo uProcessorInfo;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public int lpMaximumApplicationAddress;
            public IntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort dwProcessorLevel;
            public ushort dwProcessorRevision;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ProcessorInfo
        {
            [FieldOffset(0)]
            internal uint dwOemId;
            [FieldOffset(0)]
            internal ushort wProcessorArchitecture;
            [FieldOffset(2)]
            internal ushort wReserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MemoryStatusEx
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
            public MemoryStatusEx()
            {
                dwLength = (uint)Marshal.SizeOf(this);
            }
        }

        [return: MarshalAs(UnmanagedType.Struct)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void GetSystemInfo([In, Out] ref SystemInfo lpSystemInfo);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

        public async Task<SystemInfo> GetSystemInfo()
        {
            return await Task.Run(() =>
            {
                var sysInfo = new SystemInfo();
                GetSystemInfo(ref sysInfo);
                return sysInfo;
            });
        }

        public async Task<MemoryStatusEx> GetMemoryInfo()
        {
            return await Task.Run(() =>
            {
                var memInfo = new MemoryStatusEx();
                GlobalMemoryStatusEx(memInfo);
                return memInfo;
            });
        }

        public async Task<uint> CurrentClockSpeed()
        {
            return await Task.Run(() => {
                using (var mo = new ManagementObject("Win32_Processor.DeviceID='CPU0'"))
                    return (uint)mo["CurrentClockSpeed"];

            });
        }

        public async Task<uint> MaximumClockSpeed()
        {
            return await Task.Run(() => {
                using (var mo = new ManagementObject("Win32_Processor.DeviceID='CPU0'"))
                    return (uint)mo["MaxClockSpeed"];

            });
        }
    }
}
