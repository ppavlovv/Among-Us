using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AmongUsExternal.Features;

namespace AmongUsExternal
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=================================================");
                Console.WriteLine("          KV3 AMONG US TOOLS - ytpavlov          ");
                Console.WriteLine("=================================================");
                Console.WriteLine("[*] INSERT tusuna basarak menuyu acabilirsiniz");
                Console.WriteLine("[*] Oyun otomatik tespit edilecektir");
                Console.ResetColor();
            }
            catch { }

            try
            {
                // 1. OFFSET YÜKLEME STRATEJİSİ (Öncelik sırasıyla):
                //    1) offsets.json (kullanıcı tarafından girilmişse)
                //    2) Otomatik pattern scan (best-effort)
                //    3) Fallback: v18e known offsets
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[1/3] Offsetler yukleniyor...");
                Console.ResetColor();

                // DLL yolunu tespit et (birden fazla konumu dene)
                string dllPath = FindGameAssemblyDll();
                OffsetScanner scanner = new OffsetScanner(dllPath);

                Logger.LogInfo("Metadata 31 profili kullaniliyor; eski offsets.json uygulanmayacak.");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[+] Offset sistemi hazir!");
                Console.ResetColor();

                // 2. Bellek yöneticisi başlat
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[2/3] Bellek yoneticisi baslatiliyor...");
                Console.ResetColor();

                MemoryManager memory = new MemoryManager();

                // Arka planda bağlantı yönetimi thread'i
                Thread serviceThread = new Thread(delegate()
                {
                    int lastCheckPassed = -1;
                    while (true)
                    {
                        try
                        {
                            if (!memory.IsConnected)
                            {
                                bool attached = memory.Attach("Among Us");
                                if (attached && lastCheckPassed != 1)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine(string.Format("\n[+] Among Us'a baglanildi (PID: {0})", memory.GetProcessId()));
                                    Console.WriteLine(string.Format("[+] GameAssembly.dll Base: 0x{0:X}", memory.GameAssemblyBase.ToInt64()));
                                    Console.ResetColor();

                                    // Offset doğrulama
                                    OffsetVerificationResult verify = scanner.VerifyOffsets(memory);
                                    Console.ForegroundColor = verify.PlayerControlValid ? ConsoleColor.Green : ConsoleColor.Red;
                                    Console.WriteLine(verify.ToString());
                                    Console.ResetColor();

                                    lastCheckPassed = 1;
                                }
                                else if (!attached && lastCheckPassed != 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    Console.WriteLine("[.] Among Us bekleniyor...");
                                    Console.ResetColor();
                                    lastCheckPassed = 0;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("Service thread hatasi", ex);
                        }
                        Thread.Sleep(2000);
                    }
                });
                serviceThread.IsBackground = true;
                serviceThread.Start();

                // 3. UI Başlat
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[3/3] Kullanici arayuzu baslatiliyor...");
                Console.ResetColor();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[+] HAZIR! [INSERT] tusuna basin.");
                Console.ResetColor();

                OverlayForm menu = new OverlayForm(memory, scanner);
                if (Array.IndexOf(args, "--esp") >= 0)
                    menu.Shown += delegate { menu.BeginInvoke(new Action(delegate { menu.Hide(); })); };
                Application.Run(menu);
            }
            catch (Exception ex)
            {
                Logger.LogError("Kritik Program hatasi", ex);
                MessageBox.Show(ex.Message + "\r\nDetaylar: error.log", "Baslatma hatasi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// GameAssembly.dll'i olası konumlarda ara
        /// </summary>
        static string FindGameAssemblyDll()
        {
            string configuredPath = Environment.GetEnvironmentVariable("AMONG_US_PATH");
            string[] possiblePaths = new string[]
            {
                string.IsNullOrWhiteSpace(configuredPath) ? string.Empty : Path.Combine(configuredPath, "GameAssembly.dll"),
                @"C:\Program Files\Epic Games\AmongUs\GameAssembly.dll",
                @"C:\Program Files (x86)\Epic Games\AmongUs\GameAssembly.dll",
                @"C:\Program Files (x86)\Steam\steamapps\common\Among Us\GameAssembly.dll",
                @"C:\Program Files\Steam\steamapps\common\Among Us\GameAssembly.dll",
                @"D:\Steam\steamapps\common\Among Us\GameAssembly.dll",
                @"D:\Epic Games\AmongUs\GameAssembly.dll",
                @"E:\Steam\steamapps\common\Among Us\GameAssembly.dll",
                @"E:\Epic Games\AmongUs\GameAssembly.dll",
            };

            foreach (string path in possiblePaths)
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    Console.WriteLine("[✓] GameAssembly.dll bulundu: " + path);
                    return path;
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("[!] GameAssembly.dll bulunamadi (memory'den okunacak)");
            Console.ResetColor();
            return string.Empty;
        }
    }
}
