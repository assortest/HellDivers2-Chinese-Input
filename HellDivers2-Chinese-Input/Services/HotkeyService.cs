using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HellDivers2_Chinese_Input.Services
{
    internal class HotkeyService
    {
        private readonly ConfigurationService _configService;

        public HotkeyService(ConfigurationService configService)
        {
            this._configService = configService;
        }
        //注册热键 
        public bool RegisterHotkey(IntPtr hwnd, int hotkeyId)
        {
            var (modifiers,key) = _configService.GetHotKey();
            NativeMethods.RegisterHotKey(hwnd, hotkeyId, modifiers, key);
            return true;
        }

        public void UnregisterHotkey(IntPtr hwnd, int hotkeyId)
        { //卸载热键
            NativeMethods.UnregisterHotKey(hwnd, hotkeyId);
          
        }

        //重置热键
        public void ResetAndSaveHotkey()
        {

            _configService.SaveHotKey(NativeMethods.MOD_CONTROL,(uint)Keys.T);

        }
     



    }
}
