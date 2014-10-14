using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using dotNetInteropPlugin.PluginDev;

namespace AimpBetterCoverDisplay.Plugin
{
    [AIMPManagedPlugin("AIMP Better Cover Display", "LordJZ", "1.0.0")]
    public class AimpBetterCoverDisplayPlugin : MAIMPManagedPlugin
    {
        public const string PluginName = "AIMP Disc Cover";
        public const string Version = "1.5.0";

        string m_pipename;
        IAbcdService m_service;
        Process m_proc;

        public AimpBetterCoverDisplayPlugin()
        {
        }

        public override void ShowSettingDialog(System.Windows.Forms.IWin32Window parent)
        {
        }

        public override void Initialize()
        {
            Task.Run(() => InitializeAsync());

            this.Player.EventManager.PlayFileEvent += EventManager_PlayFileEvent;
            this.Player.EventManager.PlayerStateChangedEvent += EventManager_PlayerStateChangedEvent;
        }

        public override void Dispose()
        {
            this.Player.EventManager.PlayerStateChangedEvent -= EventManager_PlayerStateChangedEvent;
            this.Player.EventManager.PlayFileEvent -= EventManager_PlayFileEvent;

            ICommunicationObject service = Co(m_service);
            if (service != null)
            {
                try
                {
                    service.Close();
                }
                catch
                {
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
                }
            }
        }

        void EventManager_PlayFileEvent(object sender, MAIMPPlayerEventArgs args)
        {
            if (args.EventType == AIMP2CallbackType.AIMP_PLAY_FILE)
                this.UpdateRemoteProcess();
        }

        void EventManager_PlayerStateChangedEvent(object sender, MAIMPPlayerEventArgs args)
        {
            if (args.EventType == AIMP2CallbackType.AIMP_PLAYER_STATE)
                this.UpdateRemoteProcess();
        }

        NowPlaying m_np;

        NowPlaying GetNowPlaying()
        {
            NowPlaying np = new NowPlaying();

            try
            {
                if (Player.PlayingState != AIMPPlayingState.Stoped)
                {
                    IMAIMPCurrentPlayingInfo info = Player.CurrentPlayingInfo;
                    MAIMPFileInfo ti = info.GetCurrentTrackInfo();
                    np.Title = ti.Title;
                    np.Artist = ti.Artist;
                    np.Album = ti.Album;
                    np.FileName = ti.FileName;
                }
            }
            catch
            {
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

            await Task.Run(() => UpdateRemoteProcessAsync());
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

            ProcessStartInfo psi = new ProcessStartInfo("AimpBetterCoverDisplay.UI.exe", "/pipename " + m_pipename);
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

        public override bool HasSettingDialog
        {
            get { return false; }
        }
    }
}
