namespace ColorWithoutLimits;

internal sealed partial class MainForm
{
    Button MakeButton(string text, string accessibleName)
    {
        var b = new AppButton
        {
            Text = text,
            AccessibleName = accessibleName,
            Font = _buttonFont,
            Size = ButtonSize,
            Padding = ButtonPadding,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            TabStop = true,
            Margin = Padding.Empty,
        };
        b.FlatAppearance.BorderSize = 1;
        StyleButton(b, ready: false);
        return b;
    }

    static void StyleButton(Button b, bool ready)
    {
        b.Enabled = ready;
        if (ready)
        {
            b.BackColor = ReadyFill;
            b.ForeColor = ReadyText;
            b.FlatAppearance.BorderColor = ReadyFill;
            b.FlatAppearance.MouseOverBackColor = ReadyHover;
            b.FlatAppearance.MouseDownBackColor = ReadyPress;
        }
        else
        {
            b.BackColor = IdleFill;
            b.ForeColor = IdleText;
            b.FlatAppearance.BorderColor = IdleText;
            b.FlatAppearance.MouseOverBackColor = IdleHover;
            b.FlatAppearance.MouseDownBackColor = IdlePress;
        }
        b.Invalidate();
    }

    static Size FitButtonSize(Font font)
    {
        var widest = 0;
        foreach (var text in new[] { "APPLY", "RESTORE", "RESET", "PICK COLOR" })
            widest = Math.Max(widest, TextRenderer.MeasureText(text, font).Width);
        return new Size(widest + 28, 32);
    }

    void PlaceBottom(Panel bottom)
    {
        _apply.Location = new Point(bottom.ClientSize.Width - ButtonSize.Width - 16, 12);
        _reset.Location = new Point(_apply.Left - ButtonSize.Width - 8, 12);
        _restore.Location = new Point(_reset.Left - ButtonSize.Width - 8, 12);
        _status.Location = new Point(16, 16);
        _status.Width = Math.Max(80, _restore.Left - 28);
    }

    void RefreshPreview()
    {
        var activeOk = ColorMath.TryParseHex(_activeBox.Text, out var ar, out var ag, out var ab);
        if (activeOk)
        {
            _activeSwatch.BackColor = Color.FromArgb(ar, ag, ab);
            _activeExact.Text = ColorMath.Hex(ar, ag, ab);
            var inactive = ColorMath.InactiveFrom(ar, ag, ab);
            _inactiveSwatch.BackColor = Color.FromArgb(inactive.R, inactive.G, inactive.B);
            _inactiveExact.Text = ColorMath.Hex(inactive.R, inactive.G, inactive.B);
            var button = ColorMath.ButtonFrom(ar, ag, ab);
            _buttonTile.BackColor = Color.FromArgb(button.R, button.G, button.B);
            _buttonExact.Text = ColorMath.Hex(button.R, button.G, button.B);
        }
        else
        {
            _activeSwatch.BackColor = Color.Black;
            _activeExact.Text = "Enter a 6-digit hex";
            _inactiveSwatch.BackColor = Color.Black;
            _inactiveExact.Text = "";
            _buttonTile.BackColor = Color.Black;
            _buttonExact.Text = "";
        }

        _applyReady = activeOk;
        _restoreReady = RegistryWork.HasBackup();
        StyleButton(_apply, _applyReady);
        StyleButton(_restore, _restoreReady);
        StyleButton(_reset, true);
        StyleButton(_pickActive, true);
        _status.Text = _applyReady ? "> Ready" : "> Enter a 6-digit hex";
    }

    void PickInto(TextBox box)
    {
        using var dialog = new ColorDialog { FullOpen = true, AnyColor = true };
        if (ColorMath.TryParseHex(box.Text, out var r, out var g, out var b))
            dialog.Color = Color.FromArgb(r, g, b);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        box.Text = ColorMath.Hex(dialog.Color.R, dialog.Color.G, dialog.Color.B)[1..];
    }

    void OnApply()
    {
        if (!_applyReady) return;
        if (!ColorMath.TryParseHex(_activeBox.Text, out var ar, out var ag, out var ab)) return;
        var inactive = ColorMath.InactiveFrom(ar, ag, ab);

        if (!Program.IsAdmin())
        {
            Elevate($"--apply {ar:X2}{ag:X2}{ab:X2}");
            return;
        }

        try
        {
            var hadBackup = RegistryWork.HasBackup();
            RegistryWork.Apply(ar, ag, ab, inactive.R, inactive.G, inactive.B);
            RefreshPreview();
            _status.Text = hadBackup
                ? "> Applied. Original backup kept. Sign out, then back in."
                : "> Applied. Sign out, then back in.";
        }
        catch (Exception ex)
        {
            _status.Text = "> Failed: " + ex.Message;
        }
    }

    void OnRestore()
    {
        if (!_restoreReady && !_autoRestore) return;
        if (!RegistryWork.HasBackup()) return;
        if (!Program.IsAdmin())
        {
            Elevate("--restore");
            return;
        }
        try
        {
            RegistryWork.Restore();
            RefreshPreview();
            _status.Text = "> Restored. Sign out, then back in.";
        }
        catch (Exception ex)
        {
            _status.Text = "> Failed: " + ex.Message;
        }
    }

    void OnReset()
    {
        if (!Program.IsAdmin())
        {
            Elevate("--reset");
            return;
        }
        try
        {
            var hklm = RegistryWork.Reset();
            RefreshPreview();
            _status.Text = hklm
                ? "> Reset. Sign out, then back in."
                : "> Reset. HKLM skipped. Sign out, then back in.";
        }
        catch (Exception ex)
        {
            _status.Text = "> Failed: " + ex.Message;
        }
    }

    void Elevate(string arguments)
    {
        try
        {
            var exe = Environment.ProcessPath ?? Application.ExecutablePath;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = arguments,
            });
            Close();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            _status.Text = "> Admin cancelled. Nothing was written.";
        }
    }

    void ParseArgs(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--reset") _autoReset = true;
            if (args[i] == "--restore") _autoRestore = true;
            if (args[i] == "--apply" && i + 1 < args.Length)
            {
                _activeBox.Text = args[i + 1];
                _autoApply = true;
                i += 1;
            }
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _titleFont.Dispose();
        _sectionFont.Dispose();
        _hintFont.Dispose();
        _noteFont.Dispose();
        _buttonFont.Dispose();
        base.OnFormClosed(e);
    }
}
