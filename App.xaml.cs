using System;
using System.Diagnostics;
using System.Windows;

namespace keyboard_unchatter_csharp
{
    public partial class App : Application
    {
        public static InputHook InputHook { get; private set; }
        public static KeyboardMonitor KeyboardMonitor { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
            InputHook = new InputHook();
            KeyboardMonitor = new KeyboardMonitor();
            KeyboardMonitor.ChatterTimeMs = Convert.ToDouble(keyboard_unchatter_csharp.Properties.Settings.Default.chatterThreshold);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (InputHook != null)
            {
                InputHook.Dispose();
            }
            base.OnExit(e);
        }
    }
}
