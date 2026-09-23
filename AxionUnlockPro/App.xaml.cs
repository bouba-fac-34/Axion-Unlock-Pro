using System;
using System.Windows;

namespace AxionUnlockPro
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Ensure bin folder exists for adb/fastboot
            var bin = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
            if (!System.IO.Directory.Exists(bin))
                System.IO.Directory.CreateDirectory(bin);
        }
    }
}
