# SmartHome Kiosk (Windows)

Eine performante, schlanke und robuste Kiosk-Anwendung für Windows auf Basis von **C# / .NET 8 WPF** und **Microsoft Edge WebView2**, konzipiert für Wand-Displays und SmartHome-Terminals (wie Neo Deck Dashboard).

---

## Features

- 🖥️ **Echtes Vollbild ohne Leisten:** Keine Fensterrahmen, keine Windows-Taskleiste, randlos maximiert.
- 🧙‍♂️ **Ersteinrichtungs-Assistent (Setup Wizard):** Beim Erststart öffnet sich automatisch ein Dialog zur Eingabe der Dashboard-URL, PIN und Autostart-Option.
- 🔒 **PIN-geschütztes Einstellungsmenü:**
  - **Maus-Shortcut 1:** **Scrollrad-Klick (mittlere Maustaste)** an beliebiger Stelle auf dem Bildschirm.
  - **Maus-Shortcut 2:** Schneller **3-fach-Klick** mit links in die **obere linke Bildschirmecke**.
  - **Touch-Geste:** Halte die **obere linke Bildschirmecke für 3 Sekunden** gedrückt.
  - **Tastatur-Kürzel:** `F2` oder `Strg + Umschalt + S`.
  - Im Menü können URL, PIN, Standby-Zeiten, Mauszeiger-Verhalten und Autostart angepasst werden. Ebenso gibt es Buttons für Cache-Leerung, Neuladen und ordentliches Beenden.
- 🌙 **Hybrid-Standby & Wake-on-Touch:**
  - Stufe 1: Nach $X$ Minuten Inaktivität schaltet sich ein pechschwarzer Vollbild-Schoner ein (sofortiges Wake-on-Touch ohne Signalverlust oder HDMI-Verzögerung).
  - Stufe 2: Optional nach $Y$ Minuten Hardware-Standby des Bildschirms via Win32 DPMS.
  - Verhindert unkontrolliertes Windows-Einschlafen (`SetThreadExecutionState`).
- 🔄 **Verbindungs-Watchdog:** Bei Boot-Verzögerungen oder Server-Neustarts zeigt die App einen Ladebildschirm und versucht alle 5 Sekunden automatisch die Wiederverbindung.
- 🖱️ **Mauszeiger-Verhalten:** Wählbar zwischen Auto-Hide (Mauszeiger verschwindet nach 3 Sekunden Inaktivität) und dauerhafter Sichtbarkeit (für Desktop-/Maus-Betrieb).
- 🚫 **Kiosk-Schutz:** Deaktiviert Kontextmenüs (Rechtsklick) und Pinch/Mausrad-Zoom.
- 🔊 **Auto-Permissions:** Genehmigt Audio-Autoplay, Kamera und Mikrofon ohne Bestätigungs-Popups.
- 🚀 **Autostart-Verwaltung:** Kann sich selbst in den Windows-Autostart (`HKCU\...\Run`) eintragen oder austragen.

---

## Projekt kompilieren & veröffentlichen

Führe das Skript `build.ps1` in PowerShell aus:

```powershell
.\build.ps1
```

Das Skript erzeugt eine einzige, eigenständige `.exe`-Datei:
```text
.\publish\SmartHomeKiosk.exe
```

---

## 📦 Installationsassistent erstellen (Setup.exe)

Führe das Skript `build-installer.ps1` aus, um die fertige Installationsdatei zu erzeugen:

```powershell
.\installer\build-installer.ps1
```

Der Installer wird im Ordner `dist\` abgelegt:
```text
.\dist\SmartHomeKiosk-Setup.exe
```

**Was der Installer bietet:**
- 🖼️ **Eigenes Setup-Icon:** Trägt das offizielle Dashboard-Logo.
- 👤 **Dual-Mode:** Kann ohne Admin-Rechte nur für den aktuellen Benutzer oder mit Admin-Rechten für alle Benutzer installiert werden.
- 📦 **Offline WebView2-Bundle:** Enthält den Evergreen Bootstrapper von Microsoft und installiert fehlende Laufzeitbibliotheken vollautomatisch.
- 🚀 **Desktop-Symbol & Autostart:** Kann direkt im Setup als Aufgabe ausgewählt werden.
- 🧹 **Saubere Deinstallation:** Eintrag in Windows *„Apps & Features“* mit optionaler Entfernung aller Benutzerdaten und Caches.

---

## Konfiguration (`appsettings.json`)

Die Einstellungen werden in `%APPDATA%\SmartHomeKiosk\appsettings.json` gespeichert:

```json
{
  "IsConfigured": true,
  "DashboardUrl": "http://192.168.178.100:3000",
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
