using System;
using System.Runtime.InteropServices;

namespace SmartHomeKiosk.Services
{
    public static class PowerService
    {
        [Flags]
        public enum ExecutionState : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_MOVE = 0x0001;

        /// <summary>
        /// Verhindert, dass Windows eigenständig in den Standby oder Ruhezustand wechselt.
        /// </summary>
        public static void PreventSystemSleep()
        {
            try
            {
                SetThreadExecutionState(ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED | ExecutionState.ES_DISPLAY_REQUIRED);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PowerService.PreventSystemSleep error: {ex.Message}");
            }
        }

        /// <summary>
        /// Hebt die Blockierung des Windows-Ruhezustands auf.
        /// </summary>
        public static void AllowSystemSleep()
        {
            try
            {
                SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PowerService.AllowSystemSleep error: {ex.Message}");
            }
        }

        /// <summary>
        /// Schaltet den physischen Monitor hardwareseitig in den Standby (2 = Power Off).
        /// </summary>
        public static void TurnOffDisplay()
        {
            try
            {
                SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, new IntPtr(SC_MONITORPOWER), new IntPtr(2));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PowerService.TurnOffDisplay error: {ex.Message}");
            }
        }

        /// <summary>
        /// Weckt den physischen Monitor auf (-1 = Power On & minimaler Cursor-Impuls).
        /// </summary>
        public static void TurnOnDisplay()
        {
            try
            {
                SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, new IntPtr(SC_MONITORPOWER), new IntPtr(-1));
                // Minimaler Mausimpuls simuliert Nutzeraktivität für Grafikkartentreiber
                mouse_event(MOUSEEVENTF_MOVE, 0, 1, 0, 0);
                mouse_event(MOUSEEVENTF_MOVE, 0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PowerService.TurnOnDisplay error: {ex.Message}");
            }
        }
    }
}
