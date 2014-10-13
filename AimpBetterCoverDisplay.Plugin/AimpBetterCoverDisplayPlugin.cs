using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using dotNetInteropPlugin.PluginDev;

namespace AimpBetterCoverDisplay.Plugin
{
    [AIMPManagedPlugin("AIMP Better Cover Display", "LordJZ", "1.0.0")]
    public class AimpBetterCoverDisplayPlugin : MAIMPManagedPlugin
    {
        public const string PluginName = "AIMP Disc Cover";
        public const string Version = "1.5.0";

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

            if (m_service != null)
            {
                try
                {
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    ((ICommunicationObject)m_service).Close();
                }
                catch
                {
                }
            }

            if (m_proc != null)
            {
                try
                {
                    m_proc.Kill();
                }
                catch
                {
                }
            }
        }

        void EventManager_PlayFileEvent(object sender, MAIMPPlayerEventArgs args)
        {
            if (m_service == null)
                return;

            if (args.EventType == AIMP2CallbackType.AIMP_PLAY_FILE)
                this.UpdateRemoteProcess();
        }

        void EventManager_PlayerStateChangedEvent(object sender, MAIMPPlayerEventArgs args)
        {
            if (m_service == null)
                return;

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
            NowPlaying np = GetNowPlaying();
            if (m_np == np)
                return;

            m_np = np;

            await Task.Run(() =>
                           {
                               try
                               {
                                   m_service.NowPlayingChanged(m_np);
                               }
                               catch
                               {
                               }
                           });
        }

        async void InitializeAsync()
        {
            string pipename = "ABCD_" + Process.GetCurrentProcess().Id;

            m_proc = Process.Start("AimpBetterCoverDisplay.UI.exe", "/pipename " + pipename);

            await Task.Delay(1000);

            var factory = new ChannelFactory<IAbcdService>(new NetNamedPipeBinding(), "net.pipe://localhost/" + pipename);
            m_service = factory.CreateChannel();

            UpdateRemoteProcess();
        }

        public override bool HasSettingDialog
        {
            get { return false; }
        }
    }
}
