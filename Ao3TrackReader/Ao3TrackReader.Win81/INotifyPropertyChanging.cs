using Xamarin.Forms;

namespace Ao3TrackReader
{
    internal interface INotifyPropertyChanging
    {
        event PropertyChangingEventHandler PropertyChanging;
    }
}