using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RandomKeyboard
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RandomKeyboardContext());
        }
    }

    class RandomKeyboardContext : ApplicationContext
    {
        private NotifyIcon _tray;
        private bool _enabled = false;
        private Random _rnd = new Random();

        public RandomKeyboardContext()
        {
            _tray = new NotifyIcon();
            _tray.Icon = SystemIcons.Application;
            _tray.Visible = true;
            _tray.Text = "Random Keyboard (F8 to toggle)";

            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem toggleItem = new ToolStripMenuItem("Toggle (F8)");
            toggleItem.Click += delegate { Toggle(); };
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += delegate { ExitThread(); };

            menu.Items.Add(toggleItem);
            menu.Items.Add(exitItem);
            _tray.ContextMenuStrip = menu;

            UpdateTrayText();

            LowLevelKeyboardHookHandler.KeyPressed += OnKeyPressed;
            LowLevelKeyboardHookHandler.InitializeKeyboardHook();
            Application.ApplicationExit += delegate { Cleanup(); };
        }

        private void Toggle()
        {
            _enabled = !_enabled;
            UpdateTrayText();
        }

        private void UpdateTrayText()
        {
            _tray.Text = "Random Keyboard - " + (_enabled ? "Enabled" : "Disabled") + " (F8)";
        }

        private void Cleanup()
        {
            LowLevelKeyboardHookHandler.StopHook();
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
            }
        }

        protected override void ExitThreadCore()
        {
            Cleanup();
            base.ExitThreadCore();
        }

        private void OnKeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (e.VirtualKeyCode == (int)Keys.F8)
            {
                Toggle();
                return;
            }

            if (_enabled)
            {
                SendRandomChar();
                LowLevelKeyboardHookHandler.DisableBlocking(false);
                LowLevelKeyboardHookHandler.EnableBlocking();
            }
        }

        private void SendRandomChar()
        {
            char c = (char)('a' + _rnd.Next(0, 26));
            INPUT[] inputs = new INPUT[2];

            inputs[0].type = 1; // INPUT_KEYBOARD
            inputs[0].ki = new KEYBDINPUT { wVk = 0, wScan = c, dwFlags = KEYEVENTF.UNICODE, time = 0, dwExtraInfo = IntPtr.Zero };
            inputs[1].type = 1;
            inputs[1].ki = new KEYBDINPUT { wVk = 0, wScan = c, dwFlags = KEYEVENTF.UNICODE | KEYEVENTF.KEYUP, time = 0, dwExtraInfo = IntPtr.Zero };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT { public uint type; public KEYBDINPUT ki; }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT { public ushort wVk; public ushort wScan; public KEYEVENTF dwFlags; public uint time; public IntPtr dwExtraInfo; }

        [Flags]
        enum KEYEVENTF : uint { EXTENDEDKEY = 0x0001, KEYUP = 0x0002, UNICODE = 0x0004, SCANCODE = 0x0008 }

        [DllImport("user32.dll", SetLastError = true)] static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    }
}
