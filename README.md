# SmartHome Kiosk (Windows)

[![Release Build](https://github.com/Daddelgreis74/smarthome-kiosk-windows/actions/workflows/release.yml/badge.svg)](https://github.com/Daddelgreis74/smarthome-kiosk-windows/actions/workflows/release.yml)
[![GitHub Release](https://img.shields.io/github/v/release/Daddelgreis74/smarthome-kiosk-windows)](https://github.com/Daddelgreis74/smarthome-kiosk-windows/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

A lightweight, performant, and robust fullscreen kiosk application for Windows built with **C# / .NET 8 WPF** and **Microsoft Edge WebView2**. Specially designed for wall-mounted touchscreens, smart tablets, and home automation terminals (such as the Neo Deck Dashboard).

---

## ✨ Features

- 🖥️ **True Borderless Fullscreen:** No window borders, no Windows taskbar, perfectly maximized for embedded displays.
- 🧙‍♂️ **First-Run Setup Wizard:** On first launch, a friendly wizard automatically guides you through entering your dashboard URL, setting a security PIN, and configuring autostart.
- 🔒 **PIN-Protected Settings Menu:**
  - **Mouse Shortcut 1:** **Middle mouse click (scroll wheel)** anywhere on the screen.
  - **Mouse Shortcut 2:** Quick **triple left-click** in the **top-left corner**.
  - **Touch Gesture:** Press and hold the **top-left corner for 3 seconds**.
  - **Keyboard Shortcut:** `F2` or `Ctrl + Shift + S`.
  - The settings dialog allows changing the dashboard URL, PIN, standby timers, cursor visibility mode, and autostart. Also includes quick actions to reload, clear web cache, or cleanly exit.
- 🌙 **Hybrid Standby & Instant Wake-on-Touch:**
  - **Stage 1 (Blackout Saver):** After $X$ minutes of user inactivity, a pitch-black fullscreen overlay appears. The screen immediately wakes on the slightest touch or movement without any HDMI reconnect lag or video sync delays.
  - **Stage 2 (Hardware Standby):** Optionally powers down the monitor via Win32 DPMS after $Y$ minutes.
  - Keeps Windows awake and prevents uncontrolled OS sleep (`SetThreadExecutionState`).
- 🔄 **Connection Watchdog & Auto-Reconnect:** If the dashboard server reboots or network drops, a neat loading screen is displayed with automatic retry every 5 seconds.
- 🖱️ **Configurable Mouse Cursor Behavior:** Switch between auto-hiding the cursor after 3 seconds of inactivity (ideal for wall touchscreens) or keeping it permanently visible (for mouse/desktop use).
- 🔊 **Auto-Granted Permissions:** Silently grants permissions for audio autoplay, webcam, and microphone without intrusive prompt popups.
- 🔄 **In-App Auto-Updates (GitHub Releases):** Automatically checks for updates on startup and every 24 hours, displays a discreet update badge on the dashboard when a new release is available, and provides seamless 1-click silent update installation directly in the settings menu with changelog display.
- 🚀 **Windows Autostart Integration:** Easily enable or disable autostart with Windows via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

---

## 📥 Installation

### Option 1: Installer (Recommended)
Download **`SmartHomeKiosk-Setup.exe`** from the [Latest Releases](https://github.com/Daddelgreis74/smarthome-kiosk-windows/releases/latest).

The setup wizard features:
- Dual installation mode (install for current user or all users / administrative).
- Automatic detection and installation of Microsoft Edge WebView2 Evergreen Runtime if missing.
- Option to create a desktop shortcut and enable launch on Windows startup.
- Clean uninstallation via Windows *Apps & Features*.

### Option 2: Portable Executable
Download **`SmartHomeKiosk.exe`** from the [Latest Releases](https://github.com/Daddelgreis74/smarthome-kiosk-windows/releases/latest) and run it anywhere without installation.

---

## 🛡️ Windows 11 Security & Smart App Control (Troubleshooting)

Because this is an open-source project without an expensive corporate EV code-signing certificate, Windows security features might flag or block the installer on some devices:

### 1. "Error 1551: An application control policy has blocked this file" (Smart App Control)
On Windows 11 tablets and newer PCs, Microsoft's **Smart App Control (SAC)** is enabled by default. It automatically blocks all unsigned open-source binaries without an override button:
- **How to resolve:**
  1. Open the Windows Start menu and search for **Windows Security** (*Windows-Sicherheit*).
  2. Navigate to **App & browser control** (*App- und Browsersteuerung*).
  3. Click on **Smart App Control settings** (*Einstellungen für die intelligente App-Steuerung*).
  4. Set the toggle to **Off** (*Aus*).
  *(Note: Windows Defender real-time antivirus protection remains fully active).*

### 2. Windows SmartScreen ("Windows protected your PC")
- Click on **More info** (*Weitere Informationen*).
- Click on **Run anyway** (*Trotzdem ausführen*).

### 3. Unblocking Downloaded Files (Mark of the Web)
If Windows marks downloaded files from the browser as restricted:
- Right-click (or long press on touchscreen) `SmartHomeKiosk-Setup.exe` > **Properties** (*Eigenschaften*).
- At the bottom of the *General* tab, check the **Unblock** (*Zulassen*) box.
- Click **Apply** and **OK**.

---

## 🛠️ Building from Source

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (optional, only needed for creating the installer)
- PowerShell 5.1 or PowerShell 7+

### 1. Build Portable Single-File Executable
```powershell
.\build.ps1
```
The output will be generated in `.\publish\SmartHomeKiosk.exe`.

### 2. Build Inno Setup Installer
```powershell
.\installer\build-installer.ps1
```
The installer will be generated in `.\dist\SmartHomeKiosk-Setup.exe`.

---

## ⚙️ Configuration (`appsettings.json`)

Settings are stored in `%APPDATA%\SmartHomeKiosk\appsettings.json`:

```json
{
  "IsConfigured": true,
  "DashboardUrl": "https://192.168.178.100:8443",
  "PinCode": "1234",
  "AutoStartEnabled": true,
  "IsHybridStandbyEnabled": true,
  "DimTimeoutMinutes": 3,
  "HardwareStandbyTimeoutMinutes": 15,
  "HideCursorDelaySeconds": 3,
  "DisableTouchZoom": true,
  "DisableContextMenu": true,
  "AutoApprovePermissions": true
}
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
