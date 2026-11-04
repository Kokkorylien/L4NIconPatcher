using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace L4NIconPatcher
{
    internal static class AppTheme
    {
        internal static readonly Color Background = Color.FromArgb(246, 248, 251);
        internal static readonly Color Surface = Color.White;
        internal static readonly Color Text = Color.FromArgb(28, 37, 54);
        internal static readonly Color Muted = Color.FromArgb(106, 117, 135);
        internal static readonly Color Border = Color.FromArgb(222, 228, 237);
        internal static readonly Color Accent = Color.FromArgb(72, 87, 216);

        internal static Color ParentColor(Control parent)
        {
            while (parent != null)
            {
                var surface = parent as SurfacePanel;
                if (surface != null) return surface.FillColor;
                if (parent.BackColor.A == 255) return parent.BackColor;
                parent = parent.Parent;
            }
            return Background;
        }

        internal static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
        {
            float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class SurfacePanel : Panel
    {
        internal Color FillColor { get; set; } = AppTheme.Surface;
        internal bool HighlightFocus { get; set; }

        internal SurfacePanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            e.Control.GotFocus += ChildFocusChanged;
            e.Control.LostFocus += ChildFocusChanged;
        }

        private void ChildFocusChanged(object sender, EventArgs e) { Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(AppTheme.ParentColor(Parent));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = DeviceDpi / 96f;
            using (var shape = AppTheme.RoundedRectangle(new RectangleF(.5f, .5f, Width - 1, Height - 1), 10 * scale))
            using (var fill = new SolidBrush(FillColor))
            using (var border = new Pen(HighlightFocus && ContainsFocus ? AppTheme.Accent : AppTheme.Border))
            {
                e.Graphics.FillPath(fill, shape);
                e.Graphics.DrawPath(border, shape);
            }
        }
    }

    internal sealed class ModernButton : Button
    {
        internal bool Primary { get; set; }
        internal bool Quiet { get; set; }
        private bool hovered;
        private bool pressed;

        internal ModernButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
            UseVisualStyleBackColor = false;
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(AppTheme.ParentColor(Parent));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = Primary ? AppTheme.Accent : AppTheme.Surface;
            Color text = Primary ? Color.White : AppTheme.Text;
            if (Quiet) { fill = AppTheme.ParentColor(Parent); text = AppTheme.Muted; }
            if (hovered) fill = Primary ? Color.FromArgb(59, 72, 192) : Color.FromArgb(234, 238, 248);
            if (pressed) fill = Primary ? Color.FromArgb(48, 60, 170) : Color.FromArgb(222, 228, 242);
            if (!Enabled) { fill = Color.FromArgb(235, 238, 244); text = Color.FromArgb(148, 157, 173); }
            using (var shape = AppTheme.RoundedRectangle(new RectangleF(.5f, .5f, Width - 1, Height - 1), 9 * DeviceDpi / 96f))
            using (var brush = new SolidBrush(fill))
            using (var border = new Pen(Focused ? AppTheme.Accent : AppTheme.Border))
            {
                e.Graphics.FillPath(brush, shape);
                if ((!Primary && !Quiet) || Focused) e.Graphics.DrawPath(border, shape);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            if (Focused && ShowFocusCues)
            {
                Rectangle focus = ClientRectangle;
                focus.Inflate(-5, -5);
                ControlPaint.DrawFocusRectangle(e.Graphics, focus, text, fill);
            }
        }
    }
}
