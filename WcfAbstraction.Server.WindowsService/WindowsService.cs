using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace WcfAbstraction.Server.WindowsService
{
    partial class WindowsService : ServiceBase
    {
        #region Variables

        /// <summary>
        /// The service hosts element
        /// </summary>
        private ServiceHosts hosts = new ServiceHosts();
        
        #endregion

        public WindowsService()
        {
            InitializeComponent();

            this.EventLog.Log = "Wcf Abstraction Log";
        }

        protected override void OnStart(string[] args)
        {
            Start(false);

        }

        protected override void OnStop()
        {
            hosts.Stop();
        }

        public void Start(bool console)
        {
            if (console)
            {
                Console.WriteLine("Starting services...");
            }

            hosts.Start();

            if (console)
            {
                Console.WriteLine("---List of service:");
                foreach (string serv in hosts.ServiceNames)
                {
                    Console.WriteLine("     " + serv);
                }

                Console.WriteLine("Server is ready");
                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();
            }
        }
    }
}
