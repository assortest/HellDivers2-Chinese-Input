using HellDivers2_Chinese_Input.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace HellDivers2_Chinese_Input
{
    public partial class SettingsForm : Form
    {
        private readonly ConfigurationService _configService;
        private readonly HotkeyService _hotkeyService;

        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);
        private uint newModifiers;
        private Keys newKey;

        public SettingsForm()
        {
            _configService = new ConfigurationService();
            _hotkeyService = new HotkeyService(_configService);
            InitializeComponent();
            this.Load += SettingsForm_Load;
            this.txtHotkey.ReadOnly = true;
            this.TopMost = true;
            this.txtHotkey.GotFocus += (sender, e) => HideCaret(txtHotkey.Handle);
            this.txtHotkey.KeyDown += txtHotkey_KeyDown;
            this.btnSave.Click += btnSave_Click;
            this.btnCancel.Click += btnCancel_Click;
        }

        //处理窗体加载
        private void SettingsForm_Load(object sender, EventArgs e)
        {
            //设置图标
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            //读取以及保存的配置
            var (currentModifiers, currentKey) = _configService.GetHotKey();

            if (currentKey == 0)
            {
                //如果配置为0，则设置默认值
                newModifiers = 0x0002;
                newKey = Keys.T;

            }
            else
            {
                newModifiers = currentModifiers;
                newKey = (Keys)currentKey;
            }

            txtHotkey.Text = KeyToString(newModifiers, newKey);
        }

        //处理文本框按下事件
        private void txtHotkey_KeyDown(object sender, KeyEventArgs e)
        {

            e.SuppressKeyPress = true;

            //忽略单独按键
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu)
            {
                return;
            }
            //获取组合按键
            newModifiers = 0;
            if (e.Control) newModifiers |= 0x0002;
            if (e.Shift) newModifiers |= 0x0004;
            if (e.Alt) newModifiers |= 0x0001;

            newKey = e.KeyCode;
            //在文本框中显示
            txtHotkey.Text = KeyToString(newModifiers, newKey);
        }

        //处理保存

        private void btnSave_Click(object sender, EventArgs e)
        {
            //保存配置
            _configService.SaveHotKey(newModifiers, (uint)newKey);

            this.DialogResult = DialogResult.OK;
            this.Close();

        }
        //处理取消
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }


        private string KeyToString(uint modifiers, Keys key)
        {
            //用于将按键转换为可读字符串
            var sb = new StringBuilder();
            if ((modifiers & 0x0002) != 0) sb.Append("Ctrl + ");
            if ((modifiers & 0x0001) != 0) sb.Append("Alt + ");
            if ((modifiers & 0x0004) != 0) sb.Append("Shift + ");
            sb.Append(key.ToString());
            return sb.ToString();
        }


    }
}