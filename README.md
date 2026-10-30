# Color Without Limits

Windows tool that writes a custom accent color. Settings darkens a custom color. This keeps the hex you type on the taskbar, title bars, trim, and quick-settings buttons.

Windows only. After Apply, Reset, or Restore, sign out and back in.

The download is the zip on the Releases page. Windows may say the app is unrecognized because it is not signed.

## Build

Requires the .NET 8 SDK.

```
dotnet publish -c Release -r win-x64 --self-contained true
```
