using System.ServiceModel;

namespace AimpBetterCoverDisplay
{
    [ServiceContract]
    public interface IAbcdService
    {
        [OperationContract]
        void NowPlayingChanged(NowPlaying np);
    }
}
