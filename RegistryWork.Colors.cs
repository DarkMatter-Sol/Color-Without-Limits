using System.Text.Json;
using Microsoft.Win32;

namespace ColorWithoutLimits;

public static partial class RegistryWork
{
    static void WriteColors(string hive, string key, string activeText, string inactiveText)
    {
        foreach (var name in ActiveNames) WriteString(hive, key, name, activeText);
        foreach (var name in InactiveNames) WriteString(hive, key, name, inactiveText);
        WriteString(hive, key, "TitleText", "255 255 255");
        WriteString(hive, key, "InactiveTitleText", "180 180 180");
    }

    static void BackupApply()
    {
        var file = LoadBackup();
        var known = Known(file);
        foreach (var (hive, prefix) in Both())
        {
            SnapshotAll(file, known, hive, prefix + AccentKey);
            Snapshot(file, known, hive, prefix + DwmKey,
                "ColorPrevalence", "EnableWindowColorization", "AccentColor", "AccentColorInactive",
                "ColorizationColor", "ColorizationAfterglow");
            Snapshot(file, known, hive, prefix + PersonalizeKey,
                "ColorPrevalence", "EnableTransparency", "SystemUsesLightTheme", "AppsUseLightTheme");
            SnapshotColors(file, known, hive, prefix + ColorsKey);
        }
        Snapshot(file, known, "HKCU", DesktopKey, "AutoColorization");
        SnapshotColors(file, known, "HKCU", DesktopColorsKey);
        SaveBackup(file);
    }

    static void BackupReset()
    {
        var file = LoadBackup();
        var known = Known(file);
        SnapshotAll(file, known, "HKCU", AccentKey);
        SnapshotAll(file, known, "HKU", @".DEFAULT\" + AccentKey);
        SnapshotAll(file, known, "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Themes\History\Colors");
        SnapshotAll(file, known, "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Themes\Accent");
        SnapshotAll(file, known, "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\AccentColorInfo");
        SnapshotAll(file, known, "HKCU", DwmKey);
        SnapshotAll(file, known, "HKU", @".DEFAULT\" + DwmKey);
        SnapshotAll(file, known, "HKCU", PersonalizeKey);
        SnapshotAll(file, known, "HKU", @".DEFAULT\" + PersonalizeKey);
        Snapshot(file, known, "HKCU", DesktopKey, "AutoColorization");
        SnapshotAll(file, known, "HKCU", ColorsKey);
        SnapshotAll(file, known, "HKU", @".DEFAULT\" + ColorsKey);
        SnapshotAll(file, known, "HKCU", DesktopColorsKey);
        SnapshotAll(file, known, "HKU", @".DEFAULT\" + DesktopColorsKey);
        Snapshot(file, known, "HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "Background");
        Snapshot(file, known, "HKLM", @"SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon");
        Snapshot(file, known, "HKLM", @"Software\Policies\Microsoft\Windows\Personalization",
            "PersonalColors_Background", "PersonalColors_Accent", "NoLockScreen");
        SaveBackup(file);
    }

    static void ClearDwm(string hive, string key, bool includeSafeMode)
    {
        string[] names =
        [
            "AccentColor", "AccentColorInactive", "AccentColorMenu", "StartColorMenu", "AccentPalette",
            "EnableWindowColorization", "ColorizationColor", "ColorizationAfterglow",
            "ColorizationColorBalance", "ColorizationAfterglowBalance", "ColorizationBlurBalance",
            "ColorizationGlassAttribute", "ColorizationOpaqueBlend", "EnableBlurBehind",
        ];
        foreach (var name in names) DeleteValue(hive, key, name);
        if (includeSafeMode) DeleteValue(hive, key, "DisabledBySafeMode");
        WriteDword(hive, key, "ColorPrevalence", 0);
    }

    static void ClearPersonalize(string hive, string key)
    {
        foreach (var name in new[]
        {
            "EnabledBlurBehind", "EnableWindowColorization", "AccentColor", "StartColorMenu",
            "AccentColorMenu", "AccentPalette", "TaskbarColorOverride",
        })
            DeleteValue(hive, key, name);
        WriteDword(hive, key, "ColorPrevalence", 0);
        WriteDword(hive, key, "EnableTransparency", 1);
        WriteDword(hive, key, "AppsUseLightTheme", 0);
        WriteDword(hive, key, "SystemUsesLightTheme", 0);
    }

    static void WriteStock(string hive, string key, (string Name, string Value)[] rows)
    {
        foreach (var (name, value) in rows) WriteString(hive, key, name, value);
    }
}
