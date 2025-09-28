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
        //托盘相关
        private NotifyIcon trayIcon; //定义一个托盘图标
        private ContextMenuStrip trayMenu; //用于托盘的菜单

        //保存按键相关
        private SettingsForm settingsForm;
        private bool isSettingsWindowOpen = false;

        //存储当前热键
        private uint currentModifiers;
        private uint currentKey;

        //窗口拖动相关
        private bool isDragging = false;
        private Point lastCursor;
        private Point lastForm;


        //定义热键的名称
       // private const uint MOD_CTRL = 0x0002; // CTRL键
        //定义热键按下
        private const uint WM_HOTKEY = 0x0312;
        // 热键 ID，用于识别哪个热键被按下
        private const int HOTKET_ID = 9000;
        //定义热键id
        const int INPUT_KEYBOARD = 1;//输入类型为键盘
        const uint KEYEVENTF_KEYUP = 0x0002;//键位释放
        const uint KEYEVENTF_UNICODE = 0x0004;//Unicode字符
        //Windows API相关
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
        //[DllImport("user32.dll")]//附加线程输入
        //static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        //[DllImport("kernel32.dll")]
        //static extern uint GetCurrentThreadId();
        //[DllImport("user32.dll")]
        //static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public Form1()
        {
            InitializeComponent();
            SetupDragging();
            LoadWindowPosition();
           

            //注册load事件
            //this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing; // 注册关闭事件
            this.textBox2.KeyDown += textBox1_KeyDown; //绑定按下事件
            //用于解决补帧无法输入中文的问题 待定
            //IntPtr hwnd = FindWindow(null, "Lossless Scaling");
            //GetWindowThreadProcessId(hwnd, out uint pid);
            //uint Currentid = GetCurrentThreadId();
            //AttachThreadInput(Currentid, pid, true);


            trayMenu = new ContextMenuStrip();//初始化菜单
            trayMenu.Items.Add("显示窗口", null, (s, e) => ShowWindow());//添加菜单项
            trayMenu.Items.Add("重置窗口", null, (s, e) => PositionWindow());//添加菜单项
            trayMenu.Items.Add("设置按键", null, (s, e) => SettingsMenuItem_Click(s, e));//添加菜单项
            trayMenu.Items.Add("重置按键", null, (s, e) => ResetHotKeys());//添加菜单项
            trayMenu.Items.Add("退出程序", null, (s, e) =>
            {
                trayIcon.Visible = false;//隐藏托盘图标
                Application.Exit();
            });//添加退出菜单项
            trayIcon = new NotifyIcon//初始化托盘图标
            {
                Text = "绝地潜兵中文输入工具 v1.6 by:assor",
                Icon = this.Icon,
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            trayIcon.MouseDoubleClick += TrayIcon_MouseClick;//绑定鼠标点击事件


        }

        //添加隐藏逻辑
        protected override void OnResize(EventArgs e)
        {//最小化窗口时隐藏
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
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
                    ShowWindow();
                }
            }
            base.WndProc(ref m);
        }

        private void ShowWindow()
        {

            this.Show();
            this.textBox2.Text = "";
            this.textBox2.Focus();
            this.Activate();

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
            LoadAndRegisterHotKey();
        }



        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Control && !e.Shift && !e.Alt)
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
                SetForegroundWindow(FindWindow("stingray_window", null)) ; //寻找指定窗口           
                SimulateUnicodeTyping(text); 
            }

        }

        //模拟输入文本
        private void SimulateUnicodeTyping(string text)
        {
            var inputs = new List<INPUT>();//

            int count = 0;//计数器          
            var EnterDown = new INPUT
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
            var EnterUp = new INPUT
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
                var down = new INPUT
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
                var up = new INPUT
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
                inputs.Add(down); //按下
                inputs.Add(up);//抬起
                count++;
                if (count == 10)
                {//游戏有缓冲区 10个按键就会堵塞 所以每10个按键休眠
                    Thread.Sleep(100);
                    count = 0;//重新将计数器清零
                    SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
                    inputs.Clear();//发送完成清空list
                }
  
            }
            if (inputs.Count > 0)
            {//处理剩余的按键
                Thread.Sleep(100);//防止游戏反应不过来
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
                count = 0;
                inputs.Clear();//发送完成清空list
            }

            Thread.Sleep(100); //休眠 然后继续发送
            SendInput(1, new INPUT[] { EnterDown }, Marshal.SizeOf(typeof(INPUT)));
            Thread.Sleep(100);
            SendInput(1, new INPUT[] { EnterUp }, Marshal.SizeOf(typeof(INPUT)));

        }
        private void LoadWindowPosition()
        {
         
             //载入保存的位置
                if(HellDivers2_Chinese_Input.Properties.Settings.Default.LastLocation != Point.Empty)
                {
                    this.Location = HellDivers2_Chinese_Input.Properties.Settings.Default.LastLocation;
                }
                else
                {
                    PositionWindow();
                }
       
        }

        private void PositionWindow()
        {
            var screen = Screen.PrimaryScreen.WorkingArea;

            int rightMargin = (int)(screen.Width * 0.055); // 右边距为屏幕宽度的5%
            int bottomMargin = (int)(screen.Height * 0.058); // 下边距为屏幕高度的8%

            this.Location = new Point(
                screen.Right - this.Width - rightMargin,
                screen.Bottom - this.Height - bottomMargin
            );
        }


        private void ResetHotKeys()
        {
            currentKey = (uint)Keys.T;
            currentModifiers = 0x0002;
            RegisterHotKey(this.Handle, HOTKET_ID, currentModifiers, currentKey);
            MessageBox.Show("热键已重置为CTRL + T");

        }

        private void SetupDragging()
        {//处理拖动事件
            this.textBox2.MouseDown += Form_MouseDown;
            this.textBox2.MouseMove += Form_MouseMove;
            this.textBox2.MouseUp += Form_MouseUp;


        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        { //处理鼠标按下事件

            if (e.Button == MouseButtons.Left && textBox2.TextLength == 0)
            {//判断是否为左键
                isDragging = true;
                lastCursor = Cursor.Position;//获取当前鼠标的位置
                lastForm = this.Location; //获取窗体位置
            }

        }


        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging == true)
            {
                var currentCursor = Cursor.Position;//获取当前鼠标位置
                //计算鼠标位置变化
                int deltaX = currentCursor.X - lastCursor.X;
                int deltaY = currentCursor.Y - lastCursor.Y;
                this.Location = new Point(lastForm.X + deltaX, lastForm.Y + deltaY);//更新窗体位置
            }
            //保存位置
            HellDivers2_Chinese_Input.Properties.Settings.Default.LastLocation = this.Location;
            HellDivers2_Chinese_Input.Properties.Settings.Default.Save();
        }
        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
            }
            
        }
        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {//处理托盘图标的点击事件
            if (e.Button == MouseButtons.Left && e.Clicks==2)
            {
                if(this.Visible)              
                    this.Hide();      
                else
                    ShowWindow();
            }
        }
        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            if (isSettingsWindowOpen == true) return;
            try
            {
                isSettingsWindowOpen = true;
                //建立一个非模态的窗体
                UnregisterHotKey(this.Handle, HOTKET_ID);
                settingsForm = new SettingsForm();

                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    LoadAndRegisterHotKey();
                }
                else
                {
                    RegisterHotKey(this.Handle, HOTKET_ID, currentModifiers, currentKey);
                }
            }
            finally
            {
                if(settingsForm != null)
                    settingsForm.Dispose();
                isSettingsWindowOpen = false;
            }
        }

        private void LoadAndRegisterHotKey()
        {
            //得到新的热键
            currentKey = HellDivers2_Chinese_Input.Properties.Settings.Default.Hotkey;
            currentModifiers = HellDivers2_Chinese_Input.Properties.Settings.Default.HotkeyModifiers;

            if(currentKey == 0)
            {
                currentKey = (uint)Keys.T;
                currentModifiers=0x0002;
            }

            if (!RegisterHotKey(this.Handle, HOTKET_ID, currentModifiers, currentKey))
            {
                MessageBox.Show("注册热键失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }       
        }


    }
}
