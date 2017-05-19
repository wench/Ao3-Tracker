using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Profile;
using Windows.Foundation.Metadata;

namespace Ao3TrackReader
{
    public partial class App
    {
        static bool PhoneHasBackButton
        {
            get
            {
                if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
                {
                    if (typeof(Windows.Phone.UI.Input.HardwareButtons) == null)
                    {
                        var eh = new EventHandler<Windows.Phone.UI.Input.BackPressedEventArgs>((sender, e) => { });
                        Windows.Phone.UI.Input.HardwareButtons.BackPressed += eh;
                        Windows.Phone.UI.Input.HardwareButtons.BackPressed -= eh;
                    }
                    return true;
                }
                return false;
            }
        }

        public static InteractionMode InteractionMode
        {
            get
            {
                var s = Windows.UI.ViewManagement.UIViewSettings.GetForCurrentView();
                if (s.UserInteractionMode == Windows.UI.ViewManagement.UserInteractionMode.Mouse)
                    return InteractionMode.PC;

                try
                {
                    if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile" && PhoneHasBackButton)
                        return InteractionMode.Phone;
                }
                catch
                {

                }

                if (s.UserInteractionMode == Windows.UI.ViewManagement.UserInteractionMode.Touch)
                    return InteractionMode.Tablet;

                return InteractionMode.Unknown;
            }
        }

        public static string OSVersion
        {
            get
            {
                ulong v = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
                ulong v1 = (v & 0xFFFF000000000000L) >> 48;
                ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
                ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
                ulong v4 = (v & 0x000000000000FFFFL);
                return $"{v1}.{v2}.{v3}.{v4}";
            }
        }

        public static string OSName => AnalyticsInfo.VersionInfo.DeviceFamily;

        public static string HardwareType => AnalyticsInfo.DeviceForm;
    }
}
