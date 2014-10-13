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
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class AbcdService : IAbcdService
    {
        public void NowPlayingChanged(NowPlaying np)
        {
            CoverSearcher.Search(np);
        }
    }
}
