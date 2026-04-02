using System;

namespace IkariamWatcher
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Initialize WinForms compatibility for the tray application
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            // Run the tray application context (tray-only, no windows)
            System.Windows.Forms.Application.Run(new TrayApplicationContext());
        }
    }
}
