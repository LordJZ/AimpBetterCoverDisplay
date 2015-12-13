using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AimpBetterCoverDisplay;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        string m_pipename;
        IAbcdService m_service;
        Process m_proc;

        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "A Better Cover Display";
            about.Description = "Displays";
            about.Author = "LordJZ";
            about.TargetApplication = "";   // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            about.Type = PluginType.General;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = ReceiveNotificationFlags.PlayerEvents;
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            return false;
        }

        public void SaveSettings()
        {
        }

        public void Close(PluginCloseReason reason)
        {
            ICommunicationObject service = Co(m_service);
            if (service != null)
            {
                try
                {
                    service.Close();
                }
                catch
                {
                    // ignore
                }
            }

            if (m_proc != null)
            {
                try
                {
                    m_proc.CloseMainWindow();
                }
                catch
                {
                    // ignore
                }
            }
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                    this.InitializeAsync();
                    break;
                case NotificationType.TrackChanged:
                    this.UpdateRemoteProcess();
                    break;
            }
        }

        public string[] GetProviders()
        {
            return null;
        }

        NowPlaying m_np;

        NowPlaying GetNowPlaying()
        {
            NowPlaying np = new NowPlaying();

            try
            {
                string file = mbApiInterface.NowPlaying_GetFileUrl();
                Console.WriteLine(file);

                np.Title = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle);
                np.Artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
                np.Album = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Album);
                np.FileName = file;
            }
            catch
            {
                // ignore
            }

            return np;
        }

        async void UpdateRemoteProcess()
        {
            // remote process has not been created
            if (m_pipename == null)
                return;

            NowPlaying np = GetNowPlaying();
            if (m_np == np)
                return;

            m_np = np;

            await Task.Factory.StartNew(() => UpdateRemoteProcessAsync());
        }

        void UpdateRemoteProcessAsync(bool retry = false)
        {
            IAbcdService service = m_service;

            try
            {
                service.NowPlayingChanged(m_np);
            }
            catch
            {
                if (!retry)
                {
                    ReinitializeService(service);

                    UpdateRemoteProcessAsync(true);
                }
            }
        }

        async void InitializeAsync()
        {
            m_pipename = "ABCD_" + Process.GetCurrentProcess().Id;

            const string filename = "AimpBetterCoverDisplay.UI.exe";
            string path = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "ABCD",
                filename);

            ProcessStartInfo psi = new ProcessStartInfo(path, "/pipename " + m_pipename);
            psi.UseShellExecute = false;
            psi.EnvironmentVariables["__COMPAT_LAYER"] = string.Empty;
            m_proc = Process.Start(psi);

            await Task.Delay(1000);

            ReinitializeService(null);

            UpdateRemoteProcess();
        }

        void ReinitializeService(IAbcdService previous)
        {
            IAbcdService service;
            try
            {
                var factory = new ChannelFactory<IAbcdService>(new NetNamedPipeBinding(),
                                                               "net.pipe://localhost/" + m_pipename);
                service = factory.CreateChannel();
            }
            catch
            {
                return;
            }

            IAbcdService actualPrevious = Interlocked.CompareExchange(ref m_service, service, previous);
            if (actualPrevious != previous)
                Co(service).Close();
        }

        static ICommunicationObject Co(IAbcdService svc)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            return (ICommunicationObject)svc;
        }
    }
}