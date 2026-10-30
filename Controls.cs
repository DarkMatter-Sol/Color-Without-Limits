namespace ColorWithoutLimits;

internal sealed class BorderBox : Panel
{
    public BorderBox()
    {
        BackColor = Color.Black;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate(true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        using var pen = new Pen(Color.FromArgb(0x9A, 0x9A, 0x9A));
        e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
    }
}

internal sealed class AppButton : Button
{
    bool _hover;
    bool _press;

    public AppButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _press = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) _press = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _press = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var fill = BackColor;
        var fore = ForeColor;
        var border = FlatAppearance.BorderColor;
        if (Enabled && _press)
        {
            fill = FlatAppearance.MouseDownBackColor;
            border = fill;
        }
        else if (Enabled && _hover)
        {
            fill = FlatAppearance.MouseOverBackColor;
            border = fill;
        }
        e.Graphics.Clear(fill);
        using var pen = new Pen(border);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
