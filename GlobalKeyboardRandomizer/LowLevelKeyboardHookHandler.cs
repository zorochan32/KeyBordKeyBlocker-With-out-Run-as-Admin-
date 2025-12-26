using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RandomKeyboard
{
    internal static class LowLevelKeyboardHookHandler
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _proc;
        private static IntPtr _hookID = IntPtr.Zero;
        private static bool _blocking = true;

        public static event EventHandler<KeyPressedEventArgs> KeyPressed;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        public static void InitializeKeyboardHook()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vk = Marshal.ReadInt32(lParam);
                if (KeyPressed != null) KeyPressed(null, new KeyPressedEventArgs(vk));
                if (_blocking) return (IntPtr)1;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public static void StopHook()
        {
            if (_hookID != IntPtr.Zero)
                UnhookWindowsHookEx(_hookID);
        }

        public static void EnableBlocking() { _blocking = true; }
        public static void DisableBlocking(bool notify) { _blocking = false; }
    }

    internal class KeyPressedEventArgs : EventArgs
    {
        public int VirtualKeyCode { get; private set; }
        public KeyPressedEventArgs(int vk) { VirtualKeyCode = vk; }
    }
}
