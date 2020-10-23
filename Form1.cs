using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace WakeyWakeyUI
{
    public partial class frmMain : Form
    {
        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            //public UInt32 dwTime;
            public int dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        [DllImport("user32.dll")]
        public static extern void keybd_event(
        byte bVk,
        byte bScan,
        uint dwFlags,
        uint dwExtraInfo
        );

        const int VK_CONTROL = 0x11;
        const uint KEYEVENTF_KEYUP = 0x2;

        private int _lastTime;
        private DateTime _dtCalc;
        private DateTime _dtCalc2;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            int tmrTime = int.Parse(ConfigurationManager.AppSettings["TimerTime"]);
            tmrUpdateAway.Interval = tmrTime;
            _dtCalc = DateTime.Now;
            _dtCalc2 = DateTime.Now;
            KeepAlive();
        }

        private void KeepAlive()
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_AWAYMODE_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);
            keybd_event((byte)VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            int lastInputTime = GetLastInputTime();
            Debug.WriteLine(String.Format("Keep Alive Triggered, lastInputTime {0} / _lastTime {1}", lastInputTime, _lastTime));
            if (lastInputTime < _lastTime)
            {
                _dtCalc = DateTime.Now;
            }
            _lastTime = lastInputTime;
        }

        private int GetLastInputTime()
        {
            int idleTime = 0;
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            int envTicks = Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                int lastInputTick = lastInputInfo.dwTime;
                idleTime = envTicks - lastInputTick;
            }
            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }

        private void tmrCheckStatus_Tick(object sender, EventArgs e)
        {
            lblTimer.Text = (DateTime.Now - _dtCalc).ToString(@"hh\:mm\:ss");
            lblTimer2.Text = (DateTime.Now - _dtCalc2).ToString(@"hh\:mm\:ss");

            int lastInputTime = GetLastInputTime();
            if (lastInputTime < _lastTime)
            {
                _dtCalc = DateTime.Now;
            }
            _lastTime = lastInputTime;
        }

        private void tmrUpdateAway_Tick(object sender, EventArgs e)
        {
            KeepAlive();
        }

        private void lblTimer2_Click(object sender, EventArgs e)
        {
            _dtCalc2 = DateTime.Now;
        }

        
    }
}
