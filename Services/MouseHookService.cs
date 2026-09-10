using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SmartHomeKiosk.Services
{
    public class MouseHookService : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MBUTTONDOWN = 0x0207;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private readonly Window _window;
        private IntPtr _windowHandle = IntPtr.Zero;
        private LowLevelMouseProc? _proc;
        private IntPtr _hookId = IntPtr.Zero;

        public event Action? ActivityDetected;
        public event Action? MiddleClickDetected;
        public event Action<int, int>? LeftMouseDownDetected;
        public event Action? LeftMouseUpDetected;

        public MouseHookService(Window window)
        {
            _window = window;
            _window.Loaded += (s, e) =>
            {
                _windowHandle = new WindowInteropHelper(_window).Handle;
                StartHook();
            };
            _window.Closed += (s, e) => Dispose();
        }

        private void StartHook()
        {
            if (_hookId != IntPtr.Zero) return;

            _proc = HookCallback;
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            if (curModule?.ModuleName != null)
            {
                _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                IntPtr activeHwnd = GetForegroundWindow();
                // Nur verarbeiten, wenn unser Kiosk-Fenster das aktive Fenster ist
                if (activeHwnd == _windowHandle)
                {
                    int msg = wParam.ToInt32();
                    var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                    _window.Dispatcher.BeginInvoke(() =>
                    {
                        if (msg == WM_MOUSEMOVE)
                        {
                            ActivityDetected?.Invoke();
                        }
                        else if (msg == WM_MBUTTONDOWN)
                        {
                            ActivityDetected?.Invoke();
                            MiddleClickDetected?.Invoke();
                        }
                        else if (msg == WM_LBUTTONDOWN)
                        {
                            ActivityDetected?.Invoke();
                            LeftMouseDownDetected?.Invoke(hookStruct.pt.x, hookStruct.pt.y);
                        }
                        else if (msg == WM_LBUTTONUP)
                        {
                            LeftMouseUpDetected?.Invoke();
                        }
                    });
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }
    }
}
