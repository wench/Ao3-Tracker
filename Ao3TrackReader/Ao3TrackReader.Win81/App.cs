using System;
using System.Collections.Generic;
using System.Text;

using System.Reflection;

namespace Ao3TrackReader
{
    public partial class App
    {
        public static string OSVersion
        {
            get
            {
                var analyticsInfoType = Type.GetType("Windows.System.Profile.AnalyticsInfo, Windows, ContentType=WindowsRuntime");
                var versionInfoType = Type.GetType("Windows.System.Profile.AnalyticsVersionInfo, Windows, ContentType=WindowsRuntime");
                if (analyticsInfoType != null && versionInfoType != null)
                {
                    var versionInfo = analyticsInfoType.GetRuntimeProperty("VersionInfo").GetValue(null);
                    ulong v = (ulong)versionInfoType.GetRuntimeProperty("DeviceFamilyVersion").GetValue(versionInfo);
                    ulong v1 = (v & 0xFFFF000000000000L) >> 48;
                    ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
                    ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
                    ulong v4 = (v & 0x000000000000FFFFL);
                    return $"{v1}.{v2}.{v3}.{v4}";
                }
                return null;
            }
        }

        public static string OSName
        {
            get
            {
                var eas = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
                return eas.OperatingSystem;
            }
        }

        public static string HardwareType => null;
    }
}
