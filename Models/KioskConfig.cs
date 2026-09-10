namespace SmartHomeKiosk.Models
{
    public class KioskConfig
    {
        public bool IsConfigured { get; set; } = false;
        public string DashboardUrl { get; set; } = "";
        public string PinCode { get; set; } = "1234";
        public bool AutoStartEnabled { get; set; } = false;
        
        // Standby-Verhalten
        public bool IsHybridStandbyEnabled { get; set; } = true;
        public int DimTimeoutMinutes { get; set; } = 3;
        public int HardwareStandbyTimeoutMinutes { get; set; } = 15;

        // UI & Kiosk-Sicherheit
        public bool AutoHideCursor { get; set; } = true;
        public int HideCursorDelaySeconds { get; set; } = 3;
        public bool DisableTouchZoom { get; set; } = true;
        public bool DisableContextMenu { get; set; } = true;
        public bool AutoApprovePermissions { get; set; } = true;
    }
}
