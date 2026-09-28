using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AmongUsExternal
{
    public class RadarCanvas : Control
    {
        private List<PlayerDataModel> players = new List<PlayerDataModel>();
        private readonly Dictionary<byte, PointF> points = new Dictionary<byte, PointF>();
        public bool DrawLines { get; set; }
        public bool DrawNames { get; set; }
        public bool ShowDead { get; set; }
        public bool OnlyImpostors { get; set; }
        public bool FollowLocal { get; set; }
        public float Range { get; set; }
        public byte? SelectedPlayerId { get; set; }
        public event Action<byte> PlayerSelected;
        public int RenderedPlayers { get; private set; }
        public int RenderedLines { get; private set; }
        public Rectangle[] RenderedLabels { get; private set; }

        public RadarCanvas()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(18, 21, 23);
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 9);
            DrawLines = DrawNames = ShowDead = true;
            Range = 35;
            MinimumSize = new Size(220, 200);
        }

        public void SetPlayers(List<PlayerDataModel> snapshot)
        {
            players = snapshot ?? new List<PlayerDataModel>();
            Invalidate();
        }

        public static PointF Project(float x, float y, PointF center, float scale, RectangleF viewport)
        {
            return new PointF(viewport.Left + viewport.Width / 2 + (x - center.X) * scale,
                viewport.Top + viewport.Height / 2 - (y - center.Y) * scale);
        }

        public static Color TeamColor(PlayerDataModel player)
        {
            if (player.IsLocal) return Color.FromArgb(78, 218, 181);
            if (player.IsDead) return Color.FromArgb(136, 144, 149);
            if (!player.RoleKnown) return Color.FromArgb(238, 195, 91);
            return player.IsImpostor ? Color.FromArgb(255, 94, 107) : Color.FromArgb(115, 187, 242);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            points.Clear();
            RenderedPlayers = RenderedLines = 0;
            RenderedLabels = new Rectangle[0];
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF view = new RectangleF(30, 34, Math.Max(1, Width - 60), Math.Max(1, Height - 68));
            var visible = players.FindAll(delegate(PlayerDataModel p)
            {
                return p.HasPosition && (ShowDead || !p.IsDead || p.IsLocal)
                    && (!OnlyImpostors || (p.RoleKnown && p.IsImpostor) || p.IsLocal);
            });
            PlayerDataModel local = players.Find(delegate(PlayerDataModel p) { return p.IsLocal && p.HasPosition; });
            PointF center = local == null ? PointF.Empty : new PointF(local.PositionX, local.PositionY);
            float worldWidth = Math.Max(5, Range);
            if (!FollowLocal && visible.Count > 0)
            {
                float minX = visible[0].PositionX, maxX = minX, minY = visible[0].PositionY, maxY = minY;
                foreach (var p in visible)
                {
                    minX = Math.Min(minX, p.PositionX); maxX = Math.Max(maxX, p.PositionX);
                    minY = Math.Min(minY, p.PositionY); maxY = Math.Max(maxY, p.PositionY);
                }
                center = new PointF((minX + maxX) / 2, (minY + maxY) / 2);
                worldWidth = Math.Max(worldWidth, Math.Max(maxX - minX, maxY - minY) + 8);
            }
            float scale = Math.Min(view.Width, view.Height) / worldWidth;
            using (var grid = new Pen(Color.FromArgb(37, 43, 46)))
            using (var axis = new Pen(Color.FromArgb(66, 78, 83)))
            {
                float gridStep = worldWidth > 100 ? 20 : (worldWidth > 50 ? 10 : 5);
                for (float x = (float)Math.Floor((center.X - view.Width / scale / 2) / gridStep) * gridStep;
                    x <= center.X + view.Width / scale / 2; x += gridStep)
                {
                    PointF point = Project(x, center.Y, center, scale, view);
                    g.DrawLine(Math.Abs(x) < 0.01 ? axis : grid, point.X, view.Top, point.X, view.Bottom);
                }
                for (float y = (float)Math.Floor((center.Y - view.Height / scale / 2) / gridStep) * gridStep;
                    y <= center.Y + view.Height / scale / 2; y += gridStep)
                {
                    PointF point = Project(center.X, y, center, scale, view);
                    g.DrawLine(Math.Abs(y) < 0.01 ? axis : grid, view.Left, point.Y, view.Right, point.Y);
                }
            }
            PointF origin = local == null ? Project(center.X, center.Y, center, scale, view)
                : Project(local.PositionX, local.PositionY, center, scale, view);
            var labels = new List<Rectangle>();
            foreach (var p in visible)
            {
                PointF projected = Project(p.PositionX, p.PositionY, center, scale, view);
                points[p.PlayerId] = new PointF(Math.Max(view.Left, Math.Min(view.Right, projected.X)),
                    Math.Max(view.Top, Math.Min(view.Bottom, projected.Y)));
            }
            foreach (var p in visible)
            {
                PointF projected = Project(p.PositionX, p.PositionY, center, scale, view);
                PointF point = new PointF(Math.Max(view.Left, Math.Min(view.Right, projected.X)),
                    Math.Max(view.Top, Math.Min(view.Bottom, projected.Y)));
                points[p.PlayerId] = point;
                Color color = TeamColor(p);
                bool selected = SelectedPlayerId == p.PlayerId;
                if (DrawLines && local != null && !p.IsLocal)
                {
                    using (var line = new Pen(Color.FromArgb(selected ? 230 : 100, color), selected ? 2 : 1))
                        g.DrawLine(line, origin, point);
                    RenderedLines++;
                }
                using (var brush = new SolidBrush(color))
                    g.FillEllipse(brush, point.X - 5, point.Y - 5, 10, 10);
                if (selected || p.IsLocal)
                    using (var ring = new Pen(selected ? Color.White : color, 2))
                        g.DrawEllipse(ring, point.X - 9, point.Y - 9, 18, 18);
                if (p.IsDead)
                    using (var pen = new Pen(BackColor, 2))
                    {
                        g.DrawLine(pen, point.X - 3, point.Y - 3, point.X + 3, point.Y + 3);
                        g.DrawLine(pen, point.X - 3, point.Y + 3, point.X + 3, point.Y - 3);
                    }
                if (DrawNames)
                {
                    string name = (p.IsLocal ? "SEN: " : "") + p.Name;
                    if (p.RoleKnown && p.IsImpostor) name += " [I]";
                    if (!p.RoleKnown) name += " [?]";
                    if (!double.IsNaN(p.Distance) && !p.IsLocal) name += " " + p.Distance.ToString("F1");
                    int width = Math.Min(Math.Max(80, TextRenderer.MeasureText(name, Font).Width + 5), Math.Max(80, Width - 18));
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        int left = (int)Math.Max(4, Math.Min(Width - width - 4, attempt % 2 == 0 ? point.X + 10 : point.X - width - 10));
                        int top = (int)Math.Max(30, Math.Min(Height - 48, point.Y - 9 + (attempt / 2) * 23));
                        Rectangle label = new Rectangle(left, top, width, 21);
                        if (labels.Exists(delegate(Rectangle other) { return other.IntersectsWith(label); })) continue;
                        bool coversMarker = false;
                        foreach (PointF marker in points.Values)
                            if (label.IntersectsWith(new Rectangle((int)marker.X - 10, (int)marker.Y - 10, 20, 20)))
                            { coversMarker = true; break; }
                        if (coversMarker) continue;
                        labels.Add(label);
                        TextRenderer.DrawText(g, name, Font, label, color, BackColor,
                            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
                        break;
                    }
                }
                RenderedPlayers++;
            }
            RenderedLabels = labels.ToArray();
            string status = visible.Count == 0 ? "Konum verisi bekleniyor" : visible.Count + " konum  |  " + (local == null ? "Yerel oyuncu yok" : "Canli radar");
            TextRenderer.DrawText(g, status, Font, new Rectangle(12, 6, Width - 24, 22), ForeColor,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "X + sag   /   Y + yukari   |   " + worldWidth.ToString("F0") + " birim",
                Font, new Rectangle(12, Height - 28, Width - 24, 20), Color.DarkGray,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            foreach (var pair in points)
                if (Math.Abs(pair.Value.X - e.X) <= 12 && Math.Abs(pair.Value.Y - e.Y) <= 12)
                {
                    SelectedPlayerId = pair.Key;
                    if (PlayerSelected != null) PlayerSelected(pair.Key);
                    Invalidate();
                    break;
                }
        }
    }
}
