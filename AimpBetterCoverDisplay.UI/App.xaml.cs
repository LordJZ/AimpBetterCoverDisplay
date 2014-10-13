using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;

namespace AimpBetterCoverDisplay.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            StartWcfServer(e.Args[Array.IndexOf(e.Args, "/pipename") + 1]);
        }

        static void StartWcfServer(string pipename)
        {
            ServiceHost svh = new ServiceHost(typeof(AbcdService));
            svh.AddServiceEndpoint(typeof(IAbcdService), new NetNamedPipeBinding(), "net.pipe://localhost/" + pipename);
            svh.Open();
        }
    }
}
