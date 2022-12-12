using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Mayhem.Launcher
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Process runningProcess = GetRunningProcess();

            if (runningProcess != null && args.Length > 0)
            {
                UnsafeNative.SendMessage(runningProcess.MainWindowHandle, string.Join(" ", args));
            }
            else
            {
                var app = new App(args);
            }
        }

        private static Process GetRunningProcess()
        {
            Process process = Process.GetCurrentProcess();
            string processName = process.ProcessName.Replace(".vshost", "");

            return Process.GetProcesses()
                .FirstOrDefault(x => (x.ProcessName == processName ||
                                x.ProcessName == process.ProcessName ||
                                x.ProcessName == process.ProcessName + ".vshost") && x.Id != process.Id);
        }
    }
}
