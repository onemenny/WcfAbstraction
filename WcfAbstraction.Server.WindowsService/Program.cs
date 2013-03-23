using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Xml;

namespace WcfAbstraction.Server.WindowsService
{
    /// <summary>
    /// Main program entry point
    /// </summary>
    public static class Program
    {
        #region Interops
        [DllImport("Kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetEnvironmentVariable(string name, string value);
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main()
        {
            bool isDebugging = false || System.Diagnostics.Debugger.IsAttached;
            var service = new WindowsService();
            //bool shouldInstallService = false;

            //if (!isDebugging)
            //{
            //    if (shouldInstallService)
            //    {
            //        string frameworkPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            //        AppDomain.CurrentDomain.ExecuteAssembly(System.IO.Path.Combine(frameworkPath, "InstallUtil.exe"), null, new string[] { System.Reflection.Assembly.GetEntryAssembly().Location });
            //    }
            //    else
            //    {
            //        ServiceBase.Run(service);
            //    }
            //}
            //else
            //{
                // open console
                AllocConsole();
                service.Start(true);
            //}
        }
    }
}
