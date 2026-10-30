using System.Security.Principal;

namespace ColorWithoutLimits;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(args));
    }

    public static bool IsAdmin()
    {
        using var id = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }
}

internal sealed partial class MainForm : Form
{
    TextBox _activeBox;
    Panel _activeSwatch;
    Panel _inactiveSwatch;
    Label _activeExact;
    Label _inactiveExact;
    Label _buttonExact;
    Panel _buttonTile;
    readonly Label _status;
    readonly Button _apply;
    readonly Button _restore;
    readonly Button _reset;
    Button _pickActive;
    readonly Font _titleFont;
    readonly Font _sectionFont;
    readonly Font _hintFont;
    readonly Font _noteFont;
    readonly Font _buttonFont;
    bool _applyReady;
    bool _restoreReady;
    bool _autoApply;
    bool _autoReset;
    bool _autoRestore;

    static readonly Color IdleFill = Color.Black;
    static readonly Color IdleText = Color.FromArgb(0x9A, 0x9A, 0x9A);
    static readonly Color IdleHover = Color.FromArgb(0x22, 0x22, 0x22);
    static readonly Color IdlePress = Color.FromArgb(0x33, 0x33, 0x33);
    static readonly Color ReadyFill = Color.White;
    static readonly Color ReadyText = Color.Black;
    static readonly Color ReadyHover = Color.FromArgb(0xD8, 0xD8, 0xD8);
    static readonly Color ReadyPress = Color.FromArgb(0xB0, 0xB0, 0xB0);
    static readonly Color Silver = Color.FromArgb(0xC0, 0xC0, 0xC0);
    static Size ButtonSize = new(168, 32);
    static readonly Padding ButtonPadding = new(4, 0, 4, 0);

    public MainForm(string[] args)
    {
        Text = "COLOR WITHOUT LIMITS";
        BackColor = Color.Black;
        ForeColor = Color.White;
        Font = new Font("Consolas", 10f, FontStyle.Regular);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        SizeGripStyle = SizeGripStyle.Hide;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        AutoScroll = false;

        _titleFont = new Font("Consolas", 18f, FontStyle.Bold);
        _sectionFont = new Font("Consolas", 13f, FontStyle.Bold);
        _hintFont = new Font("Consolas", 10f, FontStyle.Regular);
        _noteFont = new Font("Consolas", 9f, FontStyle.Regular);
        _buttonFont = new Font("Consolas", 10f, FontStyle.Bold);
        ButtonSize = FitButtonSize(_buttonFont);

        var title = MakeLabel("> COLOR WITHOUT LIMITS", _titleFont, Color.White);
        title.Dock = DockStyle.Top;
        title.Padding = new Padding(16, 14, 16, 6);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.Black };
        _status = MakeLabel("> ready", _hintFont, Color.White);
        _status.AutoSize = false;
        _status.Height = 24;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        _status.Location = new Point(16, 16);
        _apply = MakeButton("APPLY", "Apply colors");
        _restore = MakeButton("RESTORE", "Restore backup");
        _reset = MakeButton("RESET", "Reset colors");
        _apply.Click += (_, _) => OnApply();
        _restore.Click += (_, _) => OnRestore();
        _reset.Click += (_, _) => OnReset();
        bottom.Controls.Add(_status);
        bottom.Controls.Add(_restore);
        bottom.Controls.Add(_reset);
        bottom.Controls.Add(_apply);
        bottom.Resize += (_, _) => PlaceBottom(bottom);

        var page = Page();
        Fill(page);

        Controls.Add(page);
        Controls.Add(bottom);
        Controls.Add(title);

        AcceptButton = _apply;
        ParseArgs(args);
        RefreshPreview();
        Shown += (_, _) =>
        {
            LockToContent(page, title, bottom);
            if (_autoReset)
            {
                _autoReset = false;
                OnReset();
                return;
            }
            if (_autoRestore)
            {
                _autoRestore = false;
                OnRestore();
                return;
            }
            if (!_autoApply) return;
            _autoApply = false;
            OnApply();
        };
    }
}
