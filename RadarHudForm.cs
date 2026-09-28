using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AmongUsExternal
{
    public class RadarHudForm : Form
    {
        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, out Rect rect);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr window, ref Point point);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
        private readonly MemoryManager memory;
        private readonly RadarCanvas canvas;
        public bool RadarEnabled { get; set; }

        public RadarHudForm(MemoryManager memory)
        {
            this.memory = memory;
            Text = "Among Us Radar HUD";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            Size = new Size(380, 300);
            Opacity = 0.88;
            canvas = new RadarCanvas { Dock = DockStyle.Fill, DrawNames = true };
            Controls.Add(canvas);
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= 0x80000 | 0x20 | 0x8000000 | 0x80; return cp; }
        }

        public void CopySettings(RadarCanvas source)
        {
            canvas.DrawLines = source.DrawLines; canvas.DrawNames = source.DrawNames;
            canvas.ShowDead = source.ShowDead; canvas.OnlyImpostors = source.OnlyImpostors;
            canvas.FollowLocal = source.FollowLocal; canvas.Range = source.Range;
            canvas.SelectedPlayerId = source.SelectedPlayerId;
        }

        public void UpdatePlayers(List<PlayerDataModel> players)
        {
            canvas.SetPlayers(players);
            if (!RadarEnabled || !memory.IsConnected || players.Count == 0) { Hide(); return; }
            try
            {
                using (Process process = Process.GetProcessById(memory.GetProcessId()))
                {
                    IntPtr handle = process.MainWindowHandle;
                    Rect rect; Point origin = new Point();
                    if (handle == IntPtr.Zero || IsIconic(handle) || !GetClientRect(handle, out rect)
                        || !ClientToScreen(handle, ref origin)) { Hide(); return; }
                    int width = rect.Right - rect.Left, height = rect.Bottom - rect.Top;
                    if (width < 400 || height < 320) { Hide(); return; }
                    Location = new System.Drawing.Point(origin.X + width - Width - 12, origin.Y + 12);
                    if (!Visible) Show();
                }
            }
            catch (InvalidOperationException) { Hide(); }
            catch (ArgumentException) { Hide(); }
        }
    }
}
