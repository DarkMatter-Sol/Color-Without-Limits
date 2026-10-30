using System.Text.Json;
using Microsoft.Win32;

namespace ColorWithoutLimits;

public static partial class RegistryWork
{
    const string AccentKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Accent";
    const string DwmKey = @"SOFTWARE\Microsoft\Windows\DWM";
    const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    const string DesktopKey = @"Control Panel\Desktop";
    const string ColorsKey = @"Control Panel\Colors";
    const string DesktopColorsKey = @"Control Panel\Desktop\Colors";

    static readonly string[] ActiveNames =
    [
        "ActiveBorder", "ActiveTitle", "GradientActiveTitle",
        "Hilight", "HotTrackingColor", "MenuHilight",
    ];

    static readonly string[] InactiveNames =
    [
        "InactiveTitle", "InactiveBorder", "GradientInactiveTitle",
    ];

    public static string BackupPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ColorWithoutLimits", "backup.json");

    public static bool HasBackup() => File.Exists(BackupPath);

    public static void Apply(byte ar, byte ag, byte ab, byte ir, byte ig, byte ib)
    {
        var activeDword = ColorMath.ToDword(ar, ag, ab);
        var inactiveDword = ColorMath.ToDword(ir, ig, ib);
        var palette = ColorMath.AccentPalette(ar, ag, ab);
        var activeText = ColorMath.RgbString(ar, ag, ab);
        var inactiveText = ColorMath.RgbString(ir, ig, ib);

        BackupApply();

        // Working file wipes this key first so old palette values cannot stick.
        DeleteKey("HKU", @".DEFAULT\" + AccentKey);

        foreach (var (hive, prefix) in Both())
        {
            WriteDword(hive, prefix + AccentKey, "AccentColorMenu", activeDword);
            WriteDword(hive, prefix + AccentKey, "StartColorMenu", activeDword);
            WriteDword(hive, prefix + AccentKey, "AccentColor", activeDword);
            WriteBinary(hive, prefix + AccentKey, "AccentPalette", palette);

            WriteDword(hive, prefix + DwmKey, "ColorPrevalence", 1);
            WriteDword(hive, prefix + DwmKey, "EnableWindowColorization", 1);
            WriteDword(hive, prefix + DwmKey, "AccentColor", activeDword);
            WriteDword(hive, prefix + DwmKey, "AccentColorInactive", inactiveDword);
            WriteDword(hive, prefix + DwmKey, "ColorizationColor", activeDword);
            WriteDword(hive, prefix + DwmKey, "ColorizationAfterglow", activeDword);

            WriteDword(hive, prefix + PersonalizeKey, "ColorPrevalence", 1);
            WriteDword(hive, prefix + PersonalizeKey, "EnableTransparency", 1);
            WriteDword(hive, prefix + PersonalizeKey, "SystemUsesLightTheme", 0);
            WriteDword(hive, prefix + PersonalizeKey, "AppsUseLightTheme", 0);

            WriteColors(hive, prefix + ColorsKey, activeText, inactiveText);
        }

        WriteDword("HKCU", DesktopKey, "AutoColorization", 0);
        WriteColors("HKCU", DesktopColorsKey, activeText, inactiveText);

        Broadcast();
    }

    /// <summary>Same actions as Reset-Everything-Colors. Returns false if HKLM was skipped.</summary>
    public static bool Reset()
    {
        BackupReset();

        DeleteKey("HKCU", AccentKey);
        DeleteKey("HKU", @".DEFAULT\" + AccentKey);
        DeleteKey("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Themes\History\Colors");
        DeleteKey("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Themes\Accent");
        DeleteKey("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\AccentColorInfo");

        ClearDwm("HKCU", DwmKey, includeSafeMode: true);
        ClearDwm("HKU", @".DEFAULT\" + DwmKey, includeSafeMode: false);
        ClearPersonalize("HKCU", PersonalizeKey);
        ClearPersonalize("HKU", @".DEFAULT\" + PersonalizeKey);

        WriteDword("HKCU", DesktopKey, "AutoColorization", 0);
        WriteStock("HKCU", ColorsKey, StockColors);
        WriteStock("HKU", @".DEFAULT\" + ColorsKey, StockColors);
        WriteStock("HKCU", DesktopColorsKey, StockDesktopColors);
        WriteStock("HKU", @".DEFAULT\" + DesktopColorsKey, StockDesktopColors);

        var hklm = true;
        try
        {
            DeleteValue("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "Background");
            DeleteValue("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon");
            DeleteValue("HKLM", @"Software\Policies\Microsoft\Windows\Personalization", "PersonalColors_Background");
            DeleteValue("HKLM", @"Software\Policies\Microsoft\Windows\Personalization", "PersonalColors_Accent");
            DeleteValue("HKLM", @"Software\Policies\Microsoft\Windows\Personalization", "NoLockScreen");
        }
        catch (UnauthorizedAccessException)
        {
            hklm = false;
        }

        Broadcast();
        return hklm;
    }

    public static void Restore()
    {
        if (!File.Exists(BackupPath)) return;
        var file = JsonSerializer.Deserialize<BackupFile>(File.ReadAllText(BackupPath))
                   ?? throw new InvalidDataException("backup is empty");
        foreach (var entry in file.Entries)
        {
            using var root = OpenRoot(entry.Hive);
            if (!entry.Existed)
            {
                using var key = root.OpenSubKey(entry.Key, writable: true);
                key?.DeleteValue(entry.Name, throwOnMissingValue: false);
                continue;
            }
            using var created = root.CreateSubKey(entry.Key, writable: true)
                                ?? throw new InvalidOperationException("could not open " + entry.Key);
            switch (entry.Kind)
            {
                case "DWord":
                    created.SetValue(entry.Name, int.Parse(entry.Data ?? "0"), RegistryValueKind.DWord);
                    break;
                case "QWord":
                    created.SetValue(entry.Name, long.Parse(entry.Data ?? "0"), RegistryValueKind.QWord);
                    break;
                case "String":
                    created.SetValue(entry.Name, entry.Data ?? "", RegistryValueKind.String);
                    break;
                case "ExpandString":
                    created.SetValue(entry.Name, entry.Data ?? "", RegistryValueKind.ExpandString);
                    break;
                case "Binary":
                    created.SetValue(entry.Name, Convert.FromHexString(entry.Data ?? ""), RegistryValueKind.Binary);
                    break;
                case "MultiString":
                    created.SetValue(entry.Name, (entry.Data ?? "").Split('\n'), RegistryValueKind.MultiString);
                    break;
                default:
                    throw new InvalidDataException("unknown kind " + entry.Kind);
            }
        }
        File.Delete(BackupPath);
        Broadcast();
    }
}
