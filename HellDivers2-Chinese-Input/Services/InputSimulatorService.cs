using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static HellDivers2_Chinese_Input.NativeMethods;


namespace HellDivers2_Chinese_Input.Services
{
    internal class InputSimulatorService
    {

        public void SendTextToTargetWindow(string text,string windowClassName)
        {

            NativeMethods.SetForegroundWindow( NativeMethods.FindWindow(windowClassName,null));
           
            SendEnterKey();//发送回车
            SimulateUnicodeTyping(text);
            SendEnterKey();//发送回车


        }

        public void SendEnterKey()
        {
            CreateKeyInput(Keys.Enter,false);
            CreateKeyInput(Keys.Enter, true);
            //一定要休眠 否则游戏反应不过来
            Thread.Sleep(100);
            NativeMethods.SendInput(1, new INPUT[] { CreateKeyInput(Keys.Enter,false)}, Marshal.SizeOf(typeof(INPUT)));
            Thread.Sleep(50);//休眠
            NativeMethods.SendInput(1, new INPUT[] { CreateKeyInput(Keys.Enter, true)}, Marshal.SizeOf(typeof(INPUT)));
        }

        public void SimulateUnicodeTyping (string text)
        {
            var inputs = new List<INPUT>();
            int count = 0;//计数器          

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


        }


        private NativeMethods.INPUT CreateKeyInput(Keys key,bool keyup)
        {

            return new NativeMethods.INPUT
                {
              type = INPUT_KEYBOARD,
                mkhi = new MOUSEKEYBDHARDWAREINPUT
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)Keys.Enter,
                        wScan = 0,
                        dwFlags = keyup? NativeMethods.KEYEVENTF_KEYUP:0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

    }
}
