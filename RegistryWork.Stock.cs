using System.Text.Json;
using Microsoft.Win32;

namespace ColorWithoutLimits;

public static partial class RegistryWork
{
    static readonly (string Name, string Value)[] StockColors =
    [
        ("ActiveBorder", "180 180 180"),
        ("ActiveTitle", "153 180 209"),
        ("AppWorkspace", "171 171 171"),
        ("Background", "0 0 0"),
        ("ButtonAlternateFace", "0 0 0"),
        ("ButtonDkShadow", "105 105 105"),
        ("ButtonFace", "240 240 240"),
        ("ButtonHilight", "255 255 255"),
        ("ButtonLight", "227 227 227"),
        ("ButtonShadow", "160 160 160"),
        ("ButtonText", "0 0 0"),
        ("GradientActiveTitle", "185 209 234"),
        ("GradientInactiveTitle", "215 228 242"),
        ("GrayText", "109 109 109"),
        ("Hilight", "51 153 255"),
        ("HilightText", "255 255 255"),
        ("HotTrackingColor", "0 102 204"),
        ("InactiveBorder", "244 247 252"),
        ("InactiveTitle", "191 205 219"),
        ("InactiveTitleText", "0 0 0"),
        ("InfoText", "0 0 0"),
        ("InfoWindow", "255 255 225"),
        ("Menu", "240 240 240"),
        ("MenuBar", "240 240 240"),
        ("MenuHilight", "51 153 255"),
        ("MenuText", "0 0 0"),
        ("Scrollbar", "200 200 200"),
        ("TitleText", "0 0 0"),
        ("Window", "255 255 255"),
        ("WindowFrame", "100 100 100"),
        ("WindowText", "0 0 0"),
    ];

    static readonly (string Name, string Value)[] StockDesktopColors =
    [
        ("ActiveBorder", "212 208 200"),
        ("ActiveTitle", "10 36 106"),
        ("AppWorkSpace", "128 128 128"),
        ("ButtonAlternateFace", "181 181 181"),
        ("ButtonDkShadow", "64 64 64"),
        ("ButtonFace", "212 208 200"),
        ("ButtonHiLight", "255 255 255"),
        ("ButtonLight", "212 208 200"),
        ("ButtonShadow", "128 128 128"),
        ("ButtonText", "0 0 0"),
        ("GradientActiveTitle", "166 202 240"),
        ("GradientInactiveTitle", "192 192 192"),
        ("GrayText", "128 128 128"),
        ("Hilight", "10 36 106"),
        ("HilightText", "255 255 255"),
        ("HotTrackingColor", "0 0 128"),
        ("InactiveBorder", "212 208 200"),
        ("InactiveTitle", "128 128 128"),
        ("InactiveTitleText", "212 208 200"),
        ("InfoText", "0 0 0"),
        ("InfoWindow", "255 255 255"),
        ("Menu", "212 208 200"),
        ("MenuText", "0 0 0"),
        ("Scrollbar", "212 208 200"),
        ("TitleText", "255 255 255"),
        ("Window", "255 255 255"),
        ("WindowFrame", "0 0 0"),
        ("WindowText", "0 0 0"),
    ];

    static (string Hive, string Prefix)[] Both() =>
    [
        ("HKCU", ""),
        ("HKU", @".DEFAULT\"),
    ];

    static HashSet<string> Known(BackupFile file) =>
        new(file.Entries.Select(e => e.Hive + "|" + e.Key + "|" + e.Name), StringComparer.OrdinalIgnoreCase);

    static void SaveBackup(BackupFile file)
    {
        var dir = Path.GetDirectoryName(BackupPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(BackupPath, JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true }));
    }

    static void SnapshotColors(BackupFile file, HashSet<string> known, string hive, string key)
    {
        var names = ActiveNames.Concat(InactiveNames).Append("TitleText").Append("InactiveTitleText");
        Snapshot(file, known, hive, key, names.ToArray());
    }

    static void SnapshotAll(BackupFile file, HashSet<string> known, string hive, string key)
    {
        using var opened = OpenRoot(hive).OpenSubKey(key, writable: false);
        if (opened is null) return;
        Snapshot(file, known, hive, key, opened.GetValueNames());
    }

    static void Snapshot(BackupFile file, HashSet<string> known, string hive, string key, params string[] names)
    {
        using var root = OpenRoot(hive);
        using var opened = root.OpenSubKey(key, writable: false);
        foreach (var name in names)
        {
            var id = hive + "|" + key + "|" + name;
            if (!known.Add(id)) continue;
            var entry = new BackupEntry { Hive = hive, Key = key, Name = name, Existed = false };
            if (opened is not null)
            {
                var value = opened.GetValue(name);
                if (value is not null || HasValue(opened, name))
                {
                    entry.Existed = true;
                    var kind = opened.GetValueKind(name);
                    entry.Kind = kind.ToString();
                    entry.Data = kind switch
                    {
                        RegistryValueKind.DWord => Convert.ToInt32(value).ToString(),
                        RegistryValueKind.QWord => Convert.ToInt64(value).ToString(),
                        RegistryValueKind.Binary => Convert.ToHexString((byte[])value!),
                        RegistryValueKind.MultiString => string.Join('\n', (string[])value!),
                        _ => value?.ToString() ?? "",
                    };
                }
            }
            file.Entries.Add(entry);
        }
    }

    static bool HasValue(RegistryKey key, string name)
    {
        try
        {
            key.GetValueKind(name);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    static BackupFile LoadBackup()
    {
        if (!File.Exists(BackupPath)) return new BackupFile();
        return JsonSerializer.Deserialize<BackupFile>(File.ReadAllText(BackupPath)) ?? new BackupFile();
    }

    static void WriteDword(string hive, string key, string name, uint value)
    {
        using var created = OpenRoot(hive).CreateSubKey(key, writable: true)
                            ?? throw new InvalidOperationException("could not open " + key);
        created.SetValue(name, unchecked((int)value), RegistryValueKind.DWord);
    }

    static void WriteBinary(string hive, string key, string name, byte[] value)
    {
        using var created = OpenRoot(hive).CreateSubKey(key, writable: true)
                            ?? throw new InvalidOperationException("could not open " + key);
        created.SetValue(name, value, RegistryValueKind.Binary);
    }

    static void WriteString(string hive, string key, string name, string value)
    {
        using var created = OpenRoot(hive).CreateSubKey(key, writable: true)
                            ?? throw new InvalidOperationException("could not open " + key);
        created.SetValue(name, value, RegistryValueKind.String);
    }

    static void DeleteKey(string hive, string key)
    {
        try
        {
            OpenRoot(hive).DeleteSubKeyTree(key, throwOnMissingSubKey: false);
        }
        catch (ArgumentException)
        {
        }
    }

    static void DeleteValue(string hive, string key, string name)
    {
        using var opened = OpenRoot(hive).OpenSubKey(key, writable: true);
        opened?.DeleteValue(name, throwOnMissingValue: false);
    }

    static RegistryKey OpenRoot(string hive) => hive switch
    {
        "HKCU" => Registry.CurrentUser,
        "HKU" => Registry.Users,
        "HKLM" => Registry.LocalMachine,
        _ => throw new ArgumentException(hive),
    };

    static void Broadcast()
    {
        try
        {
            SendMessageTimeout(new IntPtr(0xffff), 0x001A, UIntPtr.Zero, "ImmersiveColorSet", 2, 500, out _);
        }
        catch
        {
            // Sign-out still applies the colors if the broadcast cannot be sent.
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, UIntPtr wParam, string lParam, uint flags, uint timeout, out UIntPtr result);
}

public sealed class BackupFile
{
    public List<BackupEntry> Entries { get; set; } = [];
}

public sealed class BackupEntry
{
    public string Hive { get; set; } = "";
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Existed { get; set; }
    public string? Kind { get; set; }
    public string? Data { get; set; }
}
