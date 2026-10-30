namespace ColorWithoutLimits;

internal sealed partial class MainForm
{
    void Fill(FlowLayoutPanel page)
    {
        _activeBox = MakeHexBox();
        _activeSwatch = new Panel { BackColor = Color.Black, Dock = DockStyle.Fill };
        _inactiveSwatch = new Panel { BackColor = Color.Black, Dock = DockStyle.Fill };
        _activeExact = MakeLabel("", _hintFont, Color.White);
        _inactiveExact = MakeLabel("", _hintFont, Color.White);
        _buttonExact = MakeLabel("", _hintFont, Color.White);
        _buttonTile = new Panel { BackColor = Color.Black, Dock = DockStyle.Fill };
        _pickActive = MakeButton("PICK COLOR", "Pick color");
        _pickActive.Click += (_, _) => PickInto(_activeBox);
        _activeBox.TextChanged += (_, _) => RefreshPreview();

        page.Controls.Add(MakeLabel("1  ACTIVE WINDOWS/TASKBAR", _sectionFont, Color.White));
        page.Controls.Add(MakeLabel("Enter a hex code or use PICK COLOR.", _hintFont, Color.White));
        page.Controls.Add(Row(_activeBox, _pickActive));
        page.Controls.Add(SwatchBox(_activeSwatch));
        page.Controls.Add(_activeExact);

        var step2 = MakeLabel("2  INACTIVE WINDOWS/ACTIVE BUTTONS", _sectionFont, Color.White);
        step2.Margin = new Padding(0, 16, 0, 2);
        page.Controls.Add(step2);
        page.Controls.Add(PreviewPair());
        var step3 = MakeLabel("3  APPLY", _sectionFont, Color.White);
        step3.Margin = new Padding(0, 16, 0, 2);
        page.Controls.Add(step3);
        page.Controls.Add(MakeLabel("After APPLY, sign out then back in.", _noteFont, Silver));
        page.Controls.Add(MakeLabel("After RESET, sign out then back in.", _noteFont, Silver));
        page.Controls.Add(MakeLabel("After RESTORE, sign out then back in.", _noteFont, Silver));
    }

    static FlowLayoutPanel Page()
    {
        var page = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Black,
            Padding = new Padding(16, 8, 16, 8),
            Margin = Padding.Empty,
        };
        page.Resize += (_, _) =>
        {
            var w = Math.Max(200, page.ClientSize.Width - page.Padding.Horizontal - 8);
            foreach (Control c in page.Controls)
            {
                if (c.Tag as string == "swatch")
                {
                    c.Invalidate(true);
                    continue;
                }
                if (c is Label label)
                {
                    label.MaximumSize = new Size(w, 0);
                    label.Invalidate();
                    continue;
                }
                if (c.Width != w) c.Width = w;
                c.Invalidate(true);
            }
            page.Invalidate(true);
        };
        return page;
    }

    void LockToContent(FlowLayoutPanel page, Control title, Control bottom)
    {
        var clientW = 32 + ButtonSize.Width * 3 + 16 + 420;
        ClientSize = new Size(clientW, ClientSize.Height);
        page.PerformLayout();
        var contentBottom = 0;
        foreach (Control c in page.Controls)
            contentBottom = Math.Max(contentBottom, c.Bottom + c.Margin.Bottom);
        var clientH = contentBottom + page.Padding.Vertical + title.Height + bottom.Height + 8;
        ClientSize = new Size(clientW, clientH);
        var locked = Size;
        MinimumSize = locked;
        MaximumSize = locked;
    }

    Panel PreviewPair()
    {
        var row = new Panel
        {
            Tag = "swatch",
            Size = new Size(432, 96),
            MinimumSize = new Size(432, 96),
            MaximumSize = new Size(432, 96),
            BackColor = Color.Black,
            Margin = new Padding(0, 6, 0, 4),
        };
        var windows = MakeLabel("WINDOWS", _noteFont, Color.White);
        var buttons = MakeLabel("BUTTONS", _noteFont, Color.White);
        windows.Location = new Point(0, 0);
        buttons.Location = new Point(224, 0);
        var frame = new BorderBox
        {
            Location = new Point(0, 22),
            Size = new Size(200, 44),
            Padding = new Padding(4),
        };
        frame.Controls.Add(_inactiveSwatch);
        var buttonFrame = new BorderBox
        {
            Location = new Point(224, 22),
            Size = new Size(200, 44),
            Padding = new Padding(4),
        };
        _buttonTile.Dock = DockStyle.Fill;
        buttonFrame.Controls.Add(_buttonTile);
        _inactiveExact.Location = new Point(0, 72);
        _buttonExact.Location = new Point(224, 72);
        row.Controls.Add(windows);
        row.Controls.Add(buttons);
        row.Controls.Add(frame);
        row.Controls.Add(buttonFrame);
        row.Controls.Add(_inactiveExact);
        row.Controls.Add(_buttonExact);
        return row;
    }

    static Panel Row(TextBox box, Button button)
    {
        var row = new Panel { Height = 40, BackColor = Color.Black, Margin = new Padding(0, 6, 0, 6) };
        var frame = new BorderBox { Location = new Point(0, 4), Size = new Size(152, ButtonSize.Height), Padding = new Padding(6, 6, 6, 0) };
        box.Dock = DockStyle.Fill;
        box.Width = 140;
        frame.Controls.Add(box);
        button.Location = new Point(168, 4);
        row.Controls.Add(frame);
        row.Controls.Add(button);
        return row;
    }

    static Panel SwatchBox(Panel swatch)
    {
        var box = new BorderBox
        {
            Tag = "swatch",
            Size = new Size(200, 44),
            MinimumSize = new Size(200, 44),
            MaximumSize = new Size(200, 44),
            Margin = new Padding(0, 4, 0, 4),
            Padding = new Padding(4),
        };
        box.Controls.Add(swatch);
        box.Resize += (_, _) => box.Invalidate(true);
        return box;
    }

    static Label MakeLabel(string text, Font font, Color color) => new()
    {
        Text = text,
        Font = font,
        ForeColor = color,
        BackColor = Color.Black,
        AutoSize = true,
        Margin = new Padding(0, 4, 0, 2),
        UseCompatibleTextRendering = false,
    };

    TextBox MakeHexBox()
    {
        var box = new TextBox
        {
            Font = _hintFont,
            BackColor = Color.Black,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            Width = 148,
            MaxLength = 7,
            CharacterCasing = CharacterCasing.Upper,
        };
        box.TextChanged += (_, _) => KeepSixDigits(box);
        return box;
    }

    static void KeepSixDigits(TextBox box)
    {
        var raw = box.Text.Trim();
        if (raw.StartsWith('#')) raw = raw[1..];
        var hex = new string(raw.Where(Uri.IsHexDigit).Take(6).ToArray());
        if (hex == box.Text) return;
        var caret = Math.Min(box.SelectionStart, hex.Length);
        box.Text = hex;
        box.SelectionStart = caret;
    }
}
