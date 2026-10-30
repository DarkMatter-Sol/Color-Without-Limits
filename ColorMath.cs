namespace ColorWithoutLimits;

/// <summary>
/// Byte layout from 9945ff-V8.reg. Alpha on every DWORD is 00. Palette alpha byte is FF.
/// </summary>
public static class ColorMath
{
    public static bool TryParseHex(string? text, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var s = text.Trim();
        if (s.StartsWith('#')) s = s[1..];
        if (s.Length != 6) return false;
        for (var i = 0; i < 6; i++)
        {
            if (!Uri.IsHexDigit(s[i])) return false;
        }
        r = Convert.ToByte(s[..2], 16);
        g = Convert.ToByte(s.Substring(2, 2), 16);
        b = Convert.ToByte(s.Substring(4, 2), 16);
        return true;
    }

    public static string Hex(byte r, byte g, byte b) => $"#{r:X2}{g:X2}{b:X2}";

    public static string RgbString(byte r, byte g, byte b) => $"{r} {g} {b}";

    /// <summary>00BBGGRR. High byte stays 00. #9945FF is 00ff4599.</summary>
    public static uint ToDword(byte r, byte g, byte b) => (uint)((b << 16) | (g << 8) | r);

    public static string DwordText(uint dword) => dword.ToString("x8");

    /// <summary>
    /// 8 colors, R,G,B,FF. Slots 1-4 step toward white. Slots 5-8 are the exact color.
    /// Same steps for every hex: 52%, 39%, 28%, 13%.
    /// </summary>
    public static byte[] AccentPalette(byte r, byte g, byte b)
    {
        double[] towardWhite = [0.52, 0.39, 0.28, 0.13];
        var pal = new byte[32];
        for (var i = 0; i < 4; i++)
        {
            var t = towardWhite[i];
            pal[i * 4] = MixWhite(r, t);
            pal[i * 4 + 1] = MixWhite(g, t);
            pal[i * 4 + 2] = MixWhite(b, t);
            pal[i * 4 + 3] = 0xFF;
        }
        for (var i = 4; i < 8; i++)
        {
            pal[i * 4] = r;
            pal[i * 4 + 1] = g;
            pal[i * 4 + 2] = b;
            pal[i * 4 + 3] = 0xFF;
        }
        return pal;
    }

    /// <summary>Palette slot 2. The quick-settings button.</summary>
    public static (byte R, byte G, byte B) ButtonFrom(byte r, byte g, byte b)
    {
        var pal = AccentPalette(r, g, b);
        return (pal[4], pal[5], pal[6]);
    }

    /// <summary>Same hue for every hex. Saturation 42%. Lightness 47%.</summary>
    public static (byte R, byte G, byte B) InactiveFrom(byte r, byte g, byte b)
    {
        RgbToHsl(r, g, b, out var h, out var s, out var l);
        HslToRgb(h, s * 0.42, l * 0.47, out var rr, out var gg, out var bb);
        return (rr, gg, bb);
    }

    /// <summary>
    /// Shade Settings rebuilds into the dark palette slots (the one that turns the taskbar dark).
    /// Display only. Never written.
    /// </summary>
    public static (byte R, byte G, byte B) SettingsWouldDarken(byte r, byte g, byte b) => Darken(r, g, b);

    static (byte R, byte G, byte B) Darken(byte r, byte g, byte b)
    {
        RgbToHsl(r, g, b, out var h, out var s, out var l);
        var next = l * 0.55;
        if (Math.Abs(next - l) < 0.02) next = l * 0.70;
        if (next < 0) next = 0;
        HslToRgb(h, s, next, out var rr, out var gg, out var bb);
        return (rr, gg, bb);
    }

    static byte MixWhite(byte channel, double t)
    {
        var v = (int)Math.Round(channel + (255 - channel) * t);
        if (v < 0) return 0;
        if (v > 255) return 255;
        return (byte)v;
    }

    static void RgbToHsl(byte r, byte g, byte b, out double h, out double s, out double l)
    {
        var rf = r / 255d;
        var gf = g / 255d;
        var bf = b / 255d;
        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        l = (max + min) / 2d;
        if (Math.Abs(max - min) < 0.00001)
        {
            h = 0;
            s = 0;
            return;
        }
        var d = max - min;
        s = l > 0.5 ? d / (2d - max - min) : d / (max + min);
        if (Math.Abs(max - rf) < 0.00001) h = ((gf - bf) / d + (gf < bf ? 6 : 0)) / 6d;
        else if (Math.Abs(max - gf) < 0.00001) h = ((bf - rf) / d + 2) / 6d;
        else h = ((rf - gf) / d + 4) / 6d;
    }

    static void HslToRgb(double h, double s, double l, out byte r, out byte g, out byte b)
    {
        if (s <= 0)
        {
            var v = (byte)Math.Clamp((int)Math.Round(l * 255), 0, 255);
            r = g = b = v;
            return;
        }
        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        r = Hue(p, q, h + 1d / 3d);
        g = Hue(p, q, h);
        b = Hue(p, q, h - 1d / 3d);
    }

    static byte Hue(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        double v;
        if (t < 1d / 6d) v = p + (q - p) * 6 * t;
        else if (t < 1d / 2d) v = q;
        else if (t < 2d / 3d) v = p + (q - p) * (2d / 3d - t) * 6;
        else v = p;
        return (byte)Math.Clamp((int)Math.Round(v * 255), 0, 255);
    }
}
