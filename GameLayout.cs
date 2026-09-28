using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AmongUsExternal
{
    // Extracted from the installed metadata-v31 build, then checked by class name at runtime.
    public static class GameLayout
    {
        public const string BinaryHash = "3A86BF7D6E69594809D9BA9965F9B5BC4EB88934FDE1C05DBEC191899D1763DF";
        public const int StaticFields = 0xB8;
        public const int PlayerControlRva = 0x33B42B8;
        public const int OptionsManagerRva = 0x33CF828;
        public const int AmongUsClientRva = 0x338C770;

        public static int NetworkMode(MemoryManager memory)
        {
            IntPtr klass = GetClass(memory, AmongUsClientRva, "AmongUsClient");
            if (klass == IntPtr.Zero) return -1;
            IntPtr fields = memory.ReadPointer(IntPtr.Add(klass, StaticFields));
            IntPtr client = fields == IntPtr.Zero ? IntPtr.Zero : memory.ReadPointer(fields);
            byte[] bytes;
            return client != IntPtr.Zero && memory.TryReadBytes(IntPtr.Add(client, 0x44), 4, out bytes)
                ? BitConverter.ToInt32(bytes, 0) : -1;
        }

        public static bool MatchesBinary(string path)
        {
            return MatchesFile(path, BinaryHash);
        }

        public static bool MatchesFile(string path, string expectedHash)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "") == expectedHash;
        }

        public static string ClassName(MemoryManager memory, IntPtr klass)
        {
            if (klass == IntPtr.Zero) return string.Empty;
            IntPtr name = memory.ReadPointer(IntPtr.Add(klass, 0x10));
            if (name == IntPtr.Zero) return string.Empty;
            byte[] bytes = memory.ReadBytes(name, 96);
            int end = Array.IndexOf(bytes, (byte)0);
            return end < 0 ? string.Empty : Encoding.UTF8.GetString(bytes, 0, end);
        }

        public static IntPtr GetClass(MemoryManager memory, int rva, string expected)
        {
            if (!memory.IsConnected || !memory.IsCompatibleBuild) return IntPtr.Zero;
            IntPtr klass = memory.ReadPointer(IntPtr.Add(memory.GameAssemblyBase, rva));
            return ClassName(memory, klass) == expected ? klass : IntPtr.Zero;
        }

        public static IntPtr GetPlayerStatics(MemoryManager memory)
        {
            IntPtr klass = GetClass(memory, PlayerControlRva, "PlayerControl");
            return klass == IntPtr.Zero ? IntPtr.Zero : memory.ReadPointer(IntPtr.Add(klass, StaticFields));
        }

        public static IntPtr GetLocalPlayer(MemoryManager memory)
        {
            IntPtr fields = GetPlayerStatics(memory);
            IntPtr player = fields == IntPtr.Zero ? IntPtr.Zero : memory.ReadPointer(fields);
            return player != IntPtr.Zero && ClassName(memory, memory.ReadPointer(player)) == "PlayerControl"
                ? player : IntPtr.Zero;
        }

        public static IntPtr GetOptions(MemoryManager memory)
        {
            IntPtr klass = GetClass(memory, OptionsManagerRva, "GameOptionsManager");
            if (klass == IntPtr.Zero) return IntPtr.Zero;
            IntPtr fields = memory.ReadPointer(IntPtr.Add(klass, StaticFields));
            IntPtr manager = fields == IntPtr.Zero ? IntPtr.Zero : memory.ReadPointer(fields);
            if (manager == IntPtr.Zero) return IntPtr.Zero;
            IntPtr options = memory.ReadPointer(IntPtr.Add(manager, 0x28));
            return options != IntPtr.Zero && ClassName(memory, memory.ReadPointer(options)) == "NormalGameOptionsV11"
                ? options : IntPtr.Zero;
        }

        public static bool IsSupportedWrite(MemoryManager memory, IntPtr address, int length)
        {
            if (!memory.IsCompatibleBuild || length != 4) return false;
            IntPtr player = GetLocalPlayer(memory);
            if (player != IntPtr.Zero && address == IntPtr.Add(player, 0xB8)) return true;
            IntPtr options = GetOptions(memory);
            if (options == IntPtr.Zero) return false;
            long offset = address.ToInt64() - options.ToInt64();
            return offset == 0x20 || offset == 0x24 || offset == 0x28;
        }
    }
}
