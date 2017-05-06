using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Version
{
    // Current version of Ao3TrackReader
    static partial class Version
    {
        public const int Major = 1;
        public const int Minor = 0;
        public const int Build = 5;

        public const string String = "1.0.5";
        public const string LongString = "1.0.5.0";
        public const string AltString = "1.0.0.5";
        public const int Integer = Major * 1000000 + Minor * 1000 + Build;

        public const string Copyright = "Copyright © 2017 Alexis Ryan";

        public static (string Name, Uri uri) License => ("Apache License, Version 2.0", new Uri("https://www.apache.org/licenses/LICENSE-2.0"));

        public static Uri Source
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(GitTag))
                    return new Uri("https://github.com/wench/Ao3-Tracker/releases/" + GitTag);
                else if (!string.IsNullOrWhiteSpace(GitRevision))
                    return new Uri("https://github.com/wench/Ao3-Tracker/tree/" + GitRevision);
                else
                    return new Uri("https://github.com/wench/Ao3-Tracker");
            }
        }
    }
}
