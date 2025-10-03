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
using HellDivers2_Chinese_Input.Services;


namespace HellDivers2_Chinese_Input
{
    public partial class MainForm : Form
    {
        private readonly ConfigurationService _configurationService;
        private readonly HotkeyService _hotkeyService;
        private readonly InputSimulatorService _inputSimulatorService;

        //热键相关
        private const int HOTKEY_ID = 9000;

        //托盘相关
        private NotifyIcon trayIcon;
        //模拟相关
        private const string GAME_WINDOW_CLASS = "stingray_window";

        //拖拽相关 
        private bool isDragging = false;
        private Point lastForm;
        private Point lastCursor;

        //窗口设置相关
        private bool isSettingsWindowOpen = false;

        public MainForm()
        {
            _configurationService = new ConfigurationService();
            _hotkeyService = new HotkeyService(_configurationService);
            _inputSimulatorService = new InputSimulatorService();

            InitializeComponent();


            //注册load事件
            //this.Load += Form1_Load;
            this.FormClosing += MainForm_FormClosing; // 注册关闭事件
            this.inputTextBox.KeyDown += inputTextBox_KeyDown; //绑定按下事件
            InitializeTrayIcon();
            SetupDragging();
            LoadWindowPosition();
        }

        private void InitializeTrayIcon()
        {
            var trayMenu = new ContextMenuStrip();//初始化菜单
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
            if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
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
            this.inputTextBox.Text = "";
            this.inputTextBox.Focus();
            this.Activate();
        }

        private void MainForm_FormClosing(object sende, FormClosingEventArgs e)
        {
            trayIcon.Visible = false; // 关闭时移除托盘图标
            //卸载热键
            _hotkeyService.UnregisterHotkey(Handle, HOTKEY_ID);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //注册热键
            if (!_hotkeyService.RegisterHotkey(Handle, HOTKEY_ID))
            {
                MessageBox.Show("注册热键失败！可能是其他程序占用了热键。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }



        private void inputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Control && !e.Shift && !e.Alt)
            {
                e.Handled = true;//表示该事件已被处理
                e.SuppressKeyPress = true;//抑制按键的进一步处理，防止按键事件传递到其他控件或处理程序
                string text = inputTextBox.Text.Trim();//吧文本框里的内容交给text顺便吧空格去掉
                if (string.IsNullOrEmpty(text))//检测字符串是否为空
                {
                    this.Hide();
                    return;
                }
                this.Hide();
                _inputSimulatorService.SendTextToTargetWindow(text, GAME_WINDOW_CLASS); //寻找指定窗口           
            }

        }

        //模拟输入文本

        private void LoadWindowPosition()
        {

            //载入保存的位置
            if (HellDivers2_Chinese_Input.Properties.Settings.Default.LastLocation != Point.Empty)
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
            _hotkeyService.UnregisterHotkey(this.Handle, HOTKEY_ID);
            _hotkeyService.ResetAndSaveHotkey();
            if (_hotkeyService.RegisterHotkey(this.Handle, HOTKEY_ID))

                MessageBox.Show("热键已重置为CTRL + T", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
        }

        private void SetupDragging()
        {//处理拖动事件
            this.inputTextBox.MouseDown += InputTextBox_MouseDown;
            this.inputTextBox.MouseMove += InputTextBox_MouseMove;
            this.inputTextBox.MouseUp += InputTextBox_MouseUp;

        }

        private void InputTextBox_MouseDown(object sender, MouseEventArgs e)
        { //处理鼠标按下事件

            if (e.Button == MouseButtons.Left && inputTextBox.TextLength == 0)
            {//判断是否为左键
                isDragging = true;
                lastCursor = Cursor.Position;//获取当前鼠标的位置
                lastForm = this.Location; //获取窗体位置
            }

        }


        private void InputTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging == true)
            {
                var currentCursor = Cursor.Position;//获取当前鼠标位置
                //计算鼠标位置变化
                int deltaX = currentCursor.X - lastCursor.X;
                int deltaY = currentCursor.Y - lastCursor.Y;
                this.Location = new Point(lastForm.X + deltaX, lastForm.Y + deltaY);//更新窗体位置
            }
           
           
        }
        private void InputTextBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
            }
            //保存位置
            _configurationService.SaveWindowLocation(this.Location);
        }
        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {//处理托盘图标的点击事件
            if (e.Button == MouseButtons.Left && e.Clicks == 2)
            {
                if (this.Visible)
                    this.Hide();
                else
                    ShowWindow();
            }
        }
        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            if (isSettingsWindowOpen == true) return;

            _hotkeyService.UnregisterHotkey(this.Handle, HOTKEY_ID);
            isSettingsWindowOpen = true;
            var settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
            _hotkeyService.RegisterHotkey(this.Handle, HOTKEY_ID);


            if (settingsForm != null)
                settingsForm.Dispose();
            isSettingsWindowOpen = false;

        }


    }
}