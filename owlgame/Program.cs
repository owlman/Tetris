using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace owlgame
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Picks the UI language before any window exists; the labels are
            // fixed at construction time, so this is the only moment it can run.
            Strings.Configure(args);

            // Without this the window is rendered at 96 DPI and then bitmap
            // stretched by the system on a 125% / 150% display, which blurs both
            // the text and the cell edges. Must run before any window exists.
            try { SetProcessDPIAware(); }
            catch (Exception) { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}