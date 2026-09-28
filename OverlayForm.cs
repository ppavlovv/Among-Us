using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using AmongUsExternal.Features;

namespace AmongUsExternal
{
    public partial class OverlayForm : Form
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr window, int id);
        private const int InsertHotKeyId = 0x4B56;
        private const int WmHotKey = 0x0312;
        private const uint ModNoRepeat = 0x4000;
        public const int VK_INSERT = 0x2D;
        public const int VK_F1 = 0x70;
        public const int VK_F2 = 0x71;
        public const int VK_F3 = 0x72;

        // Systems
        private MemoryManager memory;
        private OffsetScanner scanner;
        private PlayerManager playerManager;

        // Features
        private NoClipFeature noClip;
        private VisionFeature vision;
        private ImpostorFeature impostor;
        private SpeedFeature speed;
        private GodModeFeature godMode;
        private KillFeatures kill;
        private VentAccessFeature vent;
        private ESPFeature esp;
        private TeleportFeature teleport;
        private RadarForm radar;
        private EspOverlayForm screenEsp;
        private CheckBox chkScreenEsp;

        // UI Components
        private TabControl tabControl;
        private Label lblGameStatus;
        private Label lblOffsetStatus;
        private Label lblConnectionInfo;
        private ListBox playerListBox;
        private ComboBox teleportLocationCombo;
        private System.Windows.Forms.Timer updateTimer;
        private bool syncingCheckboxState;
        private bool insertHotKeyRegistered;

        // Checkbox references
        private CheckBox chkNoClip;
        private CheckBox chkVision;
        private CheckBox chkImpostor;
        private CheckBox chkSpeed;
        private CheckBox chkGodMode;
        private CheckBox chkKill;
        private CheckBox chkVent;

        public OverlayForm(MemoryManager memory, OffsetScanner scanner)
        {
            this.memory = memory;
            this.scanner = scanner;
            this.playerManager = new PlayerManager(memory);
            this.noClip = new NoClipFeature(memory);
            this.vision = new VisionFeature(memory);
            this.impostor = new ImpostorFeature(memory);
            this.speed = new SpeedFeature(memory);
            this.godMode = new GodModeFeature(memory);
            this.kill = new KillFeatures(memory);
            this.vent = new VentAccessFeature(memory);
            this.esp = new ESPFeature(playerManager);
            this.teleport = new TeleportFeature(memory, playerManager);
            this.radar = new RadarForm(memory);
            this.screenEsp = new EspOverlayForm(memory) { EspEnabled = true };

            InitializeUI();
            StartHotkeyListener();
            FormClosed += delegate
            {
                updateTimer.Stop();
                radar.Dispose();
                screenEsp.Dispose();
                speed.Disable();
                vision.Disable();
                memory.Detach();
            };
        }

        private void InitializeUI()
        {
            this.Text = "Among Us Menu V3 - Metadata 31";
            this.Size = new Size(440, 620);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(50, 50);
            this.TopMost = true;
            this.BackColor = Color.FromArgb(20, 22, 28);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Font = new Font("Segoe UI", 9F);

            // === HEADER ===
            Label lblTitle = new Label();
            lblTitle.Text = "AMONG US MENU V3";
            lblTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(230, 60, 80);
            lblTitle.Location = new Point(10, 8);
            lblTitle.Size = new Size(420, 25);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTitle);

            // === STATUS PANEL ===
            Panel statusPanel = new Panel();
            statusPanel.Location = new Point(10, 40);
            statusPanel.Size = new Size(410, 70);
            statusPanel.BackColor = Color.FromArgb(30, 32, 40);
            statusPanel.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(statusPanel);

            lblGameStatus = new Label();
            lblGameStatus.Text = "⚠ Oyun Baglantisi: Bekleniyor...";
            lblGameStatus.ForeColor = Color.Yellow;
            lblGameStatus.Location = new Point(8, 5);
            lblGameStatus.Size = new Size(395, 18);
            lblGameStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            statusPanel.Controls.Add(lblGameStatus);

            lblOffsetStatus = new Label();
            lblOffsetStatus.Text = "⚠ Offset Durumu: Kontrol ediliyor...";
            lblOffsetStatus.ForeColor = Color.OrangeRed;
            lblOffsetStatus.Location = new Point(8, 25);
            lblOffsetStatus.Size = new Size(395, 18);
            statusPanel.Controls.Add(lblOffsetStatus);

            lblConnectionInfo = new Label();
            lblConnectionInfo.Text = "Base: 0x0 | Local: 0x0";
            lblConnectionInfo.ForeColor = Color.Gray;
            lblConnectionInfo.Location = new Point(8, 45);
            lblConnectionInfo.Size = new Size(395, 18);
            lblConnectionInfo.Font = new Font("Consolas", 8F);
            statusPanel.Controls.Add(lblConnectionInfo);

            // === TAB CONTROL ===
            tabControl = new TabControl();
            tabControl.Location = new Point(10, 120);
            tabControl.Size = new Size(410, 425);
            tabControl.Font = new Font("Segoe UI", 9F);
            this.Controls.Add(tabControl);

            BuildHacksTab();
            BuildTeleportTab();
            BuildPlayersTab();
            BuildSandboxTab();
            BuildDiagnosticsTab();
            chkNoClip.Enabled = chkImpostor.Enabled = chkGodMode.Enabled = chkVent.Enabled = false;
            chkNoClip.Text = "NoClip - bu surumde desteklenmiyor";
            chkImpostor.Text = "Rol degistirme - desteklenmiyor";
            chkGodMode.Text = "God Mode - desteklenmiyor";
            chkVent.Text = "Vent erisimi - desteklenmiyor";
            tabControl.TabPages[1].Enabled = false;

            // === FOOTER ===
            Label lblFooter = new Label();
            lblFooter.Text = "ytpavlov | [INSERT] Menu | [F2] Hiz | [F3] Radar | [F4] ESP";
            lblFooter.ForeColor = Color.LightGray;
            lblFooter.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            lblFooter.Location = new Point(10, 555);
            lblFooter.Size = new Size(410, 20);
            lblFooter.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblFooter);

            // Update timer
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 500;
            updateTimer.Tick += delegate { kill.Apply(); UpdateDiagnostics(); PollSandbox(); };
            updateTimer.Start();
        }

        private void BuildHacksTab()
        {
            TabPage tab = new TabPage("Ozellikler");
            tab.BackColor = Color.FromArgb(25, 27, 34);
            tab.ForeColor = Color.White;

            int y = 10, step = 32;

            chkNoClip = CreateCheckbox("👻 NoClip (Duvardan Gec)", y, delegate { noClip.Toggle(); });
            tab.Controls.Add(chkNoClip);

            chkVision = CreateCheckbox("Genis Gorus", y + step * 1, delegate { vision.ToggleMaxVision(); });
            tab.Controls.Add(chkVision);

            chkImpostor = CreateCheckbox("👑 Impostor Rolu (Local)", y + step * 2, delegate { impostor.ToggleLocalImpostor(); });
            tab.Controls.Add(chkImpostor);

            chkSpeed = CreateCheckbox("Hiz (3.5x)", y + step * 3, delegate { speed.ToggleSpeedHack(); });
            tab.Controls.Add(chkSpeed);

            chkGodMode = CreateCheckbox("❤ God Mode (Olumsuzluk)", y + step * 4, delegate { godMode.ToggleGodMode(); });
            tab.Controls.Add(chkGodMode);

            chkKill = CreateCheckbox("Sifir Kill Cooldown", y + step * 5, delegate { kill.ToggleZeroKillCooldown(); });
            tab.Controls.Add(chkKill);

            chkVent = CreateCheckbox("🚀 Vent Erisim (Crewmate)", y + step * 6, delegate { vent.ToggleVentAccess(); });
            tab.Controls.Add(chkVent);

            Button btnESP = new Button();
            btnESP.Text = "📡 Konsola Oyuncu Radari Cikart";
            btnESP.Location = new Point(20, y + step * 7 + 10);
            btnESP.Size = new Size(365, 32);
            btnESP.BackColor = Color.FromArgb(50, 90, 140);
            btnESP.ForeColor = Color.White;
            btnESP.FlatStyle = FlatStyle.Flat;
            btnESP.Text = "Canli Radar ve Impostor Tespiti";
            btnESP.Click += delegate { ShowRadar(); };
            tab.Controls.Add(btnESP);
            chkScreenEsp = new CheckBox { Text = "Ekran ESP", Checked = true, Location = new Point(20, 290), Size = new Size(365, 28) };
            chkScreenEsp.CheckedChanged += delegate { screenEsp.EspEnabled = chkScreenEsp.Checked; screenEsp.UpdateOverlay(); };
            tab.Controls.Add(chkScreenEsp);
            var espLines = new CheckBox { Text = "Cizgiler", Checked = true, Location = new Point(20, 325), Size = new Size(100, 26) };
            var espBoxes = new CheckBox { Text = "Kutular", Checked = true, Location = new Point(135, 325), Size = new Size(100, 26) };
            var espNames = new CheckBox { Text = "Isim / Rol", Checked = true, Location = new Point(250, 325), Size = new Size(125, 26) };
            var espImpostors = new CheckBox { Text = "ESP: sadece impostor", Location = new Point(20, 357), Size = new Size(245, 26) };
            espLines.CheckedChanged += delegate { screenEsp.DrawLines = espLines.Checked; };
            espBoxes.CheckedChanged += delegate { screenEsp.DrawBoxes = espBoxes.Checked; };
            espNames.CheckedChanged += delegate { screenEsp.DrawNames = espNames.Checked; };
            espImpostors.CheckedChanged += delegate { screenEsp.OnlyImpostors = espImpostors.Checked; };
            tab.Controls.AddRange(new Control[] { espLines, espBoxes, espNames, espImpostors });

            tabControl.TabPages.Add(tab);
        }

        private void BuildTeleportTab()
        {
            TabPage tab = new TabPage("🌀 Teleport");
            tab.BackColor = Color.FromArgb(25, 27, 34);
            tab.ForeColor = Color.White;

            Label lblLoc = new Label();
            lblLoc.Text = "📍 Skeld Konumlari:";
            lblLoc.Location = new Point(15, 15);
            lblLoc.Size = new Size(370, 20);
            lblLoc.ForeColor = Color.LightCyan;
            lblLoc.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tab.Controls.Add(lblLoc);

            teleportLocationCombo = new ComboBox();
            teleportLocationCombo.Location = new Point(15, 40);
            teleportLocationCombo.Size = new Size(240, 25);
            teleportLocationCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            teleportLocationCombo.Items.AddRange(new string[] {
                "Admin", "Cafeteria", "Weapons", "O2", "Navigation",
                "Shields", "Comms", "Storage", "Electrical",
                "Lower Engine", "Security", "Reactor", "Upper Engine", "MedBay"
            });
            teleportLocationCombo.SelectedIndex = 0;
            tab.Controls.Add(teleportLocationCombo);

            Button btnTP = new Button();
            btnTP.Text = "🌀 Isinla!";
            btnTP.Location = new Point(265, 40);
            btnTP.Size = new Size(120, 26);
            btnTP.BackColor = Color.FromArgb(90, 50, 140);
            btnTP.ForeColor = Color.White;
            btnTP.FlatStyle = FlatStyle.Flat;
            btnTP.Click += delegate
            {
                string loc = teleportLocationCombo.SelectedItem.ToString().Replace(" ", "");
                bool ok = teleport.TeleportToLocation(loc);
                MessageBox.Show(ok ? "Isinlandi: " + loc : "Isinlanma basarisiz!",
                    "Teleport", MessageBoxButtons.OK,
                    ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            };
            tab.Controls.Add(btnTP);

            Label lblCoord = new Label();
            lblCoord.Text = "🎯 Manuel Koordinat:";
            lblCoord.Location = new Point(15, 85);
            lblCoord.Size = new Size(370, 20);
            lblCoord.ForeColor = Color.LightCyan;
            lblCoord.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tab.Controls.Add(lblCoord);

            Label lblX = new Label();
            lblX.Text = "X:";
            lblX.Location = new Point(15, 115);
            lblX.Size = new Size(20, 20);
            tab.Controls.Add(lblX);

            TextBox txtX = new TextBox();
            txtX.Location = new Point(35, 112);
            txtX.Size = new Size(80, 22);
            txtX.Text = "0.0";
            tab.Controls.Add(txtX);

            Label lblY = new Label();
            lblY.Text = "Y:";
            lblY.Location = new Point(125, 115);
            lblY.Size = new Size(20, 20);
            tab.Controls.Add(lblY);

            TextBox txtY = new TextBox();
            txtY.Location = new Point(145, 112);
            txtY.Size = new Size(80, 22);
            txtY.Text = "0.0";
            tab.Controls.Add(txtY);

            Button btnCustomTP = new Button();
            btnCustomTP.Text = "🎯 Isinla";
            btnCustomTP.Location = new Point(240, 110);
            btnCustomTP.Size = new Size(145, 26);
            btnCustomTP.BackColor = Color.FromArgb(140, 90, 50);
            btnCustomTP.ForeColor = Color.White;
            btnCustomTP.FlatStyle = FlatStyle.Flat;
            btnCustomTP.Click += delegate
            {
                float x, y;
                if (float.TryParse(txtX.Text.Replace(",", "."),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out x)
                    && float.TryParse(txtY.Text.Replace(",", "."),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out y))
                {
                    teleport.TeleportTo(x, y);
                }
                else
                {
                    MessageBox.Show("Gecersiz koordinat!", "Hata");
                }
            };
            tab.Controls.Add(btnCustomTP);

            Label lblSpecial = new Label();
            lblSpecial.Text = "🎯 Ozel Teleportlar:";
            lblSpecial.Location = new Point(15, 160);
            lblSpecial.Size = new Size(370, 20);
            lblSpecial.ForeColor = Color.LightCyan;
            lblSpecial.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tab.Controls.Add(lblSpecial);

            Button btnToImpostor = new Button();
            btnToImpostor.Text = "🔴 En Yakin Impostor'a Isinla";
            btnToImpostor.Location = new Point(15, 190);
            btnToImpostor.Size = new Size(370, 30);
            btnToImpostor.BackColor = Color.FromArgb(140, 40, 40);
            btnToImpostor.ForeColor = Color.White;
            btnToImpostor.FlatStyle = FlatStyle.Flat;
            btnToImpostor.Click += delegate
            {
                bool ok = teleport.TeleportToNearestImpostor();
                MessageBox.Show(ok ? "Impostor'a isinlandi!" : "Impostor bulunamadi!",
                    "Teleport", MessageBoxButtons.OK,
                    ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            };
            tab.Controls.Add(btnToImpostor);

            tabControl.TabPages.Add(tab);
        }

        private void BuildPlayersTab()
        {
            TabPage tab = new TabPage("👥 Oyuncular");
            tab.BackColor = Color.FromArgb(25, 27, 34);
            tab.ForeColor = Color.White;

            Label lblTitle = new Label();
            lblTitle.Text = "🔴 = Impostor  |  ⚫ = Olu  |  🟢 = Canli";
            lblTitle.Location = new Point(10, 8);
            lblTitle.Size = new Size(380, 18);
            lblTitle.ForeColor = Color.LightGray;
            tab.Controls.Add(lblTitle);

            playerListBox = new ListBox();
            playerListBox.Location = new Point(10, 30);
            playerListBox.Size = new Size(380, 260);
            playerListBox.BackColor = Color.FromArgb(15, 17, 22);
            playerListBox.ForeColor = Color.White;
            playerListBox.Font = new Font("Consolas", 9F);
            playerListBox.BorderStyle = BorderStyle.FixedSingle;
            playerListBox.DoubleClick += delegate {
                PlayerDataModel selected = playerListBox.SelectedItem as PlayerDataModel;
                ShowRadar();
                if (selected != null) radar.SelectPlayer(selected.PlayerId);
            };
            tab.Controls.Add(playerListBox);

            Button btnRefresh = new Button();
            btnRefresh.Text = "🔄 Yenile";
            btnRefresh.Location = new Point(10, 300);
            btnRefresh.Size = new Size(120, 28);
            btnRefresh.BackColor = Color.FromArgb(50, 90, 140);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.Click += delegate { RefreshPlayerList(); };
            tab.Controls.Add(btnRefresh);

            Button btnTPPlayer = new Button();
            btnTPPlayer.Text = "🌀 Secili Oyuncuya Isinla";
            btnTPPlayer.Location = new Point(140, 300);
            btnTPPlayer.Size = new Size(250, 28);
            btnTPPlayer.BackColor = Color.FromArgb(90, 50, 140);
            btnTPPlayer.ForeColor = Color.White;
            btnTPPlayer.FlatStyle = FlatStyle.Flat;
            btnTPPlayer.Click += delegate
            {
                if (playerListBox.SelectedIndex >= 0 && playerListBox.SelectedItem != null)
                {
                    string sel = playerListBox.SelectedItem.ToString();
                    // Extract player ID from list item format "[ID] Name..."
                    int idStart = sel.IndexOf('[') + 1;
                    int idEnd = sel.IndexOf(']');
                    if (idStart > 0 && idEnd > idStart)
                    {
                        byte pid;
                        if (byte.TryParse(sel.Substring(idStart, idEnd - idStart), out pid))
                        {
                            teleport.TeleportToPlayer(pid);
                        }
                    }
                }
            };
            tab.Controls.Add(btnTPPlayer);
            btnTPPlayer.Enabled = false;

            tabControl.TabPages.Add(tab);
        }

        private void BuildDiagnosticsTab()
        {
            TabPage tab = new TabPage("🔧 Teshis");
            tab.BackColor = Color.FromArgb(25, 27, 34);
            tab.ForeColor = Color.White;

            TextBox txtDiagnostics = new TextBox();
            txtDiagnostics.Location = new Point(10, 10);
            txtDiagnostics.Size = new Size(380, 280);
            txtDiagnostics.Multiline = true;
            txtDiagnostics.ReadOnly = true;
            txtDiagnostics.ScrollBars = ScrollBars.Vertical;
            txtDiagnostics.BackColor = Color.FromArgb(15, 17, 22);
            txtDiagnostics.ForeColor = Color.LightGreen;
            txtDiagnostics.Font = new Font("Consolas", 8.5F);
            tab.Controls.Add(txtDiagnostics);

            Button btnRun = new Button();
            btnRun.Text = "🔬 Offset Dogrulamayi Calistir";
            btnRun.Location = new Point(10, 300);
            btnRun.Size = new Size(240, 30);
            btnRun.BackColor = Color.FromArgb(50, 140, 50);
            btnRun.ForeColor = Color.White;
            btnRun.FlatStyle = FlatStyle.Flat;
            btnRun.Click += delegate
            {
                if (!memory.IsConnected)
                {
                    txtDiagnostics.Text = "[!] Once Among Us'i ac ve baglanmasini bekle.\r\n";
                    return;
                }

                OffsetVerificationResult result = scanner.VerifyOffsets(memory);
                txtDiagnostics.Text = result.ToString();
                txtDiagnostics.Text += "\r\n\r\n";
                txtDiagnostics.Text += "=== SISTEM BILGISI ===\r\n";
                txtDiagnostics.Text += string.Format("Game PID: {0}\r\n", memory.GetProcessId());
                txtDiagnostics.Text += string.Format("Base Address: 0x{0:X}\r\n", memory.GameAssemblyBase.ToInt64());
                txtDiagnostics.Text += string.Format("PlayerControl Class Offset: 0x{0:X}\r\n", GameLayout.PlayerControlRva);
                txtDiagnostics.Text += string.Format("GameOptionsManager Offset: 0x{0:X}\r\n", GameLayout.OptionsManagerRva);
                txtDiagnostics.Text += "\r\n";

                if (!result.PlayerControlValid)
                {
                    txtDiagnostics.Text += "=== COZUM ONERILERI ===\r\n";
                    txtDiagnostics.Text += "1. Among Us surumunuz farkli olabilir\r\n";
                    txtDiagnostics.Text += "2. Il2CppDumper ciktisi ile surum profilini yeniden dogrulayin\r\n";
                }
            };
            tab.Controls.Add(btnRun);

            Button btnOpenLog = new Button();
            btnOpenLog.Text = "📄 error.log";
            btnOpenLog.Location = new Point(260, 300);
            btnOpenLog.Size = new Size(130, 30);
            btnOpenLog.BackColor = Color.FromArgb(140, 90, 50);
            btnOpenLog.ForeColor = Color.White;
            btnOpenLog.FlatStyle = FlatStyle.Flat;
            btnOpenLog.Click += delegate
            {
                try
                {
                    string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                    if (System.IO.File.Exists(logPath))
                        System.Diagnostics.Process.Start("notepad.exe", logPath);
                }
                catch { }
            };
            tab.Controls.Add(btnOpenLog);

            // === OTOMATIK OFFSET BUL BUTONU ===
            Button btnAutoFind = new Button();
            btnAutoFind.Enabled = false;
            btnAutoFind.Text = "Metadata 31 profili / eski tarama kapali";
            btnAutoFind.Location = new Point(10, 335);
            btnAutoFind.Size = new Size(380, 32);
            btnAutoFind.BackColor = Color.FromArgb(160, 80, 200);
            btnAutoFind.ForeColor = Color.White;
            btnAutoFind.FlatStyle = FlatStyle.Flat;
            btnAutoFind.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnAutoFind.Click += delegate
            {
                if (!memory.IsConnected)
                {
                    txtDiagnostics.Text = "[!] Once Among Us'a baglanmali!\r\n\r\nAdimlar:\r\n1. Among Us'i ac\r\n2. Bir hesap olustur veya gir\r\n3. Bu butona tekrar bas";
                    return;
                }

                txtDiagnostics.Text = "🔮 OTOMATIK OFFSET TARAMASI\r\n" +
                    "═══════════════════════════════\r\n\r\n" +
                    "UYARI: Bu islem 10-30 saniye surebilir.\r\n" +
                    "Lutfen bekleyin, program donmadi.\r\n\r\n";
                txtDiagnostics.Refresh();
                Application.DoEvents();

                // Background thread'de tara (UI donmasin)
                Thread scanThread = new Thread(delegate()
                {
                    try
                    {
                        BruteForceOffsetFinder finder = new BruteForceOffsetFinder(memory);

                        Action<string> progress = delegate(string msg)
                        {
                            this.BeginInvoke(new Action(delegate()
                            {
                                txtDiagnostics.AppendText(msg + "\r\n");
                                txtDiagnostics.SelectionStart = txtDiagnostics.Text.Length;
                                txtDiagnostics.ScrollToCaret();
                            }));
                        };

                        // ADIM 1: PE Header'ı runtime'da oku, section'ları bul
                        progress("=== ADIM 1: PE Section Analizi ===");
                        bool peOk = finder.ReadPESections(progress);
                        List<ModuleSection> dataSections = finder.GetDataSections();

                        long found = -1;

                        // ADIM 2: Section tabanlı Class Name tarama (en güvenilir)
                        if (peOk && dataSections.Count > 0)
                        {
                            progress("");
                            progress("=== ADIM 2: 'PlayerControl' class name araniyor ===");
                            foreach (ModuleSection sec in dataSections)
                            {
                                progress(string.Format("Section taraniyor: {0}", sec.ToString()));
                                found = finder.FindClassByName("PlayerControl",
                                    sec.VirtualAddress, sec.End, progress);
                                if (found > 0)
                                {
                                    progress("");
                                    progress("Static fields offset tespit ediliyor...");
                                    finder.DetectStaticFieldsOffset(found, progress);
                                    break;
                                }
                            }
                        }

                        // FALLBACK 1: Class name taraması başarısız → geniş alan tara
                        if (found <= 0)
                        {
                            progress("");
                            progress("=== FALLBACK 1: Genis Alan Class Name Taramasi ===");
                            progress("Tum modul taraniyor (0x1000000 - 0xA000000)...");
                            found = finder.FindClassByName("PlayerControl",
                                0x1000000, 0xA000000, progress);
                            if (found > 0)
                            {
                                finder.DetectStaticFieldsOffset(found, progress);
                            }
                        }

                        // FALLBACK 2: Class name de bulunmadıysa eski PlayerName tabanlı yöntem dene
                        if (found <= 0)
                        {
                            progress("");
                            progress("=== FALLBACK 2: PlayerName Tabanli Brute Force ===");
                            found = finder.FindPlayerControlClass(0x2000000, 0x4000000, progress);
                        }

                        if (found > 0)
                        {
                            // GameOptions'ı da tara
                            this.BeginInvoke(new Action(delegate()
                            {
                                txtDiagnostics.AppendText("\r\nGameOptions taraniyor...\r\n");
                            }));

                            long goFound = finder.FindGameOptionsClass(0x2000000, 0x4000000, progress);

                            this.BeginInvoke(new Action(delegate()
                            {
                                txtDiagnostics.AppendText("\r\n✅ TAMAMLANDI!\r\n");
                                txtDiagnostics.AppendText("Offsetler otomatik uygulandi.\r\n");
                                txtDiagnostics.AppendText("Simdi hackleri deneyebilirsin!\r\n\r\n");

                                // offsets.json'a kaydet
                                try
                                {
                                    string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "offsets_auto.json");
                                    string json = "{\r\n" +
                                        string.Format("  \"_note\": \"Auto-detected offsets for build detected at runtime\",\r\n") +
                                        string.Format("  \"PlayerControl_Class\": \"0x{0:X}\",\r\n", (int)Offsets.PlayerControl_Class) +
                                        string.Format("  \"GameOptionsData_Class\": \"0x{0:X}\",\r\n", (int)Offsets.GameOptionsData_Class) +
                                        string.Format("  \"PlayerControl_LocalPlayer\": \"0x{0:X}\",\r\n", Offsets.PlayerControl_LocalPlayer) +
                                        string.Format("  \"PlayerControl_MyData\": \"0x{0:X}\",\r\n", Offsets.PlayerControl_MyData) +
                                        string.Format("  \"PlayerData_PlayerName\": \"0x{0:X}\"\r\n", Offsets.PlayerData_PlayerName) +
                                        "}";
                                    System.IO.File.WriteAllText(configPath, json);
                                    txtDiagnostics.AppendText("💾 offsets_auto.json olarak kaydedildi.\r\n");
                                    txtDiagnostics.AppendText("   Bu dosyayi offsets.json olarak yeniden adlandirirsan\r\n");
                                    txtDiagnostics.AppendText("   sonraki calisttirmalarda otomatik yuklenir.\r\n");
                                }
                                catch (Exception ex)
                                {
                                    txtDiagnostics.AppendText("Kayit hatasi: " + ex.Message + "\r\n");
                                }
                            }));
                        }
                        else
                        {
                            this.BeginInvoke(new Action(delegate()
                            {
                                txtDiagnostics.AppendText("\r\n❌ Otomatik tarama basarisiz.\r\n\r\n");
                                txtDiagnostics.AppendText("Nedenler:\r\n");
                                txtDiagnostics.AppendText("• Oyunda 'Local Player' henuz olusturulmamis\r\n");
                                txtDiagnostics.AppendText("• Il2Cpp yapisi cok farkli (yeni surumu)\r\n");
                                txtDiagnostics.AppendText("• Anti-cheat engelledi\r\n\r\n");
                                txtDiagnostics.AppendText("Cozum:\r\n");
                                txtDiagnostics.AppendText("1. Among Us'ta 'PLAY' -> 'LOCAL' e gir\r\n");
                                txtDiagnostics.AppendText("2. Karakter oluşturup isim gir\r\n");
                                txtDiagnostics.AppendText("3. Lobide 3-5 saniye bekle\r\n");
                                txtDiagnostics.AppendText("4. Bu butona tekrar bas\r\n");
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        this.BeginInvoke(new Action(delegate()
                        {
                            txtDiagnostics.AppendText("\r\n[HATA] " + ex.Message + "\r\n");
                        }));
                    }
                });
                scanThread.IsBackground = true;
                scanThread.Start();
            };
            tab.Controls.Add(btnAutoFind);

            tabControl.TabPages.Add(tab);
        }

        private CheckBox CreateCheckbox(string text, int y, Action onToggle)
        {
            CheckBox chk = new CheckBox();
            chk.Text = text;
            chk.Location = new Point(15, y + 10);
            chk.Size = new Size(370, 26);
            chk.ForeColor = Color.White;
            chk.Font = new Font("Segoe UI", 9.5F);
            chk.CheckedChanged += delegate
            {
                if (!syncingCheckboxState)
                {
                    onToggle();
                }
            };
            return chk;
        }

        private void SetCheckboxState(CheckBox chk, bool isChecked)
        {
            if (chk == null || chk.Checked == isChecked) return;

            syncingCheckboxState = true;
            try
            {
                chk.Checked = isChecked;
            }
            finally
            {
                syncingCheckboxState = false;
            }
        }

        private void UpdateDiagnostics()
        {
            try
            {
                SetCheckboxState(chkSpeed, speed.SpeedHackEnabled);
                SetCheckboxState(chkVision, vision.MaxVisionEnabled);
                SetCheckboxState(chkKill, kill.ZeroKillCooldownEnabled);
                chkScreenEsp.Text = "Ekran ESP - " + screenEsp.Status;
                bool ready = GameLayout.GetLocalPlayer(memory) != IntPtr.Zero;
                bool optionsReady = GameLayout.GetOptions(memory) != IntPtr.Zero;
                chkSpeed.Enabled = chkVision.Enabled = ready && optionsReady;
                chkKill.Enabled = ready;
                if (!memory.IsConnected)
                {
                    lblGameStatus.Text = "⚠ Oyun Baglantisi: Among Us Bekleniyor";
                    lblGameStatus.ForeColor = Color.Yellow;
                    lblOffsetStatus.Text = "⚪ Offset: Bekleme modunda";
                    lblOffsetStatus.ForeColor = Color.Gray;
                    lblConnectionInfo.Text = "Base: 0x0 | Local: 0x0";
                    return;
                }

                lblGameStatus.Text = string.Format("✓ Baglandi (PID: {0})", memory.GetProcessId());
                lblGameStatus.ForeColor = Color.LightGreen;

                IntPtr playerControlClass = GameLayout.GetClass(memory, GameLayout.PlayerControlRva, "PlayerControl");
                if (playerControlClass == IntPtr.Zero)
                {
                    lblOffsetStatus.Text = "❌ Offset HATA: PlayerControl=0x0 (Surum uyumsuz!)";
                    lblOffsetStatus.ForeColor = Color.FromArgb(255, 90, 90);
                    lblConnectionInfo.Text = string.Format("Base: 0x{0:X}", memory.GameAssemblyBase.ToInt64());
                }
                else
                {
                    IntPtr localPlayer = GameLayout.GetLocalPlayer(memory);

                    if (localPlayer != IntPtr.Zero)
                    {
                        lblOffsetStatus.Text = optionsReady ? "V31: oyuncu ve oyun ayarlari dogrulandi" : "Oyuncu hazir; normal oyun ayarlari bekleniyor";
                        lblOffsetStatus.ForeColor = Color.LightGreen;
                    }
                    else
                    {
                        lblOffsetStatus.Text = "⚠ Kismi: PlayerControl OK, LocalPlayer bulunamadi";
                        lblOffsetStatus.ForeColor = Color.Orange;
                    }

                    lblConnectionInfo.Text = string.Format("Base: 0x{0:X} | PC: 0x{1:X} | LP: 0x{2:X}",
                        memory.GameAssemblyBase.ToInt64(),
                        playerControlClass.ToInt64(),
                        localPlayer.ToInt64());
                }
            }
            catch (Exception ex)
            {
                lblOffsetStatus.Text = "❌ Hata: " + ex.Message;
                lblOffsetStatus.ForeColor = Color.Red;
            }
        }

        private void RefreshPlayerList()
        {
            try
            {
                playerListBox.Items.Clear();

                if (!memory.IsConnected)
                {
                    playerListBox.Items.Add("[!] Oyuna baglanmadi");
                    return;
                }

                List<PlayerDataModel> players = playerManager.GetAllPlayers();
                if (players.Count == 0)
                {
                    playerListBox.Items.Add("[!] Aktif oyuncu bulunamadi");
                    playerListBox.Items.Add("    (Lobide veya oyun icinde olmalisiniz)");
                    return;
                }

                foreach (PlayerDataModel p in players)
                {
                    playerListBox.Items.Add(p);
                }
            }
            catch (Exception ex)
            {
                playerListBox.Items.Add("[!] Hata: " + ex.Message);
            }
        }

        private void ShowRadar()
        {
            if (radar.IsDisposed) return;
            radar.RefreshSnapshot();
            radar.Show();
            radar.BringToFront();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            insertHotKeyRegistered = RegisterHotKey(Handle, InsertHotKeyId, ModNoRepeat, VK_INSERT);
            if (!insertHotKeyRegistered) Logger.LogWarning("INSERT kisayolu kaydedilemedi");
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (insertHotKeyRegistered)
            {
                UnregisterHotKey(Handle, InsertHotKeyId);
                insertHotKeyRegistered = false;
            }
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmHotKey && message.WParam.ToInt32() == InsertHotKeyId)
            {
                ToggleMenuVisibility();
                return;
            }
            base.WndProc(ref message);
        }

        private void ToggleMenuVisibility()
        {
            if (Visible) Hide();
            else
            {
                Show();
                BringToFront();
                Activate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (radar != null) radar.Dispose();
                if (screenEsp != null) screenEsp.Dispose();
                if (updateTimer != null) updateTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private void StartHotkeyListener()
        {
            Thread hotkeyThread = new Thread(delegate()
            {
                bool[] lastState = new bool[256];
                while (!this.IsDisposed)
                {
                    try
                    {
                        // INSERT: Menü aç/kapat
                        bool insertPressed = (GetAsyncKeyState(VK_INSERT) & 0x8000) != 0;
                        if (!insertHotKeyRegistered && insertPressed && !lastState[VK_INSERT])
                        {
                            this.BeginInvoke(new Action(ToggleMenuVisibility));
                        }
                        lastState[VK_INSERT] = insertPressed;

                        // F1: NoClip toggle
                        bool f1Pressed = (GetAsyncKeyState(VK_F1) & 0x8000) != 0;
                        if (f1Pressed && !lastState[VK_F1])
                        {
                            this.BeginInvoke(new Action(delegate()
                            {
                                if (chkNoClip.Enabled) chkNoClip.Checked = !chkNoClip.Checked;
                            }));
                        }
                        lastState[VK_F1] = f1Pressed;

                        // F2: Speed toggle
                        bool f2Pressed = (GetAsyncKeyState(VK_F2) & 0x8000) != 0;
                        if (f2Pressed && !lastState[VK_F2])
                        {
                            this.BeginInvoke(new Action(delegate()
                            {
                                if (chkSpeed.Enabled)
                                {
                                    speed.ToggleSpeedHack();
                                    SetCheckboxState(chkSpeed, speed.SpeedHackEnabled);
                                }
                            }));
                        }
                        lastState[VK_F2] = f2Pressed;

                        // F3: ESP Radar
                        bool f3Pressed = (GetAsyncKeyState(VK_F3) & 0x8000) != 0;
                        if (f3Pressed && !lastState[VK_F3])
                        {
                            this.BeginInvoke(new Action(delegate()
                            {
                                if (radar.Visible) radar.Hide(); else ShowRadar();
                            }));
                        }
                        lastState[VK_F3] = f3Pressed;
                        bool f4Pressed = (GetAsyncKeyState(0x73) & 0x8000) != 0;
                        if (f4Pressed && !lastState[0x73])
                            this.BeginInvoke(new Action(delegate { chkScreenEsp.Checked = !chkScreenEsp.Checked; }));
                        lastState[0x73] = f4Pressed;
                    }
                    catch { }

                    Thread.Sleep(50);
                }
            });
            hotkeyThread.IsBackground = true;
            hotkeyThread.Start();
        }
    }
}
