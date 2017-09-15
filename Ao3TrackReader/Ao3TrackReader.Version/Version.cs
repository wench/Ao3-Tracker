using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Version
{
    // Current version of Ao3TrackReader
    static partial class Version
    {
        public const int Major = 1;
        public const int Minor = 1;
        public const int Build = 0;
        public const int Revision = 1;

        public const string String = "1.1.0.1";
        public const string IOS = "1.1.1";
        public const string UWP = "1.1.1.0";
        public const string Win81 = "1.1.0.1";
        public const int Integer = Major * 100000000 + Minor * 100000 + Build * 100 + Revision;

        public const string Copyright = "Copyright © 2017 Alexis Ryan";

        public static (string Name, Uri uri) License => ("Apache License, Version 2.0", new Uri("https://www.apache.org/licenses/LICENSE-2.0"));

        public static Uri Source
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(GitTag))
                    return new Uri("https://github.com/wench/Ao3-Tracker/releases/tag/" + GitTag);
                else if (!string.IsNullOrWhiteSpace(GitRevision))
                    return new Uri("https://github.com/wench/Ao3-Tracker/commit/" + GitRevision);
                else
                    return new Uri("https://github.com/wench/Ao3-Tracker");
            }
        }

        public static int AsInteger(int major, int minor, int build, int revision)
        {
            if (revision == -1)
                return major * 1000000 + minor * 1000 + build;
            else
                return major * 100000000 + minor * 100000 + build * 100 + Revision;
        }
    }
}
