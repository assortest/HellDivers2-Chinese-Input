using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace HellDivers2_Chinese_Input
{
    internal class NativeMethods
    {
        //定义热键的名称
        // private const uint MOD_CTRL = 0x0002; // CTRL键
        //定义热键按下
        public  const uint WM_HOTKEY = 0x0312;
 
        //定义热键id
        public const int INPUT_KEYBOARD = 1;//输入类型为键盘
        public const uint KEYEVENTF_KEYUP = 0x0002;//键位释放
        public const uint KEYEVENTF_UNICODE = 0x0004;//Unicode字符

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;


        #region struct
        // 结构体定义
        [StructLayout(LayoutKind.Sequential)]
       public struct INPUT
        {
            public uint type;
            public MOUSEKEYBDHARDWAREINPUT mkhi;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        #endregion

        #region WinAPI
        [DllImport("user32.dll")] //注册热键
        public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint key, uint vk);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        //[DllImport("user32.dll")]//附加线程输入
        //static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        //[DllImport("kernel32.dll")]
        //static extern uint GetCurrentThreadId();
        //[DllImport("user32.dll")]
        //static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        #endregion

    }
}
