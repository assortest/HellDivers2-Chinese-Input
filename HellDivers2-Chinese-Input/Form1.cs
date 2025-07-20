using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;

namespace TypingChinese
{
    public partial class Form1 : Form
    {
        // 结构体定义
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public MOUSEKEYBDHARDWAREINPUT mkhi;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private NotifyIcon trayIcon; //定义一个托盘图标
        private ContextMenuStrip trayMenu; //用于托盘的菜单

        //定义热键的名称
        private const uint MOD_CTRL = 0x0002; // CTRL键
        //定义热键按下
        private const uint WM_HOTKEY = 0x0312;
        // 热键 ID，用于识别哪个热键被按下
        private const int HOTKET_ID = 9000;
        //定义热键id
        const int INPUT_KEYBOARD = 1;//输入类型为键盘
        const uint KEYEVENTF_KEYUP = 0x0002;//键位释放
        const uint KEYEVENTF_UNICODE = 0x0004;//Unicode字符
        [DllImport("user32.dll")] //注册热键
        private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint key, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        public Form1()
        {
            InitializeComponent();

            var screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Right - this.Width - 108, screen.Bottom - this.Height - 80);

            //注册load事件
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing; // 注册关闭事件
            this.textBox2.KeyDown += textBox1_KeyDown; //绑定按下事件

            trayMenu = new ContextMenuStrip();//初始化菜单
            trayMenu.Items.Add("显示窗口", null, (s,e)=> ShowWindow());//添加菜单项
            trayMenu.Items.Add("退出程序", null, (s, e) => {
                trayIcon.Visible = false;//隐藏托盘图标
                Application.Exit();
            });//添加退出菜单项

            trayIcon = new NotifyIcon//初始化托盘图标
            {
                Text = "绝地潜兵中文输入工具 v1.0 by:assor",
                Icon =this.Icon,
                ContextMenuStrip = trayMenu,
                Visible =true
            };


        }

        //添加隐藏逻辑
        protected override void OnResize(EventArgs e)
        {//最小化窗口时隐藏
            base.OnResize(e);
            if(this.WindowState== FormWindowState.Minimized)
            {
                this.Hide();//隐藏
            }
        }
        //重写WndProc 
        protected override void WndProc(ref Message m)
        {//如果传过来的消息是WM_HOTKEY并且wparam的之和hotkey一致
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKET_ID)
            {
                //触发显示或者隐藏自己
                if (this.Visible)
                {
                    this.Hide();
                }
                else
                {
                    this.Show();
                    this.textBox2.Text = "";
                    this.textBox2.Focus();
                    this.Activate();
                }
            }
            base.WndProc(ref m);
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal; //设置窗体为正常状态
            this.BringToFront();//将窗口置顶
            this.textBox2.Focus();
           
        }

        private void Form1_FormClosing(object sende, FormClosingEventArgs e)
        {
            trayIcon.Visible = false; // 关闭时移除托盘图标
            //卸载热键
            UnregisterHotKey(this.Handle, HOTKET_ID);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //注册热键
            RegisterHotKey(this.Handle, HOTKET_ID, MOD_CTRL, (uint)Keys.T);
        }


  
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;//表示该事件已被处理
                e.SuppressKeyPress = true;//抑制按键的进一步处理，防止按键事件传递到其他控件或处理程序
                string text = textBox2.Text.Trim();//吧文本框里的内容交给text顺便吧空格去掉
                if (string.IsNullOrEmpty(text))//检测字符串是否为空
                {
                    this.Hide();
                    return;
                }
                this.Hide();
                IntPtr hWnd = FindWindow("stingray_window", null); //寻找指定窗口
                SetForegroundWindow(hWnd);
                SimulateUnicodeTyping(text); //
            }

        }

        //模拟输入文本
        private void SimulateUnicodeTyping(string text)
        {
            // List<INPUT> inputs = new List<INPUT>();//实例化一个list 用于接收input这个结构的值
            int count = 0;//计数器          
            INPUT EnterDown = new INPUT
            {
                type = INPUT_KEYBOARD,
                mkhi = new MOUSEKEYBDHARDWAREINPUT
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)Keys.Enter,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            INPUT EnterUp = new INPUT
            {
                type = INPUT_KEYBOARD,
                mkhi = new MOUSEKEYBDHARDWAREINPUT
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)Keys.Enter,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };



            Thread.Sleep(100); //休眠 然后继续发送
            SendInput(1, new INPUT[] { EnterDown }, Marshal.SizeOf(typeof(INPUT)));
            Thread.Sleep(50);
            SendInput(1, new INPUT[] { EnterUp }, Marshal.SizeOf(typeof(INPUT)));
            foreach (char c in text)
            {//循环处理字符串 然后将结构封装进inputs这个list里

                if (count == 9)
                {

                    Thread.Sleep(200); //休眠500ms 然后继续发送
                    count = 0;//重新将计数器清零
                    INPUT down = new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        mkhi = new MOUSEKEYBDHARDWAREINPUT
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = 0,
                                wScan = c,
                                dwFlags = KEYEVENTF_UNICODE,
                                time = 0,
                                dwExtraInfo = IntPtr.Zero
                            }
                        }
                    };
                    INPUT up = new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        mkhi = new MOUSEKEYBDHARDWAREINPUT
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = 0,
                                wScan = c,
                                dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                                time = 0,
                                dwExtraInfo = IntPtr.Zero
                            }
                        }
                    };

                    //不使用list一起发送 改成循环挨个发送
                    SendInput(1, new INPUT[] { down }, Marshal.SizeOf(typeof(INPUT)));
                    SendInput(1, new INPUT[] { up }, Marshal.SizeOf(typeof(INPUT)));

                }
                else
                {
                    INPUT down = new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        mkhi = new MOUSEKEYBDHARDWAREINPUT
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = 0,
                                wScan = c,
                                dwFlags = KEYEVENTF_UNICODE,
                                time = 0,
                                dwExtraInfo = IntPtr.Zero
                            }
                        }
                    };
                    INPUT up = new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        mkhi = new MOUSEKEYBDHARDWAREINPUT
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = 0,
                                wScan = c,
                                dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                                time = 0,
                                dwExtraInfo = IntPtr.Zero
                            }
                        }
                    };

                    //不使用list一起发送 改成循环挨个发送
                    SendInput(1, new INPUT[] { down }, Marshal.SizeOf(typeof(INPUT)));
                    SendInput(1, new INPUT[] { up }, Marshal.SizeOf(typeof(INPUT)));
                    count++;
                }
            }
            Thread.Sleep(100); //休眠 然后继续发送
                 SendInput(1, new INPUT[] { EnterDown }, Marshal.SizeOf(typeof(INPUT)));
            Thread.Sleep(50);
            SendInput(1, new INPUT[] { EnterUp }, Marshal.SizeOf(typeof(INPUT)));

        }

      
    }
}
