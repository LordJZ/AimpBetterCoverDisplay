using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AimpBetterCoverDisplay.UI
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    class AbcdService : IAbcdService
    {
        public async void NowPlayingChanged(NowPlaying np)
        {
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => MessageBox.Show(np.FileName));
        }
    }
}
