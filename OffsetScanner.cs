using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace AmongUsExternal
{
    /// <summary>
    /// GameAssembly.dll GERÇEK Pattern Scanner
    /// DLL'i memory'den veya dosyadan okuyup, byte patterns ile gerçek offset'leri bulur
    /// </summary>
    public class OffsetScanner
    {
        private byte[] dllData;
        private string dllPath;
        private MemoryManager memoryManager;

        // PE Header bilgileri
        private long peHeaderOffset;
        private long textSectionStart;
        private long textSectionSize;
        private long dataSectionStart;
        private long dataSectionSize;
        private long rdataSectionStart;
        private long rdataSectionSize;

        public bool DllLoaded { get { return dllData != null && dllData.Length > 0; } }

        public OffsetScanner(string dllPath)
        {
            this.dllPath = dllPath;
            LoadDll();
        }

        public OffsetScanner(MemoryManager memory)
        {
            this.memoryManager = memory;
            // Memory'den okuyacak
        }

        private void LoadDll()
        {
            try
            {
                if (File.Exists(dllPath))
                {
                    this.dllData = File.ReadAllBytes(dllPath);
                    Logger.LogInfo(string.Format("GameAssembly.dll yuklendi: {0:N0} bytes", dllData.Length));
                    ParsePEHeader();
                }
                else
                {
                    Logger.LogError("GameAssembly.dll bulunamadi: " + dllPath, null);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("DLL okuma hatasi", ex);
            }
        }

        /// <summary>
        /// PE Header'ı parse et - section'ları bul
        /// </summary>
        private void ParsePEHeader()
        {
            try
            {
                if (dllData.Length < 0x100) return;

                // DOS header'da PE offset (e_lfanew) at 0x3C
                peHeaderOffset = BitConverter.ToInt32(dllData, 0x3C);
                if (peHeaderOffset <= 0 || peHeaderOffset >= dllData.Length - 4) return;

                // PE signature check "PE\0\0"
                if (dllData[peHeaderOffset] != 'P' || dllData[peHeaderOffset + 1] != 'E') return;

                // COFF Header
                long coffHeader = peHeaderOffset + 4;
                short numberOfSections = BitConverter.ToInt16(dllData, (int)(coffHeader + 2));
                short sizeOfOptionalHeader = BitConverter.ToInt16(dllData, (int)(coffHeader + 16));

                // Section headers başlangıcı
                long sectionHeaderStart = coffHeader + 20 + sizeOfOptionalHeader;

                Logger.LogInfo(string.Format("PE parsed: {0} sections", numberOfSections));

                for (int i = 0; i < numberOfSections; i++)
                {
                    long sectionOffset = sectionHeaderStart + (i * 40);
                    string sectionName = Encoding.ASCII.GetString(dllData, (int)sectionOffset, 8).TrimEnd('\0');
                    int virtualSize = BitConverter.ToInt32(dllData, (int)(sectionOffset + 8));
                    int virtualAddress = BitConverter.ToInt32(dllData, (int)(sectionOffset + 12));
                    int rawSize = BitConverter.ToInt32(dllData, (int)(sectionOffset + 16));
                    int rawOffset = BitConverter.ToInt32(dllData, (int)(sectionOffset + 20));

                    Logger.LogInfo(string.Format("  Section '{0}': VA=0x{1:X}, Size=0x{2:X}", sectionName, virtualAddress, virtualSize));

                    if (sectionName == ".text")
                    {
                        textSectionStart = rawOffset;
                        textSectionSize = rawSize;
                    }
                    else if (sectionName == ".data")
                    {
                        dataSectionStart = rawOffset;
                        dataSectionSize = rawSize;
                    }
                    else if (sectionName == ".rdata")
                    {
                        rdataSectionStart = rawOffset;
                        rdataSectionSize = rawSize;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("PE Header parse hatasi", ex);
            }
        }

        /// <summary>
        /// Byte pattern arama (wildcard destekli)
        /// Örnek: "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20"
        /// </summary>
        public long FindPattern(string pattern, long startOffset = 0, long endOffset = -1)
        {
            if (dllData == null) return -1;

            byte[] patternBytes;
            bool[] mask;
            ParsePattern(pattern, out patternBytes, out mask);

            if (endOffset == -1 || endOffset > dllData.Length)
                endOffset = dllData.Length;

            for (long i = startOffset; i < endOffset - patternBytes.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < patternBytes.Length; j++)
                {
                    if (mask[j] && dllData[i + j] != patternBytes[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }

        private void ParsePattern(string pattern, out byte[] bytes, out bool[] mask)
        {
            string[] tokens = pattern.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bytes = new byte[tokens.Length];
            mask = new bool[tokens.Length];

            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] == "??" || tokens[i] == "?")
                {
                    bytes[i] = 0x00;
                    mask[i] = false; // wildcard
                }
                else
                {
                    bytes[i] = Convert.ToByte(tokens[i], 16);
                    mask[i] = true;
                }
            }
        }

        /// <summary>
        /// String'i .rdata section'ında bul, sonra .text'te bu string'e referans veren kodu bul
        /// </summary>
        public long FindStringReference(string searchString)
        {
            if (dllData == null) return -1;

            try
            {
                byte[] strBytes = Encoding.UTF8.GetBytes(searchString + "\0");
                long strOffset = -1;

                // .rdata section'ında string ara
                for (long i = rdataSectionStart; i < rdataSectionStart + rdataSectionSize - strBytes.Length; i++)
                {
                    bool match = true;
                    for (int j = 0; j < strBytes.Length; j++)
                    {
                        if (dllData[i + j] != strBytes[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        strOffset = i;
                        break;
                    }
                }

                if (strOffset == -1) return -1;

                Logger.LogInfo(string.Format("String '{0}' found at offset 0x{1:X}", searchString, strOffset));
                return strOffset;
            }
            catch (Exception ex)
            {
                Logger.LogError("String reference bulma hatasi", ex);
                return -1;
            }
        }

        /// <summary>
        /// Bilinen Il2Cpp class pattern'leri ile PlayerControl vb. class'ları bul
        /// Bu Il2Cpp Metadata olmadan yapılan bir best-effort scan
        /// </summary>
        public bool AutoScanOffsets()
        {
            if (dllData == null)
            {
                Logger.LogError("DLL yuklenmemis, offset taramasi yapilamaz", null);
                return false;
            }

            Logger.LogInfo("=== Otomatik Offset Taramasi Basladi ===");
            bool anyFound = false;

            try
            {
                // Il2Cpp string dumper pattern - "PlayerControl" string'ini bulup
                // ona olan referansları .text section'ında ara
                long playerControlStr = FindStringReference("PlayerControl");
                if (playerControlStr > 0)
                {
                    Logger.LogInfo(string.Format("[✓] 'PlayerControl' string bulundu: 0x{0:X}", playerControlStr));
                    anyFound = true;
                }

                long gameOptionsStr = FindStringReference("GameOptionsData");
                if (gameOptionsStr > 0)
                {
                    Logger.LogInfo(string.Format("[✓] 'GameOptionsData' string bulundu: 0x{0:X}", gameOptionsStr));
                    anyFound = true;
                }

                long amongUsClientStr = FindStringReference("AmongUsClient");
                if (amongUsClientStr > 0)
                {
                    Logger.LogInfo(string.Format("[✓] 'AmongUsClient' string bulundu: 0x{0:X}", amongUsClientStr));
                    anyFound = true;
                }

                // IL2Cpp metadata pointer pattern
                // Il2CppClass yapısı: PlayerControl için tipik pattern
                // Il2CppClass in .data section, referenced from .text
                // Pattern: 48 8B 05 [4 byte RVA] (mov rax, [rip+offset])
                long classPattern = FindPattern("48 8B 05 ?? ?? ?? ?? 48 85 C0 74 ?? 48 8B 40 ??");
                if (classPattern > 0)
                {
                    Logger.LogInfo(string.Format("[✓] Il2CppClass pattern bulundu: 0x{0:X}", classPattern));
                    anyFound = true;
                }

                Logger.LogInfo("=== Tarama Tamamlandi ===");

                if (!anyFound)
                {
                    Logger.LogInfo("[!] Hicbir pattern bulunamadi. DLL sikistirilmis veya sifreli olabilir.");
                    Logger.LogInfo("[!] Manuel offset gerekli - IDA Pro veya Il2CppDumper kullanin.");
                }

                return anyFound;
            }
            catch (Exception ex)
            {
                Logger.LogError("Auto scan hatasi", ex);
                return false;
            }
        }

        /// <summary>
        /// Config dosyasından offset yükle (kullanıcı manuel girmişse)
        /// </summary>
        public bool LoadFromConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "offsets.json");
            if (!File.Exists(configPath))
            {
                Logger.LogInfo("offsets.json bulunamadi, varsayilan degerler kullaniliyor");
                return false;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                Logger.LogInfo("Custom offsetler yuklendi (offsets.json)");

                // Basit JSON parse (external dependency olmadan)
                ParseSimpleJson(json);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Config yukleme hatasi", ex);
                return false;
            }
        }

        private void ParseSimpleJson(string json)
        {
            // Basit key:value parser - tam JSON parser değil ama basit config için yeter
            string[] lines = json.Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim().TrimEnd(',');
                if (!trimmed.Contains(":") || trimmed.StartsWith("//")) continue;

                int colonIdx = trimmed.IndexOf(':');
                string key = trimmed.Substring(0, colonIdx).Trim().Trim('"');
                string value = trimmed.Substring(colonIdx + 1).Trim().Trim('"').TrimEnd(',');

                if (value.StartsWith("0x") || value.StartsWith("0X"))
                {
                    try
                    {
                        long parsed = Convert.ToInt64(value.Substring(2), 16);
                        ApplyOffset(key, parsed);
                    }
                    catch { }
                }
                else
                {
                    int intVal;
                    if (int.TryParse(value, out intVal))
                    {
                        ApplyOffset(key, intVal);
                    }
                }
            }
        }

        private void ApplyOffset(string key, long value)
        {
            switch (key)
            {
                case "PlayerControl_Class": Offsets.PlayerControl_Class = new IntPtr(value); break;
                case "GameOptionsData_Class": Offsets.GameOptionsData_Class = new IntPtr(value); break;
                case "AmongUsClient_Class": Offsets.AmongUsClient_Class = new IntPtr(value); break;
                case "PlayerControl_LocalPlayer": Offsets.PlayerControl_LocalPlayer = (int)value; break;
                case "PlayerControl_AllPlayers": Offsets.PlayerControl_AllPlayers = (int)value; break;
                case "PlayerControl_MyData": Offsets.PlayerControl_MyData = (int)value; break;
                case "PlayerControl_MyPhysics": Offsets.PlayerControl_MyPhysics = (int)value; break;
                case "PlayerControl_NetTransform": Offsets.PlayerControl_NetTransform = (int)value; break;
                case "PlayerControl_KillTimer": Offsets.PlayerControl_KillTimer = (int)value; break;
                case "PlayerData_IsImpostor": Offsets.PlayerData_IsImpostor = (int)value; break;
                case "PlayerData_IsDead": Offsets.PlayerData_IsDead = (int)value; break;
                case "GameOptions_PlayerSpeedMod": Offsets.GameOptions_PlayerSpeedMod = (int)value; break;
                case "GameOptions_CrewLightMod": Offsets.GameOptions_CrewLightMod = (int)value; break;
                case "GameOptions_ImpostorLightMod": Offsets.GameOptions_ImpostorLightMod = (int)value; break;
                case "GameOptions_KillCooldown": Offsets.GameOptions_KillCooldown = (int)value; break;
            }
            Logger.LogInfo(string.Format("Offset uygulandi: {0} = 0x{1:X}", key, value));
        }

        /// <summary>
        /// Bilinen v18e (build 7250) offsetlerini yükle - fallback
        /// </summary>
        public void ApplyKnownOffsets_v18e()
        {
            try
            {
                // Bu değerler tahmin ve sürüme göre değişebilir!
                // Gerçek çalışması için offsets.json'a manuel gir veya AutoScan kullan
                Offsets.PlayerControl_Class = new IntPtr(0x03018B40);
                Offsets.GameOptionsData_Class = new IntPtr(0x03018BA0);
                Offsets.AmongUsClient_Class = new IntPtr(0x03018A80);

                Offsets.PlayerControl_LocalPlayer = 0x5C;
                Offsets.PlayerControl_AllPlayers = 0x60;
                Offsets.PlayerControl_PlayerId = 0x08;
                Offsets.PlayerControl_MyData = 0x34;
                Offsets.PlayerControl_MyPhysics = 0x38;
                Offsets.PlayerControl_NetTransform = 0x3C;
                Offsets.PlayerControl_KillTimer = 0x44;
                Offsets.PlayerControl_InVent = 0x48;

                Offsets.PlayerData_PlayerName = 0x0C;
                Offsets.PlayerData_ColorId = 0x10;
                Offsets.PlayerData_HatId = 0x14;
                Offsets.PlayerData_PetId = 0x18;
                Offsets.PlayerData_SkinId = 0x1C;
                Offsets.PlayerData_IsDead = 0x29;
                Offsets.PlayerData_IsImpostor = 0x28;

                Offsets.PlayerPhysics_Speed = 0x14;
                Offsets.PlayerPhysics_Collider = 0x18;
                Offsets.Collider_Enabled = 0x18;

                Offsets.NetTransform_PrevPos = 0x2C;
                Offsets.NetTransform_TargetPos = 0x34;

                Offsets.GameOptions_PlayerSpeedMod = 0x10;
                Offsets.GameOptions_CrewLightMod = 0x14;
                Offsets.GameOptions_ImpostorLightMod = 0x18;
                Offsets.GameOptions_KillCooldown = 0x20;
                Offsets.GameOptions_KillDistance = 0x24;
                Offsets.GameOptions_NumImpostors = 0x08;

                Logger.LogInfo("[FALLBACK] v18e (build 7250) offsetleri yuklendi");
                Logger.LogInfo("[!] Bu offsetler tahmini - calismiyorsa offsets.json'a manuel gir!");
            }
            catch (Exception ex)
            {
                Logger.LogError("Offset yükleme hatası", ex);
            }
        }

        /// <summary>
        /// Runtime'da offset'lerin çalışıp çalışmadığını doğrula
        /// </summary>
        public OffsetVerificationResult VerifyOffsets(MemoryManager memory)
        {
            OffsetVerificationResult result = new OffsetVerificationResult();

            if (!memory.IsConnected)
            {
                result.OverallStatus = "Bellek bagli degil";
                return result;
            }

            try
            {
                IntPtr playerControl = GameLayout.GetClass(memory, GameLayout.PlayerControlRva, "PlayerControl");
                result.PlayerControlValid = playerControl != IntPtr.Zero;
                result.PlayerControlAddress = playerControl;

                IntPtr gameOptions = GameLayout.GetOptions(memory);
                result.GameOptionsValid = gameOptions != IntPtr.Zero;
                result.GameOptionsAddress = gameOptions;

                if (result.PlayerControlValid)
                {
                    IntPtr staticFields = GameLayout.GetPlayerStatics(memory);
                    if (staticFields != IntPtr.Zero)
                    {
                        IntPtr localPlayer = GameLayout.GetLocalPlayer(memory);
                        result.LocalPlayerValid = localPlayer != IntPtr.Zero;
                        result.LocalPlayerAddress = localPlayer;
                    }
                }

                if (result.PlayerControlValid && result.GameOptionsValid && result.LocalPlayerValid)
                {
                    result.OverallStatus = "Adresler okunabiliyor; ozellik uyumlulugu henuz dogrulanmadi";
                }
                else if (result.PlayerControlValid)
                {
                    result.OverallStatus = "KISMEN CALISIYOR (offsetler eskimis olabilir)";
                }
                else
                {
                    result.OverallStatus = "Surum veya sinif uyumsuz; bellek yazma kullanilamaz";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Offset dogrulama hatasi", ex);
                result.OverallStatus = "HATA: " + ex.Message;
            }

            return result;
        }
    }

    /// <summary>
    /// GameAssembly.dll içindeki section bilgisi (runtime'da PE header'dan okunur)
    /// </summary>
    public class ModuleSection
    {
        public string Name;
        public long VirtualAddress;
        public long VirtualSize;
        public long End { get { return VirtualAddress + VirtualSize; } }
        public override string ToString()
        {
            return string.Format("[{0}] VA=0x{1:X} Size=0x{2:X}", Name, VirtualAddress, VirtualSize);
        }
    }

    /// <summary>
    /// Brute-force offset bulucu - runtime'da gerçek offset'i tespit eder
    /// </summary>
    public class BruteForceOffsetFinder
    {
        private MemoryManager memory;
        public List<ModuleSection> Sections = new List<ModuleSection>();

        public BruteForceOffsetFinder(MemoryManager memory)
        {
            this.memory = memory;
        }

        /// <summary>
        /// Runtime'da GameAssembly.dll'in PE header'ını memory'den oku, section'ları çıkar
        /// </summary>
        public bool ReadPESections(Action<string> callback)
        {
            Sections.Clear();
            try
            {
                if (!memory.IsConnected) return false;
                long baseAddr = memory.GameAssemblyBase.ToInt64();
                if (baseAddr == 0) return false;

                // DOS header: e_lfanew at offset 0x3C
                int peOffset = memory.ReadInt(new IntPtr(baseAddr + 0x3C));
                if (peOffset <= 0 || peOffset > 0x1000)
                {
                    if (callback != null) callback("HATA: PE offset gecersiz");
                    return false;
                }

                long peHeader = baseAddr + peOffset;

                // PE signature check "PE\0\0" = 0x00004550
                int sig = memory.ReadInt(new IntPtr(peHeader));
                if (sig != 0x00004550)
                {
                    if (callback != null) callback(string.Format("HATA: PE signature yok (0x{0:X8})", sig));
                    return false;
                }

                // COFF Header (24 bytes after "PE\0\0")
                byte[] coff = memory.ReadBytes(new IntPtr(peHeader + 4), 20);
                short numSections = BitConverter.ToInt16(coff, 2);
                short sizeOfOptHeader = BitConverter.ToInt16(coff, 16);

                if (callback != null)
                    callback(string.Format("PE OK: {0} sections, opt.hdr={1} bytes", numSections, sizeOfOptHeader));

                // Section table starts after: PE(4) + COFF(20) + OptHeader
                long sectionTableStart = peHeader + 4 + 20 + sizeOfOptHeader;

                for (int i = 0; i < numSections && i < 32; i++)
                {
                    long sh = sectionTableStart + (i * 40);
                    byte[] header = memory.ReadBytes(new IntPtr(sh), 40);
                    if (header.Length < 40) break;

                    string name = Encoding.ASCII.GetString(header, 0, 8).TrimEnd('\0', ' ');
                    int virtualSize = BitConverter.ToInt32(header, 8);
                    int virtualAddress = BitConverter.ToInt32(header, 12);

                    ModuleSection sec = new ModuleSection
                    {
                        Name = name,
                        VirtualAddress = virtualAddress,
                        VirtualSize = virtualSize
                    };
                    Sections.Add(sec);

                    if (callback != null)
                        callback("  " + sec.ToString());
                }

                return Sections.Count > 0;
            }
            catch (Exception ex)
            {
                if (callback != null) callback("PE parse hatasi: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Il2Cpp class pointer'larının bulunduğu section'ları döndür (.data, .rdata, il2cpp)
        /// </summary>
        public List<ModuleSection> GetDataSections()
        {
            List<ModuleSection> result = new List<ModuleSection>();
            foreach (ModuleSection s in Sections)
            {
                if (s.Name == ".data" || s.Name == ".rdata" || s.Name == "il2cpp" ||
                    s.Name == ".bss" || s.Name.StartsWith("il2cpp"))
                {
                    result.Add(s);
                }
            }
            return result;
        }

        /// <summary>
        /// Bir pointer'ın Windows heap'inde valid olup olmadığını kontrol et
        /// </summary>
        private bool IsValidHeapPointer(IntPtr ptr)
        {
            long addr = ptr.ToInt64();
            // Windows x64 user-mode heap: 0x00000001_00000000 - 0x00007FFF_FFFFFFFF
            // Il2Cpp allocated: usually 0x00000001XXXXXXXX or 0x000001XXXXXXXX
            // Module base: 0x00007FFXXXXXXXX
            if (addr < 0x10000) return false;
            if (addr > 0x00007FFFFFFFFFFF) return false;
            // Aligned to 8 bytes (64-bit pointer)
            if ((addr & 0x7) != 0) return false;
            return true;
        }

        /// <summary>
        /// PlayerControl_Class offset'ini brute-force ile bul
        /// Doğrulama: LocalPlayer -> MyData -> PlayerName okunabilir olmalı
        /// </summary>
        public long FindPlayerControlClass(long startOffset, long endOffset, Action<string> progressCallback)
        {
            if (!memory.IsConnected)
            {
                if (progressCallback != null) progressCallback("HATA: Bellek bagli degil!");
                return -1;
            }

            long baseAddr = memory.GameAssemblyBase.ToInt64();
            if (baseAddr == 0)
            {
                if (progressCallback != null) progressCallback("HATA: GameAssembly.dll base adresi 0!");
                return -1;
            }

            if (progressCallback != null)
                progressCallback(string.Format("Brute force tarama basladi...\r\nBase: 0x{0:X}\r\nAralik: 0x{1:X} - 0x{2:X}\r\n",
                    baseAddr, startOffset, endOffset));

            int attempted = 0;
            int validPointers = 0;
            long lastProgressOffset = startOffset;

            // GameAssembly.dll'in .data section'ı genelde büyük.
            // Il2Cpp class pointer'ları .data içinde bulunur.
            // Adım 8 byte (aligned pointer)
            for (long offset = startOffset; offset <= endOffset; offset += 8)
            {
                attempted++;

                // Her 500K offset'te progress göster
                if (offset - lastProgressOffset > 0x80000)
                {
                    lastProgressOffset = offset;
                    if (progressCallback != null)
                        progressCallback(string.Format("...taraniyor: 0x{0:X} ({1} denendi, {2} pointer bulundu)",
                            offset, attempted, validPointers));
                }

                try
                {
                    IntPtr addr = new IntPtr(baseAddr + offset);
                    IntPtr classPtr = memory.ReadPointer(addr);

                    if (!IsValidHeapPointer(classPtr)) continue;
                    validPointers++;

                    // Il2CppClass has static fields pointer at 0xB8 in modern Unity
                    // But in older Il2Cpp it's at 0x5C. We try both.
                    long[] staticFieldOffsets = new long[] { 0x5C, 0xB8, 0xB0, 0xC8 };
                    long[] localPlayerOffsets = new long[] { 0x5C, 0x0, 0x8, 0x10 };
                    long[] myDataOffsets = new long[] { 0x34, 0x38, 0x30, 0x40, 0x50 };

                    foreach (long sfOffset in staticFieldOffsets)
                    {
                        IntPtr staticFields = memory.ReadPointer(IntPtr.Add(classPtr, (int)sfOffset));
                        if (!IsValidHeapPointer(staticFields)) continue;

                        foreach (long lpOffset in localPlayerOffsets)
                        {
                            IntPtr localPlayer = memory.ReadPointer(IntPtr.Add(staticFields, (int)lpOffset));
                            if (!IsValidHeapPointer(localPlayer)) continue;

                            foreach (long mdOffset in myDataOffsets)
                            {
                                IntPtr myData = memory.ReadPointer(IntPtr.Add(localPlayer, (int)mdOffset));
                                if (!IsValidHeapPointer(myData)) continue;

                                // PlayerName string pointer (usually at 0x0C in PlayerData)
                                for (int nameOffset = 0x08; nameOffset <= 0x20; nameOffset += 4)
                                {
                                    IntPtr namePtr = memory.ReadPointer(IntPtr.Add(myData, nameOffset));
                                    if (!IsValidHeapPointer(namePtr)) continue;

                                    // Il2Cpp String: [ptr_to_type][monitor][length][chars...]
                                    // Try to read length at 0x08
                                    try
                                    {
                                        int len = memory.ReadInt(IntPtr.Add(namePtr, 0x08));
                                        if (len < 1 || len > 24) continue;

                                        byte[] chars = memory.ReadBytes(IntPtr.Add(namePtr, 0x0C), len * 2);
                                        if (chars.Length < len * 2) continue;

                                        // ASCII karakter validation
                                        bool valid = true;
                                        for (int i = 0; i < len; i++)
                                        {
                                            char c = (char)BitConverter.ToInt16(chars, i * 2);
                                            if (!IsValidNameChar(c))
                                            {
                                                valid = false;
                                                break;
                                            }
                                        }

                                        if (valid)
                                        {
                                            string playerName = Encoding.Unicode.GetString(chars, 0, len * 2);

                                            // BULDUK!
                                            if (progressCallback != null)
                                                progressCallback(string.Format(
                                                    "\r\n*** BULUNDU! ***\r\n" +
                                                    "PlayerControl_Class Offset: 0x{0:X}\r\n" +
                                                    "StaticFields Offset: 0x{1:X}\r\n" +
                                                    "LocalPlayer Offset: 0x{2:X}\r\n" +
                                                    "MyData Offset: 0x{3:X}\r\n" +
                                                    "PlayerName Offset: 0x{4:X}\r\n" +
                                                    "Local Player Adi: '{5}'\r\n",
                                                    offset, sfOffset, lpOffset, mdOffset, nameOffset, playerName));

                                            // Offset'leri global olarak uygula
                                            Offsets.PlayerControl_Class = new IntPtr(offset);
                                            Offsets.PlayerControl_LocalPlayer = (int)lpOffset;
                                            Offsets.PlayerControl_MyData = (int)mdOffset;
                                            Offsets.PlayerData_PlayerName = nameOffset;

                                            Logger.LogInfo(string.Format(
                                                "AUTO-DETECTED offsets: PC_Class=0x{0:X} LP=0x{1:X} MyData=0x{2:X} Name=0x{3:X}",
                                                offset, lpOffset, mdOffset, nameOffset));

                                            return offset;
                                        }
                                    }
                                    catch { continue; }
                                }
                            }
                        }
                    }
                }
                catch { continue; }
            }

            if (progressCallback != null)
                progressCallback(string.Format("\r\nTarama tamamlandi.\r\nToplam: {0} adres denendi, {1} valid pointer bulundu.\r\nAncak PlayerControl tespit edilemedi.\r\n\r\nDenemek icin:\r\n1. Among Us'da oyuna gir (lobide dur)\r\n2. Karakter olustur ve isim ver\r\n3. Tekrar taramayi calistir",
                    attempted, validPointers));

            return -1;
        }

        private bool IsValidNameChar(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                   (c >= '0' && c <= '9') || c == ' ' || c == '_' ||
                   c == '-' || c == '.' || c == '!' || c == '?';
        }

        /// <summary>
        /// EN GÜÇLÜ TARAMA: Il2CppClass.name field'ını okuyarak class ismini tanır
        /// LocalPlayer olmadan bile çalışır - class isim tabanlı doğrulama
        /// </summary>
        public long FindClassByName(string targetName, long startOffset, long endOffset, Action<string> progressCallback)
        {
            if (!memory.IsConnected) return -1;
            long baseAddr = memory.GameAssemblyBase.ToInt64();
            if (baseAddr == 0) return -1;

            int attempted = 0;
            int validPointers = 0;
            long lastReport = startOffset;

            if (progressCallback != null)
                progressCallback(string.Format("[NAME SCAN] '{0}' araniyor: 0x{1:X} - 0x{2:X}",
                    targetName, startOffset, endOffset));

            // Il2CppClass name field'ının olası offset'leri (Unity versiyonlarına göre)
            int[] nameFieldOffsets = new int[] { 0x10, 0x18, 0x0, 0x8, 0x20 };

            for (long offset = startOffset; offset <= endOffset; offset += 8)
            {
                attempted++;

                if (offset - lastReport > 0x100000)
                {
                    lastReport = offset;
                    if (progressCallback != null)
                        progressCallback(string.Format("...0x{0:X} ({1} adres, {2} ptr)",
                            offset, attempted, validPointers));
                }

                try
                {
                    IntPtr classPtr = memory.ReadPointer(new IntPtr(baseAddr + offset));
                    if (!IsValidHeapPointer(classPtr)) continue;
                    validPointers++;

                    foreach (int nameOff in nameFieldOffsets)
                    {
                        IntPtr namePtr = memory.ReadPointer(IntPtr.Add(classPtr, nameOff));
                        if (!IsValidHeapPointer(namePtr)) continue;

                        string name = ReadCString(namePtr, 32);
                        if (string.IsNullOrEmpty(name)) continue;

                        if (name == targetName)
                        {
                            if (progressCallback != null)
                                progressCallback(string.Format(
                                    "\r\n*** '{0}' BULUNDU! ***\r\n" +
                                    "  Class Offset: 0x{1:X}\r\n" +
                                    "  Class Pointer: 0x{2:X}\r\n" +
                                    "  Name Field Offset: 0x{3:X}\r\n",
                                    targetName, offset, classPtr.ToInt64(), nameOff));
                            return offset;
                        }
                    }
                }
                catch { continue; }
            }

            if (progressCallback != null)
                progressCallback(string.Format("[NAME SCAN] '{0}' bulunamadi ({1} adres tarandi)",
                    targetName, attempted));
            return -1;
        }

        /// <summary>
        /// C-string (null-terminated ASCII) okuyucu
        /// </summary>
        private string ReadCString(IntPtr ptr, int maxLen)
        {
            try
            {
                byte[] buffer = memory.ReadBytes(ptr, maxLen);
                if (buffer == null || buffer.Length == 0) return string.Empty;

                int len = 0;
                while (len < buffer.Length && buffer[len] != 0) len++;
                if (len == 0 || len >= maxLen) return string.Empty;

                // Sadece printable ASCII kontrol
                for (int i = 0; i < len; i++)
                {
                    if (buffer[i] < 0x20 || buffer[i] > 0x7E) return string.Empty;
                }

                return Encoding.ASCII.GetString(buffer, 0, len);
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// Class bulunduktan sonra static field offset'ini tespit et
        /// </summary>
        public bool DetectStaticFieldsOffset(long classOffset, Action<string> progressCallback)
        {
            if (!memory.IsConnected) return false;
            long baseAddr = memory.GameAssemblyBase.ToInt64();

            try
            {
                IntPtr classPtr = memory.ReadPointer(new IntPtr(baseAddr + classOffset));
                if (!IsValidHeapPointer(classPtr)) return false;

                // Il2CppClass'ın static_fields offset'i (versiyona göre değişir):
                // v24: 0xB8, v27: 0xB0, v29: 0xB0, v31: 0x108
                int[] sfOffsets = new int[] { 0xB8, 0xB0, 0xC0, 0xC8, 0x5C, 0x108, 0xD0, 0xA8 };

                foreach (int sfOff in sfOffsets)
                {
                    IntPtr staticFields = memory.ReadPointer(IntPtr.Add(classPtr, sfOff));
                    if (!IsValidHeapPointer(staticFields)) continue;

                    // Static fields içinde ilk 200 byte'ta pointer benzeri bir alan var mı?
                    // (LocalPlayer, AllPlayers gibi)
                    bool hasPointers = false;
                    for (int i = 0; i < 200; i += 8)
                    {
                        try
                        {
                            IntPtr p = memory.ReadPointer(IntPtr.Add(staticFields, i));
                            if (IsValidHeapPointer(p))
                            {
                                hasPointers = true;
                                break;
                            }
                        }
                        catch { }
                    }

                    if (hasPointers)
                    {
                        if (progressCallback != null)
                            progressCallback(string.Format("  Static Fields Offset: 0x{0:X} ✓", sfOff));

                        // Update the effective Class offset to include static_fields navigation
                        // Save this info for later use
                        Offsets.PlayerControl_LocalPlayer = sfOff;
                        return true;
                    }
                }

                if (progressCallback != null)
                    progressCallback("  UYARI: Static fields tespit edilemedi");
                return false;
            }
            catch (Exception ex)
            {
                if (progressCallback != null)
                    progressCallback("  Hata: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// GameOptions class offset'ini de tara (KillCooldown float değerini kullanarak)
        /// </summary>
        public long FindGameOptionsClass(long startOffset, long endOffset, Action<string> progressCallback)
        {
            if (!memory.IsConnected) return -1;
            long baseAddr = memory.GameAssemblyBase.ToInt64();

            for (long offset = startOffset; offset <= endOffset; offset += 8)
            {
                try
                {
                    IntPtr addr = new IntPtr(baseAddr + offset);
                    IntPtr classPtr = memory.ReadPointer(addr);
                    if (!IsValidHeapPointer(classPtr)) continue;

                    // GameOptions static instance
                    IntPtr staticFields = memory.ReadPointer(IntPtr.Add(classPtr, 0x5C));
                    if (!IsValidHeapPointer(staticFields)) continue;

                    IntPtr instance = memory.ReadPointer(staticFields);
                    if (!IsValidHeapPointer(instance)) continue;

                    // KillCooldown genelde 15-45 arası float
                    float killCooldown = memory.ReadFloat(IntPtr.Add(instance, 0x20));
                    // PlayerSpeedMod genelde 0.5-3.0 arası float
                    float speedMod = memory.ReadFloat(IntPtr.Add(instance, 0x10));

                    if (killCooldown >= 10f && killCooldown <= 60f &&
                        speedMod >= 0.25f && speedMod <= 3.5f)
                    {
                        if (progressCallback != null)
                            progressCallback(string.Format(
                                "GameOptions BULUNDU: 0x{0:X} (KillCD={1:F1}, Speed={2:F2})",
                                offset, killCooldown, speedMod));
                        Offsets.GameOptionsData_Class = new IntPtr(offset);
                        return offset;
                    }
                }
                catch { continue; }
            }
            return -1;
        }
    }

    public class OffsetVerificationResult
    {
        public bool PlayerControlValid;
        public IntPtr PlayerControlAddress;
        public bool GameOptionsValid;
        public IntPtr GameOptionsAddress;
        public bool LocalPlayerValid;
        public IntPtr LocalPlayerAddress;
        public string OverallStatus = "Bilinmiyor";

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== OFFSET DOGRULAMA RAPORU ===");
            sb.AppendLine(string.Format("Genel Durum: {0}", OverallStatus));
            sb.AppendLine(string.Format("PlayerControl: {0} ({1})", PlayerControlValid ? "OK" : "HATA",
                PlayerControlAddress != IntPtr.Zero ? "0x" + PlayerControlAddress.ToInt64().ToString("X") : "0x0"));
            sb.AppendLine(string.Format("GameOptions: {0} ({1})", GameOptionsValid ? "OK" : "HATA",
                GameOptionsAddress != IntPtr.Zero ? "0x" + GameOptionsAddress.ToInt64().ToString("X") : "0x0"));
            sb.AppendLine(string.Format("LocalPlayer: {0} ({1})", LocalPlayerValid ? "OK" : "HATA",
                LocalPlayerAddress != IntPtr.Zero ? "0x" + LocalPlayerAddress.ToInt64().ToString("X") : "0x0"));
            return sb.ToString();
        }
    }
}
