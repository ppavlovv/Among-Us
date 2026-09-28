using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AmongUsExternal
{
    public class MemoryManager
    {
        #region Win32 API Imports
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x0001,
            CreateThread = 0x0002,
            VMOperation = 0x0008,
            VMRead = 0x0010,
            VMWrite = 0x0020,
            DupHandle = 0x0040,
            CreateProcess = 0x0080,
            SetQuota = 0x0100,
            SetInformation = 0x0200,
            QueryInformation = 0x0400,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
        #endregion

        private Process targetProcess;
        private IntPtr processHandle = IntPtr.Zero;
        public IntPtr GameAssemblyBase = IntPtr.Zero;
        public bool IsCompatibleBuild { get; private set; }
        public IntPtr UnityPlayerBase { get; private set; }
        public bool IsCompatibleEngine { get; private set; }

        public bool IsConnected
        {
            get
            {
                try
                {
                    return targetProcess != null && !targetProcess.HasExited && processHandle != IntPtr.Zero && GameAssemblyBase != IntPtr.Zero;
                }
                catch
                {
                    return false;
                }
            }
        }

        public int GetProcessId()
        {
            try
            {
                return targetProcess != null ? targetProcess.Id : 0;
            }
            catch { return 0; }
        }

        public bool Attach(string processName)
        {
            Detach();
            try
            {
                Process[] processes = Process.GetProcessesByName("Among Us");
                if (processes.Length == 0)
                {
                    processes = Process.GetProcessesByName("AmongUs");
                }
                if (processes.Length == 0 && !string.IsNullOrEmpty(processName))
                {
                    processes = Process.GetProcessesByName(processName);
                }

                if (processes.Length == 0)
                    return false;

                targetProcess = processes[0];
                processHandle = OpenProcess(ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VMRead
                    | ProcessAccessFlags.VMWrite | ProcessAccessFlags.VMOperation, false, targetProcess.Id);

                if (processHandle == IntPtr.Zero)
                {
                    Logger.LogError("OpenProcess basarisiz! Lutfen programi Yonetici Olarak calistirin.", null);
                    return false;
                }

                try
                {
                    foreach (ProcessModule module in targetProcess.Modules)
                    {
                        if (module.ModuleName.Equals(Offsets.GameModuleName, StringComparison.OrdinalIgnoreCase))
                        {
                            GameAssemblyBase = module.BaseAddress;
                            IsCompatibleBuild = IntPtr.Size == 8 && GameLayout.MatchesBinary(module.FileName);
                            Logger.LogInfo("Oyun surum profili: " + (IsCompatibleBuild ? "metadata 31 / SHA256 uyumlu" : "desteklenmiyor; yazma kapali"));
                        }
                        else if (module.ModuleName.Equals("UnityPlayer.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            UnityPlayerBase = module.BaseAddress;
                            IsCompatibleEngine = GameLayout.MatchesFile(module.FileName, CameraReader.EngineHash);
                        }
                    }
                }
                catch (Exception modEx)
                {
                    Logger.LogError("Modul listesi okunamadi (Yonetici yetkisi gerekebilir)", modEx);
                }

                return GameAssemblyBase != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Logger.LogError("Attach hatasi olustu", ex);
                return false;
            }
        }

        public void Detach()
        {
            GameAssemblyBase = IntPtr.Zero;
            IsCompatibleBuild = false;
            IsCompatibleEngine = false;
            UnityPlayerBase = IntPtr.Zero;
            if (processHandle != IntPtr.Zero)
            {
                CloseHandle(processHandle);
                processHandle = IntPtr.Zero;
            }
        }

        public byte[] ReadBytes(IntPtr address, int size)
        {
            byte[] buffer;
            TryReadBytes(address, size, out buffer);
            return buffer;
        }

        public bool TryReadBytes(IntPtr address, int size, out byte[] buffer)
        {
            buffer = new byte[size];
            IntPtr bytesRead;
            if (!ReadProcessMemory(processHandle, address, buffer, size, out bytesRead) || bytesRead.ToInt64() != size)
            {
                Array.Clear(buffer, 0, buffer.Length);
                return false;
            }
            return true;
        }

        public bool WriteBytes(IntPtr address, byte[] data)
        {
            if (!IsConnected || !GameLayout.IsSupportedWrite(this, address, data.Length)) return false;
            IntPtr bytesWritten;
            return WriteProcessMemory(processHandle, address, data, data.Length, out bytesWritten)
                && bytesWritten.ToInt64() == data.Length;
        }

        public IntPtr ReadPointer(IntPtr address)
        {
            byte[] buffer = ReadBytes(address, IntPtr.Size);
            IntPtr pointer = (IntPtr.Size == 8) ? (IntPtr)BitConverter.ToInt64(buffer, 0) : (IntPtr)BitConverter.ToInt32(buffer, 0);
            // Nonzero bytes are not sufficient evidence of a readable pointer.
            byte[] probe = new byte[1];
            IntPtr bytesRead;
            return pointer != IntPtr.Zero && ReadProcessMemory(processHandle, pointer, probe, 1, out bytesRead)
                && bytesRead.ToInt64() == 1 ? pointer : IntPtr.Zero;
        }

        public float ReadFloat(IntPtr address)
        {
            byte[] buffer = ReadBytes(address, 4);
            return BitConverter.ToSingle(buffer, 0);
        }

        public bool WriteFloat(IntPtr address, float value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool ReadBool(IntPtr address)
        {
            byte[] buffer = ReadBytes(address, 1);
            return buffer[0] != 0;
        }

        public bool WriteBool(IntPtr address, bool value)
        {
            return WriteBytes(address, new byte[] { (byte)(value ? 1 : 0) });
        }

        public int ReadInt(IntPtr address)
        {
            byte[] buffer = ReadBytes(address, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public bool WriteInt(IntPtr address, int value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public string ReadIl2CppString(IntPtr stringPointer)
        {
            if (stringPointer == IntPtr.Zero) return string.Empty;
            int length = ReadInt(IntPtr.Add(stringPointer, IntPtr.Size * 2));
            if (length <= 0 || length > 128) return string.Empty;

            byte[] buffer = ReadBytes(IntPtr.Add(stringPointer, IntPtr.Size * 2 + 4), length * 2);
            return Encoding.Unicode.GetString(buffer);
        }

        public IntPtr ResolvePointerChain(IntPtr baseAddress, params int[] offsets)
        {
            IntPtr current = baseAddress;
            for (int i = 0; i < offsets.Length; i++)
            {
                current = ReadPointer(current);
                if (current == IntPtr.Zero) return IntPtr.Zero;
                current = IntPtr.Add(current, offsets[i]);
            }
            return current;
        }
    }
}
