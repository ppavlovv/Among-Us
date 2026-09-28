using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AmongUsExternal
{
    public class RadarForm : Form
    {
        private readonly MemoryManager memory;
        private readonly PlayerManager manager;
        private readonly RadarCanvas canvas;
        private readonly DataGridView table;
        private readonly Label summary;
        private readonly TextBox search;
        private readonly Label details;
        private readonly CheckBox hudToggle;
        private readonly Timer timer;
        private readonly RadarHudForm hud;
        private List<PlayerDataModel> snapshot = new List<PlayerDataModel>();
        private readonly CheckBox impostorsOnly;
        private readonly CheckBox showDead;
        private bool refreshing;
        public byte? SelectedPlayerId { get { return canvas.SelectedPlayerId; } }
        public RadarCanvas Canvas { get { return canvas; } }

        public RadarForm(MemoryManager memory)
        {
            this.memory = memory;
            manager = new PlayerManager(memory);
            Text = "Among Us - Canli Radar ve Roller";
            Size = new Size(1020, 660);
            MinimumSize = new Size(840, 540);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(24, 27, 29);
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 9);
            TopMost = true;
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 68, Padding = new Padding(8), WrapContents = true };
            canvas = new RadarCanvas { Dock = DockStyle.Fill };
            hud = new RadarHudForm(memory);
            var lines = new CheckBox { Text = "Cizgiler", Checked = true, AutoSize = true, Margin = new Padding(6) };
            var names = new CheckBox { Text = "Isimler", Checked = true, AutoSize = true, Margin = new Padding(6) };
            showDead = new CheckBox { Text = "Oluler", Checked = true, AutoSize = true, Margin = new Padding(6) };
            impostorsOnly = new CheckBox { Text = "Sadece impostor", AutoSize = true, Margin = new Padding(6) };
            var follow = new CheckBox { Text = "Beni merkezle", AutoSize = true, Margin = new Padding(6) };
            hudToggle = new CheckBox { Text = "Oyun ustu radar", AutoSize = true, Margin = new Padding(6) };
            var pin = new CheckBox { Text = "Ustte tut", Checked = true, AutoSize = true, Margin = new Padding(6) };
            var range = new NumericUpDown { Minimum = 5, Maximum = 200, Value = 35, Increment = 5, Width = 64 };
            toolbar.Controls.AddRange(new Control[] { lines, names, showDead, impostorsOnly, follow, hudToggle, pin,
                new Label { Text = "Olcek", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, range });
            summary = new Label { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(10, 5, 0, 0), AutoEllipsis = true };
            var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel2, SplitterWidth = 6 };
            split.Panel1.Controls.Add(canvas);
            search = new TextBox { Dock = DockStyle.Top, Height = 26, AccessibleName = "Oyuncu ara" };
            table = new DataGridView {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = Color.FromArgb(24, 27, 29), BorderStyle = BorderStyle.None,
                AllowUserToResizeRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false
            };
            table.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 49, 52);
            table.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            table.DefaultCellStyle.BackColor = BackColor;
            table.DefaultCellStyle.ForeColor = ForeColor;
            table.DefaultCellStyle.SelectionBackColor = Color.FromArgb(54, 70, 71);
            table.DefaultCellStyle.SelectionForeColor = Color.White;
            table.RowTemplate.Height = 29;
            table.Columns.Add("Name", "Oyuncu"); table.Columns.Add("Role", "Rol"); table.Columns.Add("Distance", "Mesafe");
            table.Columns[0].FillWeight = 115; table.Columns[1].FillWeight = 110; table.Columns[2].FillWeight = 65;
            foreach (DataGridViewColumn column in table.Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable;
            split.Panel2.Controls.Add(table);
            split.Panel2.Controls.Add(search);
            details = new Label { Dock = DockStyle.Bottom, Height = 55, Padding = new Padding(6), AutoEllipsis = true, Text = "Oyuncu secilmedi" };
            split.Panel2.Controls.Add(details);
            Controls.Add(split); Controls.Add(toolbar); Controls.Add(summary);
            Shown += delegate { split.SplitterDistance = Math.Max(380, split.Width - 355); RefreshSnapshot(); };
            Action updateFilters = delegate {
                canvas.DrawLines = lines.Checked; canvas.DrawNames = names.Checked;
                canvas.ShowDead = showDead.Checked; canvas.OnlyImpostors = impostorsOnly.Checked;
                canvas.FollowLocal = follow.Checked; canvas.Range = (float)range.Value;
                hud.CopySettings(canvas); canvas.Invalidate(); PopulateRows();
            };
            lines.CheckedChanged += delegate { updateFilters(); }; names.CheckedChanged += delegate { updateFilters(); };
            showDead.CheckedChanged += delegate { updateFilters(); }; impostorsOnly.CheckedChanged += delegate { updateFilters(); };
            follow.CheckedChanged += delegate { updateFilters(); }; range.ValueChanged += delegate { updateFilters(); };
            pin.CheckedChanged += delegate { TopMost = pin.Checked; };
            hudToggle.CheckedChanged += delegate { hud.RadarEnabled = hudToggle.Checked; RefreshSnapshot(); };
            search.TextChanged += delegate { PopulateRows(); };
            table.SelectionChanged += delegate {
                if (refreshing || table.SelectedRows.Count == 0) return;
                PlayerDataModel p = table.SelectedRows[0].Tag as PlayerDataModel;
                if (p == null) return;
                canvas.SelectedPlayerId = p.PlayerId; hud.CopySettings(canvas); canvas.Invalidate();
                UpdateDetails();
            };
            canvas.PlayerSelected += delegate(byte id) { SelectPlayer(id); };
            timer = new Timer { Interval = 150 };
            timer.Tick += delegate { if (Visible || hud.RadarEnabled) RefreshSnapshot(); };
            timer.Start();
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
            };
        }

        public void SelectPlayer(byte id)
        {
            foreach (DataGridViewRow row in table.Rows)
            {
                PlayerDataModel p = row.Tag as PlayerDataModel;
                if (p != null && p.PlayerId == id)
                {
                    canvas.SelectedPlayerId = id;
                    table.CurrentCell = row.Cells[0];
                    row.Selected = true;
                    hud.CopySettings(canvas);
                    canvas.Invalidate();
                    UpdateDetails();
                    return;
                }
            }
        }

        public void RefreshSnapshot()
        {
            snapshot = manager.GetAllPlayers();
            if (canvas.SelectedPlayerId.HasValue && !snapshot.Exists(delegate(PlayerDataModel p) { return p.PlayerId == canvas.SelectedPlayerId.Value; }))
                canvas.SelectedPlayerId = null;
            canvas.SetPlayers(snapshot);
            UpdateDetails();
            hud.UpdatePlayers(snapshot);
            int impostors = snapshot.FindAll(delegate(PlayerDataModel p) { return p.RoleKnown && p.IsImpostor && !p.IsDead; }).Count;
            int unknown = snapshot.FindAll(delegate(PlayerDataModel p) { return !p.RoleKnown; }).Count;
            summary.Text = !memory.IsConnected ? "Oyun baglantisi bekleniyor"
                : !memory.IsCompatibleBuild ? "Desteklenmeyen oyun surumu"
                : snapshot.Count == 0 ? "Lobi veya mac oyunculari bekleniyor"
                : string.Format("{0} oyuncu   |   {1} canli impostor   |   {2} bilinmeyen rol   |   {3:HH:mm:ss}", snapshot.Count, impostors, unknown, DateTime.Now);
            PopulateRows();
        }

        private void PopulateRows()
        {
            refreshing = true;
            try
            {
                var filtered = snapshot.FindAll(delegate(PlayerDataModel p) {
                    return (showDead.Checked || !p.IsDead) && (!impostorsOnly.Checked || (p.RoleKnown && p.IsImpostor))
                        && (p.Name ?? "").IndexOf(search.Text, StringComparison.CurrentCultureIgnoreCase) >= 0;
                });
                filtered.Sort(delegate(PlayerDataModel a, PlayerDataModel b) {
                    int team = (b.RoleKnown && b.IsImpostor).CompareTo(a.RoleKnown && a.IsImpostor);
                    return team != 0 ? team : a.PlayerId.CompareTo(b.PlayerId);
                });
                while (table.Rows.Count > filtered.Count) table.Rows.RemoveAt(table.Rows.Count - 1);
                while (table.Rows.Count < filtered.Count) table.Rows.Add();
                for (int i = 0; i < filtered.Count; i++)
                {
                    var p = filtered[i]; var row = table.Rows[i]; row.Tag = p;
                    row.Cells[0].Value = (p.IsLocal ? "[Sen] " : "") + p.Name;
                    row.Cells[1].Value = p.RoleName + (p.IsDead ? " (olu)" : "");
                    row.Cells[2].Value = double.IsNaN(p.Distance) ? "-" : p.Distance.ToString("F1");
                    row.DefaultCellStyle.ForeColor = RadarCanvas.TeamColor(p);
                    row.Selected = canvas.SelectedPlayerId == p.PlayerId;
                }
            }
            finally { refreshing = false; }
        }

        private void UpdateDetails()
        {
            PlayerDataModel p = snapshot.Find(delegate(PlayerDataModel item) { return item.PlayerId == canvas.SelectedPlayerId; });
            details.Text = p == null ? "Oyuncu secilmedi" : p.Name + " | " + p.RoleName + (p.IsDead ? " | Olu" : " | Canli") + "\r\n"
                + (p.HasPosition ? string.Format("X: {0:F2}   Y: {1:F2}", p.PositionX, p.PositionY) : "Konum okunamadi");
        }

        public void ToggleHud() { hudToggle.Checked = !hudToggle.Checked; }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (timer != null) timer.Dispose(); if (hud != null) hud.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
