using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Mayhem.Launcher
{
    internal static class UnsafeNative
    {
        private const int WM_COPYDATA = 0x004A;

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll", EntryPoint = "SendMessage", SetLastError = true)]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        public static string GetMessage(int message, IntPtr lParam)
        {
            if (message == UnsafeNative.WM_COPYDATA)
            {
                try
                {
                    var data = Marshal.PtrToStructure<UnsafeNative.COPYDATASTRUCT>(lParam);
                    var result = string.Copy(data.lpData);
                    return result;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public static void SendMessage(IntPtr hwnd, string message)
        {
            var messageBytes = Encoding.Unicode.GetBytes(message);
            var data = new UnsafeNative.COPYDATASTRUCT
            {
                dwData = IntPtr.Zero,
                lpData = message,
                cbData = messageBytes.Length + 1 /* +1 because of \0 string termination */
            };

            if (UnsafeNative.SendMessage(hwnd, WM_COPYDATA, IntPtr.Zero, ref data) != 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpData;
        }
    }
}