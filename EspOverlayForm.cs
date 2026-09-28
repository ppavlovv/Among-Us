using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AmongUsExternal
{
    public class EspOverlayForm : Form
    {
        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, out Rect rect);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr window, ref NativePoint point);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        private readonly MemoryManager memory;
        private readonly PlayerManager players;
        private readonly Timer timer;
        private List<PlayerDataModel> snapshot = new List<PlayerDataModel>();
        private CameraSnapshot camera;
        public bool EspEnabled { get; set; }
        public bool DrawLines { get; set; }
        public bool DrawBoxes { get; set; }
        public bool DrawNames { get; set; }
        public bool OnlyImpostors { get; set; }
        public bool ShowDead { get; set; }
        public string Status { get; private set; }
        public int RenderedPlayers { get; private set; }
        public int RenderedLines { get; private set; }

        public EspOverlayForm(MemoryManager memory)
        {
            this.memory = memory;
            players = new PlayerManager(memory);
            DrawLines = DrawBoxes = DrawNames = ShowDead = true;
            Text = "Among Us Screen ESP";
            BackColor = Color.Fuchsia;
            TransparencyKey = Color.Fuchsia;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9, FontStyle.Bold);
            timer = new Timer { Interval = 50 };
            timer.Tick += delegate { UpdateOverlay(); };
            timer.Start();
            Status = "ESP kapali";
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= 0x80000 | 0x20 | 0x8000000 | 0x80; return cp; }
        }

        public void SetSnapshot(CameraSnapshot view, List<PlayerDataModel> data)
        {
            camera = view;
            snapshot = data ?? new List<PlayerDataModel>();
            Invalidate();
        }

        public void UpdateOverlay()
        {
            if (!EspEnabled || !memory.IsConnected) { Status = EspEnabled ? "Oyun bekleniyor" : "ESP kapali"; Hide(); SetSnapshot(null, null); return; }
            try
            {
                using (Process game = Process.GetProcessById(memory.GetProcessId()))
                {
                    IntPtr handle = game.MainWindowHandle;
                    Rect rect; NativePoint origin = new NativePoint();
                    if (handle == IntPtr.Zero || IsIconic(handle) || GetForegroundWindow() != handle
                        || !GetClientRect(handle, out rect) || !ClientToScreen(handle, ref origin))
                    { Status = "Oyun odagi bekleniyor"; Hide(); SetSnapshot(null, null); return; }
                    CameraSnapshot currentCamera = CameraReader.Read(memory);
                    if (currentCamera == null) { Status = "Kamera okunamadi"; Hide(); SetSnapshot(null, null); return; }
                    var data = players.GetAllPlayers();
                    if (data.Count == 0) { Status = "Oyuncular bekleniyor"; Hide(); SetSnapshot(null, null); return; }
                    Bounds = new Rectangle(origin.X, origin.Y, rect.Right - rect.Left, rect.Bottom - rect.Top);
                    SetSnapshot(currentCamera, data);
                    Status = "ESP aktif";
                    if (!Visible) Show();
                }
            }
            catch (InvalidOperationException) { Hide(); SetSnapshot(null, null); }
            catch (ArgumentException) { Hide(); SetSnapshot(null, null); }
        }

        public static PointF ClipToEdge(PointF target, Size size, int margin)
        {
            float cx = size.Width / 2f, cy = size.Height / 2f;
            float dx = target.X - cx, dy = target.Y - cy;
            float ratioX = Math.Abs(dx) < 0.001f ? float.MaxValue : (cx - margin) / Math.Abs(dx);
            float ratioY = Math.Abs(dy) < 0.001f ? float.MaxValue : (cy - margin) / Math.Abs(dy);
            float ratio = Math.Min(1, Math.Min(ratioX, ratioY));
            return new PointF(cx + dx * ratio, cy + dy * ratio);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            RenderedPlayers = RenderedLines = 0;
            if (camera == null || camera.OrthographicSize <= 0) return;
            // Color-key transparency needs hard edges to avoid magenta antialias fringes.
            e.Graphics.SmoothingMode = SmoothingMode.None;
            float scale = ClientSize.Height / (2 * camera.OrthographicSize);
            var labels = new List<Rectangle>();
            var labelTexts = new List<string>();
            var labelColors = new List<Color>();
            foreach (var player in snapshot)
            {
                if (player.IsLocal || !player.HasLivePosition || (!ShowDead && player.IsDead)
                    || (OnlyImpostors && (!player.RoleKnown || !player.IsImpostor))) continue;
                PointF raw = camera.WorldToScreen(player.PositionX, player.PositionY, ClientSize);
                bool offscreen = raw.X < 18 || raw.Y < 18 || raw.X > ClientSize.Width - 18 || raw.Y > ClientSize.Height - 18;
                PointF point = ClipToEdge(raw, ClientSize, 22);
                Color color = RadarCanvas.TeamColor(player);
                if (DrawLines)
                {
                    using (var outline = new Pen(Color.Black, 3))
                    using (var line = new Pen(color, 1.5f))
                    {
                        e.Graphics.DrawLine(outline, ClientSize.Width / 2f, ClientSize.Height - 8, point.X, point.Y);
                        e.Graphics.DrawLine(line, ClientSize.Width / 2f, ClientSize.Height - 8, point.X, point.Y);
                    }
                    RenderedLines++;
                }
                if (offscreen)
                {
                    double angle = Math.Atan2(raw.Y - ClientSize.Height / 2f, raw.X - ClientSize.Width / 2f);
                    PointF[] arrow = new PointF[] { point,
                        new PointF(point.X - (float)Math.Cos(angle - 0.5) * 15, point.Y - (float)Math.Sin(angle - 0.5) * 15),
                        new PointF(point.X - (float)Math.Cos(angle + 0.5) * 15, point.Y - (float)Math.Sin(angle + 0.5) * 15) };
                    using (var brush = new SolidBrush(color)) e.Graphics.FillPolygon(brush, arrow);
                    using (var outline = new Pen(Color.Black, 1)) e.Graphics.DrawPolygon(outline, arrow);
                }
                else if (DrawBoxes)
                {
                    float boxWidth = Math.Max(15, 0.55f * scale), boxHeight = Math.Max(22, 0.8f * scale);
                    RectangleF box = new RectangleF(point.X - boxWidth / 2, point.Y - boxHeight + scale * 0.2f, boxWidth, boxHeight);
                    using (var outline = new Pen(Color.Black, 3)) e.Graphics.DrawRectangle(outline, box.X, box.Y, box.Width, box.Height);
                    using (var pen = new Pen(color, 1.5f)) e.Graphics.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
                }
                if (DrawNames)
                {
                    string text = player.Name + " | " + (player.RoleKnown ? player.RoleName : "Rol bilinmiyor")
                        + (double.IsNaN(player.Distance) ? "" : " | " + player.Distance.ToString("F1"));
                    int width = Math.Min(ClientSize.Width - 12, TextRenderer.MeasureText(text, Font).Width + 4);
                    int labelX = (int)Math.Max(6, Math.Min(ClientSize.Width - width - 6, point.X - width / 2));
                    int preferredY = (int)(point.Y - (offscreen ? 30 : scale * 0.8f));
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        int labelY = Math.Max(5, Math.Min(ClientSize.Height - 26, preferredY + attempt * 23));
                        Rectangle label = new Rectangle(labelX, labelY, width, 22);
                        if (labels.Exists(delegate(Rectangle r) { return r.IntersectsWith(label); })) continue;
                        labels.Add(label);
                        labelTexts.Add(text);
                        labelColors.Add(color);
                        break;
                    }
                }
                RenderedPlayers++;
            }
            // Paint labels last so later players' lines cannot obscure their text.
            for (int i = 0; i < labels.Count; i++)
            {
                using (var background = new SolidBrush(Color.FromArgb(20, 20, 20)))
                    e.Graphics.FillRectangle(background, labels[i]);
                TextRenderer.DrawText(e.Graphics, labelTexts[i], Font, labels[i], labelColors[i],
                    TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && timer != null) timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
