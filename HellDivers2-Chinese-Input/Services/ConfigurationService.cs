using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace HellDivers2_Chinese_Input.Services
{
    internal class ConfigurationService
    {
        public Point GetLastWindowLocation()
        {
            return HellDivers2_Chinese_Input.Properties.Settings.Default.LastLocation;
        }

        public void SaveWindowLocation(Point location)
        {
            //保存窗口位置
            HellDivers2_Chinese_Input.Properties.Settings.Default.LastLocation = location;
            HellDivers2_Chinese_Input.Properties.Settings.Default.Save();

        }

        public (uint Modifers, uint Key) GetHotKey() 
        {
          var Key = HellDivers2_Chinese_Input.Properties.Settings.Default.Hotkey;
          var  Modifers = HellDivers2_Chinese_Input.Properties.Settings.Default.HotkeyModifiers;
            if(Key==0)
            {
                return (NativeMethods.MOD_CONTROL, (uint)Keys.T);
            }
            return (Modifers, Key);
        }

        public void SaveHotKey(uint modifiers, uint Key)
        {//保存热键
            HellDivers2_Chinese_Input.Properties.Settings.Default.Hotkey = Key;
            HellDivers2_Chinese_Input.Properties.Settings.Default.HotkeyModifiers = modifiers;
            HellDivers2_Chinese_Input.Properties.Settings.Default.Save();
        }

    }
}
