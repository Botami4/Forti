using System;
using System.Runtime.InteropServices;
using System.Windows;
using Forti.Core.Tests;

namespace Forti
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        private const int ATTACH_PARENT_PROCESS = -1;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool isSelfTest = false;
            bool isBenchmark = false;
            foreach (var arg in e.Args)
            {
                if (string.Equals(arg, "--selftest", StringComparison.OrdinalIgnoreCase))
                {
                    isSelfTest = true;
                    break;
                }
                else if (string.Equals(arg, "--benchmark", StringComparison.OrdinalIgnoreCase))
                {
                    isBenchmark = true;
                }
            }

            if (isSelfTest)
            {
                // Attach to calling terminal console if available
                AttachConsole(ATTACH_PARENT_PROCESS);

                bool success = SelfTestRunner.RunAll();
                int exitCode = success ? 0 : 1;
                Environment.ExitCode = exitCode;
                Shutdown(exitCode);
                return;
            }

            if (isBenchmark)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                var benchWindow = new MainWindow { IsBenchmarkMode = true };
                benchWindow.Show();
                return;
            }

            // Normal GUI launch
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
